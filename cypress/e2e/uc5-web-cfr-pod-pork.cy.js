/**
 * UC5 — Customer places a POD order via Web (BU: CFR) for weight-based pork product
 *
 * Scenario (Thai): หมู กิโลกรัมละ 127 บาท ลูกค้าซื้อ 841.23 กรัม
 *   Pork at 127 THB/kg — customer buys 841.23 grams (0.84123 kg)
 *
 * Weight-based pricing:
 *   unitPrice:    12700 satang / kg  (= 127 THB/kg)
 *   requestedQty: 0.84123 kg         (= 841.23 g)
 *   expectedTotal 10683.621 satang   (OMS uses decimal; POS rounds to 10684)
 *
 * Verifies that OMS correctly records fractional weight quantities and that the
 * full POD flow completes with the weight-derived amount.
 *
 * POD sequence:
 *   Pending → PickStarted → POS Recalc → PickConfirmed → Packed →
 *   OutForDelivery → Delivered → STS ABB Invoice → Invoiced → Paid
 */

describe('UC5 — Web / CFR / POD — weight-based pork product (841.23 g)', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC5-${Date.now()}`;
  const invoiceNum  = `INV-UC5-${Date.now()}`;
  // 127 THB/kg × 0.84123 kg = 106.83621 THB = 10683.621 satang → POS rounds to 10684
  const PORK_UNIT_PRICE_SATANG = 12700;   // 127 THB in satang per KG
  const PORK_QTY_KG            = 0.84123; // 841.23 g expressed in KG
  const PORK_FINAL_SATANG      = 10684;   // POS-rounded total in satang
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a POD order with weight-based pork line (841.23 g)', () => {
    cy.createOrder({
      channelType:   'Web',
      businessUnit:  'CFR',
      paymentMethod: 'POD',
      isPrepaid:     false,
      lines: [
        {
          sku:           'PORK-KG',
          productName:   'Pork (per kg)',
          barcode:       '8851234570001',
          requestedQty:  PORK_QTY_KG,
          unitPrice:     PORK_UNIT_PRICE_SATANG,
          unitOfMeasure: 'KG',
        },
      ],
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('POD');
      expect(order.businessUnit).to.eq('CFR');

      const line = order.lines[0];
      expect(line.sku).to.eq('PORK-KG');
      // Verify the fractional weight quantity is stored precisely
      expect(line.requestedAmount).to.be.closeTo(PORK_QTY_KG, 0.00001);

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
      expect(res.body.newStatus).to.eq('PickStarted');
    });
  });

  it('Step 3 — WMS wave-started fires WaveStartedSentToGW outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC5-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 4 — WMS requests POS recalculation; POS returns weight-derived amount (10684 satang)', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'WeightBasedPricing',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.posRecalcPending).to.be.true;
    });

    // POS returns the weight-derived amount (127 THB/kg × 0.84123 kg = 10684 satang)
    cy.omsApi('POST', '/webhooks/pos/pos-recalc-completed', {
      orderId,
      finalAmount: PORK_FINAL_SATANG,
      currency:    'THB',
      completedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.posRecalcPending).to.be.false;
    });
  });

  it('Step 5 — WMS pick-confirmed with actual weight (841.23 g = 0.84123 kg)', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines:    [{ orderLineId: lineId, sku: 'PORK-KG', pickedQty: PORK_QTY_KG, substituted: false }],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 6 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [{ trackingId, vehicleType: 'Motorcycle', weight: PORK_QTY_KG, lineIds: [lineId] }],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 7 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 8 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer CFR Pork',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 9 — STS ABB/Tax Invoice received after Delivered (POD: forwarded to TMS + GW)', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC5-${Date.now()}`,
      invoiceAmount: PORK_FINAL_SATANG,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC5.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  it('Step 10 — POS invoiced transitions order to Invoiced', () => {
    cy.omsApi('POST', '/webhooks/pos/invoiced', {
      orderId,
      invoiceNumber: invoiceNum,
      totalAmount:   PORK_FINAL_SATANG,
      currency:      'THB',
      invoiceType:   'Standard',
      invoicedAt:    now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Invoiced');
    });
  });

  it('Step 11 — POS payment-confirmed transitions order to Paid', () => {
    cy.omsApi('POST', '/webhooks/pos/payment-confirmed', {
      orderId,
      invoiceNumber: invoiceNum,
      paymentMethod: 'Cash',
      paidAmount:    PORK_FINAL_SATANG,
      currency:      'THB',
      paidAt:        now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Paid');
    });
  });

  it('Step 12 — Final state: order is Paid with correct weight-based amount', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Paid');
      expect(res.body.amount).to.be.closeTo(PORK_FINAL_SATANG, 1);
    });
  });
});
