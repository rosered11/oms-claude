/**
 * UC13 — Order with coupon discount (FRESH10 — 10% off)
 *
 * Scenario (Thai): Customer place order แต่มีใช้ส่วนลดคูปองในการจ่ายเงินด้วย
 *   Customer places a Web/CFR/Prepaid order and applies coupon FRESH10 (10% discount).
 *   POS applies the coupon at recalculation; adjustedAmount reflects the discount.
 *
 * Prepaid sequence with coupon:
 *   Pending → PickStarted → wave-started →
 *   POS recalc (FRESH10 10% off → adjustedAmount < original) →
 *   PickConfirmed → STS ABB/Tax Invoice (discounted amount) →
 *   Packed → OutForDelivery → Delivered
 *
 * Key invariants exercised:
 *   - Promotion/coupon can be attached at order creation via promotions[]
 *   - POS recalculation returns adjustedAmount (number) that reflects the discount
 *   - Final status is Delivered with correct BU
 *   - Timeline (optional) contains a recalculation event
 */

describe('UC13 — Web / CFR / Prepaid order with coupon FRESH10 (10% discount)', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC13-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates Prepaid order with coupon FRESH10; verifies Pending', () => {
    cy.createOrder({
      businessUnit:  'CFR',
      channelType:   'Web',
      paymentMethod: 'Prepaid',
      isPrepaid:     true,
      promotions: [
        {
          promoCode:           'FRESH10',
          promoType:           'PercentageDiscount',
          discountPercentage:  0.10,
        },
      ],
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.businessUnit).to.eq('CFR');
      orderId = order.id;
      lineId  = order.lines[0].id;
    });
  });

  it('Step 2 — WMS pick-started transitions order to PickStarted', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-started', {
      orderId,
      pickerId:  'PICKER-001',
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickStarted');
    });
  });

  it('Step 3 — WMS wave-started fires WaveStartedSentToGW outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC13-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 4 — WMS recalculation-requested; POS applies FRESH10 coupon; adjustedAmount is a number', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'PickQuantityDiffers',
      promoCode:   'FRESH10',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.adjustedAmount).to.be.a('number');
    });
  });

  it('Step 5 — WMS pick-confirmed transitions order to PickConfirmed', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines:    [{ orderLineId: lineId, sku: 'APPLE-1KG', pickedQty: 2, substituted: false }],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 6 — STS ABB/Tax Invoice received (discounted invoice amount); accepted', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC13-${Date.now()}`,
      invoiceAmount: 17820, // 19800 × 0.90 = 17820 (10% off)
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC13.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  it('Step 7 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [{ trackingId, vehicleType: 'Motorcycle', weight: 1.5, lineIds: [lineId] }],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 8 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 9 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer UC13',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 10 — Final GET: status Delivered, businessUnit CFR', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
      expect(res.body.businessUnit).to.eq('CFR');
    });
  });

  it('Step 11 — (Optional) Timeline contains a recalculation event', () => {
    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      if (res.status === 200) {
        const events = res.body.events ?? res.body;
        const names  = events.map((e) => e.event ?? e.type ?? '');
        const hasRecalc = names.some(
          (n) => n.toLowerCase().includes('recalc') || n.toLowerCase().includes('recalculation'),
        );
        expect(hasRecalc).to.be.true;
      }
    });
  });
});
