/**
 * UC14 — Prepaid partial return after delivery: dish soap returned, water kept
 *
 * Scenario (Thai): Customer place order ใน payment flow Prepaid 2 ชิ้น
 *   น้ำยาล้างจาน กับ น้ำเปล่า หลังจากลูกค้าได้รับสินค้าแล้ว
 *   ลูกค้าทำเรื่องขอคืนน้ำยาล้างจาน
 *
 *   Customer places a Web/CFR/Prepaid order for dish soap (×1) + water (×2).
 *   The full Prepaid flow runs to Delivered.
 *   After delivery, customer requests return of dish soap only (partial return).
 *   WMS confirms put-away of the returned dish soap.
 *   Refund is initiated for the dish soap amount.
 *   The water line is NOT returned — order stays at Delivered (partial return).
 *
 * Prepaid sequence with partial return:
 *   Pending → PickStarted → wave-started → POS recalc → PickConfirmed →
 *   STS ABB/Tax Invoice (→ WMS + Gateway) → Packed → OutForDelivery → Delivered →
 *   POST /returns (PartialItem, dish soap only) → Requested →
 *   WMS put-away-confirmed → PutAway (refundInitiated:true)
 *
 * Key invariants exercised:
 *   - Return only allowed after Delivered
 *   - Partial return references only the dish soap orderLineId
 *   - Water line is NOT in the return items
 *   - WMS put-away-confirmed → newReturnStatus: PutAway, refundInitiated: true
 *   - Order remains Delivered after partial return (not fully Returned)
 */

describe('UC14 — Web / CFR / Prepaid — partial return: dish soap returned, water kept', () => {
  let orderId;
  let dishSoapLineId;
  let waterLineId;
  let returnId;
  const trackingId = `TRK-UC14-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates Prepaid Web/CFR order with dish soap (×1) and water (×2)', () => {
    cy.createOrder({
      businessUnit:  'CFR',
      channelType:   'Web',
      paymentMethod: 'Prepaid',
      paymentFlow:   'PRE_PAID',
      lines: [
        {
          sku:           'DISH-SOAP-500ML',
          productName:   'Dish Soap 500ml',
          barcode:       '8851234590002',
          requestedQty:  1,
          unitPrice:     4500,
          unitOfMeasure: 'Each',
        },
        {
          sku:           'WATER-500ML',
          productName:   'Water 500ml',
          barcode:       '8851234590001',
          requestedQty:  2,
          unitPrice:     1500,
          unitOfMeasure: 'Each',
        },
      ],
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.businessUnit).to.eq('CFR');
      expect(order.lines).to.have.length(2);
      orderId        = order.id;
      dishSoapLineId = order.lines[0].id;
      waterLineId    = order.lines[1].id;
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

  it('Step 3 — WMS wave-started fires WaveStartedSentToGateway outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC14-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });

    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('WaveStartedSentToGateway');
    });
  });

  it('Step 4 — WMS recalculation-requested; OMS calls POS, returns adjustedAmount', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'PickQuantityDiffers',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.adjustedAmount).to.be.a('number');
    });
  });

  it('Step 5 — WMS pick-confirmed (both lines fully picked); transitions to PickConfirmed', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines: [
        { orderLineId: dishSoapLineId, sku: 'DISH-SOAP-500ML', pickedQty: 1, substituted: false },
        { orderLineId: waterLineId,    sku: 'WATER-500ML',      pickedQty: 2, substituted: false },
      ],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 6 — STS ABB/Tax Invoice received after PickConfirmed; OMS dispatches to WMS + Gateway (Prepaid routing)', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC14-${Date.now()}`,
      invoiceAmount: 7500, // 4500 soap + 2×1500 water
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC14.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });

    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('ABBTaxInvoiceSentToWMS');
      expect(names).to.include('ABBTaxInvoiceSentToGateway');
    });
  });

  it('Step 7 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [
        {
          trackingId,
          vehicleType: 'Motorcycle',
          weight:       1.5,
          lineIds:      [dishSoapLineId, waterLineId],
        },
      ],
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
      recipientName: 'Test Customer UC14',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 10 — GET order; confirms Delivered before initiating partial return', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
    });
  });

  it('Step 11 — Customer requests partial return: dish soap only (water kept)', () => {
    cy.omsApi('POST', '/returns', {
      orderId,
      returnType:   'PartialItem',
      returnReason: 'CustomerRequest',
      items: [
        {
          orderLineId: dishSoapLineId,
          sku:         'DISH-SOAP-500ML',
          quantity:    1,
          itemReason:  'CustomerRequest',
        },
      ],
      requestedBy: 'customer@cfr.example.com',
    }).then((res) => {
      expect(res.status).to.eq(201);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.status).to.be.oneOf(['Requested', 'ReturnRequested']);
      returnId = res.body.id;
    });
  });

  it('Step 12 — GET /returns/{returnId}; status Requested, orderId correct', () => {
    cy.omsApi('GET', `/returns/${returnId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.status).to.be.oneOf(['Requested', 'ReturnRequested']);
    });
  });

  it('Step 13 — GET /returns/{returnId}/items; contains dish soap, does NOT contain water', () => {
    cy.omsApi('GET', `/returns/${returnId}/items`).then((res) => {
      expect(res.status).to.eq(200);
      const items = res.body.items ?? res.body;
      const skus  = items.map((i) => i.sku);
      expect(skus).to.include('DISH-SOAP-500ML');
      expect(skus).not.to.include('WATER-500ML');
    });
  });

  it('Step 14 — WMS put-away-confirmed for dish soap; return → PutAway, refundInitiated true', () => {
    cy.omsApi('POST', '/webhooks/wms/put-away-confirmed', {
      returnId,
      items: [
        {
          sku:         'DISH-SOAP-500ML',
          condition:   'Resellable',
          sloc:        'C-03',
          quantity:    1,
          performedBy: 'wms-staff-01',
        },
      ],
      putAwayAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.returnId).to.eq(returnId);
      expect(res.body.newReturnStatus).to.eq('PutAway');
      expect(res.body.refundInitiated).to.be.true;
    });
  });

  it('Step 15 — GET /returns/{returnId}; status PutAway', () => {
    cy.omsApi('GET', `/returns/${returnId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('PutAway');
    });
  });

  it('Step 16 — GET order; order stays at Delivered (partial return — not all items returned)', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      // Partial return: only dish soap returned, water still delivered.
      // Order remains Delivered rather than transitioning to Returned.
      expect(res.body.status).to.eq('Delivered');
    });
  });
});
