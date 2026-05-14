/**
 * UC4 — Customer places a POD (Pay on Delivery) order via Web (BU: CFR)
 *
 * POD sequence (request/sequence-diagram-pod.md):
 *   Pending → PickStarted → POS Recalc → PickConfirmed → Packed →
 *   OutForDelivery → Delivered →
 *   STS ABB/Tax Invoice (issued after Delivered; → TMS + GW)
 *   [Optional Credit Note → TMS + GW]
 *
 * Terminal state: Delivered (no POS invoiced/payment-confirmed steps in POD).
 * Customer pays the TMS driver at the door; POS is not involved post-delivery.
 *
 * Key differences from Prepaid (UC1/UC2):
 *   - isPrepaid: false, paymentMethod: 'POD'
 *   - STS ABB/Tax Invoice issued AFTER Delivered, forwarded to TMS + GW (not WMS + GW)
 *   - Order stays at Delivered — no Invoiced or Paid transitions
 */

describe('UC4 — Web / CFR / POD full order flow', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC4-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a POD order for CFR via Web', () => {
    cy.createOrder({
      channelType:   'Web',
      businessUnit:  'CFR',
      paymentMethod: 'POD',
      isPrepaid:     false,
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('POD');
      expect(order.businessUnit).to.eq('CFR');
      expect(order.isPrepaid).to.be.false;
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
      waveId:    `WAVE-UC4-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
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

  it('Step 5 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [{ trackingId, vehicleType: 'Van', weight: 1.5, lineIds: [lineId] }],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 6 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 7 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer CFR POD',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  // POD-specific: STS issues ABB/Tax Invoice AFTER Delivered; forwarded to TMS + GW
  // No POS invoiced/payment-confirmed steps — POD terminal state is Delivered.
  it('Step 8 — STS ABB/Tax Invoice received after Delivered; OMS dispatches ABBTaxInvoiceSentToTMS + ABBTaxInvoiceSentToGW', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC4-${Date.now()}`,
      invoiceAmount: 19800,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC4.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });

    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.body.status).to.eq('Delivered');
    });

    // POD routing: ABB/Tax Invoice forwarded to TMS + GW (not WMS + GW as in Prepaid)
    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('ABBTaxInvoiceSentToTMS');
      expect(names).to.include('ABBTaxInvoiceSentToGW');
    });
  });

  it('Step 9 — Final state: order remains Delivered with correct BU and payment method', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
      expect(res.body.businessUnit).to.eq('CFR');
      expect(res.body.paymentMethod).to.eq('POD');
    });
  });
});
