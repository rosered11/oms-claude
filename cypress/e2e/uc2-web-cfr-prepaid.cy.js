/**
 * UC2 — Customer places a Prepaid order via Web (BU: CFR)
 *
 * Same prepaid sequence as UC1 but for the CFR business unit.
 * Verifies BU-level data isolation: CFR orders carry businessUnit = CFR and
 * CMG operators cannot access them.
 *
 * Prepaid sequence (docs/oms-overview.md §2.2):
 *   Pending → PickStarted → POS Recalc → PickConfirmed →
 *   STS ABB/Tax Invoice (→ WMS + GW) → Packed → OutForDelivery → Delivered →
 *   Invoiced → Paid
 */

describe('UC2 — Web / CFR / Prepaid full order flow', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC2-${Date.now()}`;
  const invoiceNum  = `INV-UC2-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a Prepaid order for CFR via Web', () => {
    cy.createOrder({ businessUnit: 'CFR', channelType: 'Web' }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('Prepaid');
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
      waveId:    `WAVE-UC2-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 4 — WMS requests POS recalculation; POS returns result', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'MidPickPriceCheck',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.posRecalcPending).to.be.true;
    });

    cy.omsApi('POST', '/webhooks/pos/pos-recalc-completed', {
      orderId,
      finalAmount: 19800,
      currency:    'THB',
      completedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.posRecalcPending).to.be.false;
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

  it('Step 6 — STS webhook received; OMS dispatches ABBInvoiceSentToWMS + ABBInvoiceSentToGW', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC2-${Date.now()}`,
      invoiceAmount: 19800,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC2.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });

    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.body.status).to.eq('PickConfirmed');
    });

    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('ABBInvoiceSentToWMS');
      expect(names).to.include('ABBInvoiceSentToGW');
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
      recipientName: 'Test Customer CFR',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 10 — POS invoiced transitions order to Invoiced', () => {
    cy.omsApi('POST', '/webhooks/pos/invoiced', {
      orderId,
      invoiceNumber: invoiceNum,
      totalAmount:   19800,
      currency:      'THB',
      invoiceType:   'ABB',
      invoicedAt:    now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Invoiced');
    });
  });

  it('Step 11 — POS payment-confirmed transitions order to Paid', () => {
    cy.omsApi('POST', '/webhooks/pos/payment-confirmed', {
      orderId,
      invoiceNumber: invoiceNum,
      paymentMethod: 'Prepaid',
      paidAmount:    19800,
      currency:      'THB',
      paidAt:        now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Paid');
    });
  });

  it('Step 12 — Final state: order is Paid with correct BU', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Paid');
      expect(res.body.businessUnit).to.eq('CFR');
    });
  });
});
