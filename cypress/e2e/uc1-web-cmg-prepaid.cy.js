/**
 * UC1 — Customer places a Prepaid order via Web (BU: CMG)
 *
 * Prepaid sequence (docs/oms-overview.md §2.2):
 *   Pending → PickStarted → POS Recalc → PickConfirmed →
 *   STS ABB/Tax Invoice (→ WMS + Gateway) → Packed → OutForDelivery → Delivered
 *
 * Key invariants exercised:
 *   - WMS pick can start directly from Pending (no BookingConfirmed step)
 *   - STS ABB/Tax Invoice issued after PickConfirmed, forwarded to WMS + Gateway
 *   - BU isolation: order carries businessUnit = CMG
 *   - POS recalculation is an outbound OMS → POS call; POS does not webhook OMS
 */

describe('UC1 — Web / CMG / Prepaid full order flow', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC1-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a Prepaid order for CMG via Web', () => {
    cy.createOrder({ businessUnit: 'CMG', channelType: 'Web' }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('Prepaid');
      expect(order.businessUnit).to.eq('CMG');
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

  it('Step 3 — WMS wave-started fires WaveStartedSentToGateway outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC1-${Date.now()}`,
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

  it('Step 3a — WMS requests POS recalculation; OMS calls POS outbound and returns adjusted amount', () => {
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

  it('Step 4 — WMS pick-confirmed transitions order to PickConfirmed; OMS calls POS internally', () => {
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

  it('Step 5 — STS webhook received; OMS dispatches ABBTaxInvoiceSentToWMS + ABBTaxInvoiceSentToGateway', () => {
    // STS webhook must arrive before OMS can dispatch to WMS/Gateway — the outbox
    // events are only created as a direct consequence of this webhook.
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC1-${Date.now()}`,
      invoiceAmount: 19800,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC1.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });

    // Order must stay at PickConfirmed — invoice does not advance the state machine
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.body.status).to.eq('PickConfirmed');
    });

    // Timeline must show both outbox events created by the STS webhook
    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('ABBTaxInvoiceSentToWMS');
      expect(names).to.include('ABBTaxInvoiceSentToGateway');
    });
  });

  it('Step 6 — WMS packed transitions order to Packed', () => {
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
      recipientName: 'Test Customer',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 9 — Final state: order is Delivered with correct BU', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
      expect(res.body.businessUnit).to.eq('CMG');
    });
  });
});
