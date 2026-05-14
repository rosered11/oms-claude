/**
 * UC3 — Customer places a Prepaid order via TikTok Marketplace (BU: CMG)
 *        TikTok retrieves the AWB (Air Waybill / tracking number) after OutForDelivery
 *
 * Channel-specific behaviour (docs/oms-overview.md §5):
 *   - channelType=Marketplace, businessUnit=CMG (TikTok shop)
 *   - After PackageDispatched → OutForDelivery, TikTok calls OMS to retrieve
 *     the AWB (GET /orders/{id}/packages) for parcel tracking on their platform.
 *
 * Prepaid sequence:
 *   Pending → PickStarted → POS Recalc → PickConfirmed →
 *   STS ABB/Tax Invoice (→ WMS + GW) → Packed → OutForDelivery
 *   [TikTok fetches AWB] → Delivered → Invoiced → Paid
 */

describe('UC3 — TikTok Marketplace / CMG / Prepaid — AWB retrieval after OutForDelivery', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC3-${Date.now()}`;
  const invoiceNum  = `INV-UC3-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a Prepaid order via TikTok Marketplace for CMG', () => {
    cy.createOrder({
      channelType:  'Marketplace',
      businessUnit: 'CMG',
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.channelType).to.eq('Marketplace');
      expect(order.businessUnit).to.eq('CMG');
      expect(order.paymentMethod).to.eq('Prepaid');
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
      waveId:    `WAVE-UC3-${Date.now()}`,
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
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 6 — STS webhook received; OMS dispatches ABBInvoiceSentToWMS + ABBInvoiceSentToGW', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC3-${Date.now()}`,
      invoiceAmount: 19800,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC3.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });

    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('ABBInvoiceSentToWMS');
      expect(names).to.include('ABBInvoiceSentToGW');
    });
  });

  it('Step 7 — WMS packed transitions order to Packed (package carries AWB tracking ID)', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [{ trackingId, vehicleType: 'Motorcycle', weight: 1.5, lineIds: [lineId] }],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
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

  // TikTok-specific step: after OutForDelivery, TikTok platform calls OMS to
  // retrieve the AWB (Air Waybill / tracking number) for parcel tracking.
  it('Step 8a — TikTok retrieves AWB from OMS after OutForDelivery', () => {
    cy.omsApi('GET', `/orders/${orderId}/packages`).then((res) => {
      expect(res.status).to.eq(200);
      const packages = res.body;
      expect(packages).to.have.length.gte(1);
      // AWB is the trackingId on the package
      const awb = packages[0].trackingId;
      expect(awb).to.eq(trackingId);
    });
  });

  it('Step 9 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer TikTok CMG',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
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
      expect(res.body.newStatus).to.eq('Paid');
    });
  });
});
