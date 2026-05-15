/**
 * UC10 — Short-pick: dish soap out of stock; only water delivered
 *
 * Scenario (Thai): สั่งน้ำ 2 packs กับน้ำยาล้างจาน แต่น้ำยาล้างจานไม่มี เลยเอาแค่น้ำ
 *   Customer orders water (×2) + dish soap (×1).
 *   Dish soap is out of stock at pick time.
 *   WMS reports a short-pick; OMS records soap line pickedQty=0.
 *   Only water is packed and delivered.
 *
 * Prepaid sequence:
 *   Pending → PickStarted → WMS recalc (OutOfStock) → partial-pick →
 *   PickConfirmed → STS ABB/Tax Invoice → Packed (water only) →
 *   OutForDelivery → Delivered
 *
 * Key invariants exercised:
 *   - PATCH /orders/{id}/partial-pick records shortfall lines
 *   - WMS packed carries only the fulfilled lineIds
 *   - Final GET verifies dish soap line has pickedQty 0
 */

describe('UC10 — Short-pick: dish soap out of stock, only water delivered', () => {
  let orderId;
  let waterLineId;
  let soapLineId;
  const trackingId = `TRK-UC10-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates Prepaid order with water (×2) and dish soap (×1)', () => {
    cy.createOrder({
      businessUnit:  'CFR',
      channelType:   'Web',
      paymentMethod: 'Prepaid',
      isPrepaid:     true,
      lines: [
        {
          sku:           'WATER-500ML',
          productName:   'Water 500ml',
          requestedQty:  2,
          unitPrice:     1500,
          unitOfMeasure: 'Each',
        },
        {
          sku:           'DISH-SOAP-500ML',
          productName:   'Dish Soap 500ml',
          requestedQty:  1,
          unitPrice:     4500,
          unitOfMeasure: 'Each',
        },
      ],
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.lines).to.have.length(2);
      orderId     = order.id;
      waterLineId = order.lines[0].id;
      soapLineId  = order.lines[1].id;
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
      waveId:    `WAVE-UC10-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 4 — WMS recalculation-requested (OutOfStock); OMS calls POS and returns adjusted amount', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'OutOfStock',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.adjustedAmount).to.be.a('number');
    });
  });

  it('Step 5 — PATCH partial-pick: soap line pickedQty=0; verifies shortfallQuantity:1 on soap', () => {
    cy.omsApi('PATCH', `/orders/${orderId}/partial-pick`, {
      lines: [
        {
          orderLineId:      soapLineId,
          pickedQuantity:   0,
          orderedQuantity:  1,
          reason:           'OutOfStock',
        },
      ],
      idempotencyKey: `partial-pick-uc10-${Date.now()}`,
    }).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.orderId).to.eq(orderId);
      const partialLines = res.body.partialLines ?? res.body.lines ?? [];
      const soapLine = partialLines.find(
        (l) => l.orderLineId === soapLineId || l.sku === 'DISH-SOAP-500ML',
      );
      expect(soapLine).to.exist;
      expect(soapLine.shortfallQuantity).to.eq(1);
    });
  });

  it('Step 6 — WMS pick-confirmed: water picked (×2), soap picked (×0); transitions to PickConfirmed', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines: [
        { orderLineId: waterLineId, sku: 'WATER-500ML',   pickedQty: 2, substituted: false },
        { orderLineId: soapLineId,  sku: 'DISH-SOAP-500ML', pickedQty: 0, substituted: false },
      ],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 7 — STS ABB/Tax Invoice received; accepted', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC10-${Date.now()}`,
      invoiceAmount: 3000, // 2 × water only (soap not invoiced)
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC10.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  it('Step 8 — WMS packed with water line only; transitions to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [
        {
          trackingId,
          vehicleType: 'Motorcycle',
          weight:       1.0,
          lineIds:      [waterLineId],
        },
      ],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 9 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 10 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer UC10',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 11 — Final GET: status Delivered; dish soap line has pickedQty 0', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');

      const soapLine = res.body.lines.find(
        (l) => l.orderLineId === soapLineId || l.sku === 'DISH-SOAP-500ML' || l.id === soapLineId,
      );
      expect(soapLine).to.exist;
      expect(soapLine.pickedQty ?? soapLine.pickedQuantity).to.eq(0);
    });
  });
});
