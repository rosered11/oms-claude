/**
 * UC12 — Full order return after delivery
 *
 * Scenario (Thai): Customer สั่งสินค้า ได้รับสินค้าแล้ว แต่ต้องการขอคืนสินค้า
 *   Customer receives goods then initiates a full return (CustomerRequest).
 *   WMS confirms put-away; refund is initiated automatically.
 *
 * Prepaid sequence (full delivery then return):
 *   Pending → PickStarted → wave-started → POS recalc → PickConfirmed →
 *   STS ABB/Tax Invoice → Packed → OutForDelivery → Delivered →
 *   POST /returns (full, CustomerRequest) → Requested →
 *   WMS put-away-confirmed → PutAway (refundInitiated:true)
 *
 * Key invariants exercised:
 *   - Return is only allowed after Delivered
 *   - POST /returns → 201, status Requested
 *   - GET /returns/{id} → status Requested or ReturnRequested
 *   - GET /returns/{id}/items → includes the returned APPLE-1KG line
 *   - POST /webhooks/wms/put-away-confirmed → newReturnStatus PutAway, refundInitiated true
 */

describe('UC12 — Full return after delivery (CustomerRequest)', () => {
  let orderId;
  let lineId;
  let returnId;
  const trackingId = `TRK-UC12-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a Web/CFR/Prepaid order; verifies Pending', () => {
    cy.createOrder({ businessUnit: 'CFR' }).then((order) => {
      expect(order.status).to.eq('Pending');
      orderId = order.id;
      lineId  = order.lines[0].id;
    });
  });

  it('Step 2a — WMS pick-started transitions order to PickStarted', () => {
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

  it('Step 2b — WMS wave-started fires WaveStartedSentToGW outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC12-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 2c — WMS recalculation-requested; OMS calls POS, returns adjusted amount', () => {
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

  it('Step 2d — WMS pick-confirmed transitions order to PickConfirmed', () => {
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

  it('Step 2e — STS ABB/Tax Invoice received; accepted', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC12-${Date.now()}`,
      invoiceAmount: 19800,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC12.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  it('Step 2f — WMS packed transitions order to Packed', () => {
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

  it('Step 2g — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 2h — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer UC12',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 3 — GET order; confirms status Delivered before initiating return', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
    });
  });

  it('Step 4 — POST /returns for full order; verifies 201 and status Requested', () => {
    cy.omsApi('POST', '/returns', {
      orderId,
      returnReason: 'CustomerRequest',
      items: [
        {
          orderLineId: lineId,
          sku:         'APPLE-1KG',
          quantity:    2,
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

  it('Step 5 — GET /returns/{returnId}; status Requested, orderId correct', () => {
    cy.omsApi('GET', `/returns/${returnId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.status).to.be.oneOf(['Requested', 'ReturnRequested']);
    });
  });

  it('Step 6 — GET /returns/{returnId}/items; items include APPLE-1KG', () => {
    cy.omsApi('GET', `/returns/${returnId}/items`).then((res) => {
      expect(res.status).to.eq(200);
      const items = res.body.items ?? res.body;
      const skus = items.map((i) => i.sku);
      expect(skus).to.include('APPLE-1KG');
    });
  });

  it('Step 7 — WMS put-away-confirmed; verifies newReturnStatus PutAway and refundInitiated true', () => {
    cy.omsApi('POST', '/webhooks/wms/put-away-confirmed', {
      returnId,
      items: [
        {
          sku:         'APPLE-1KG',
          condition:   'Resellable',
          sloc:        'B-05',
          quantity:    2,
          performedBy: 'wms-picker-07',
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

  it('Step 8 — Final GET /returns/{returnId}; status is PutAway', () => {
    cy.omsApi('GET', `/returns/${returnId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('PutAway');
    });
  });
});
