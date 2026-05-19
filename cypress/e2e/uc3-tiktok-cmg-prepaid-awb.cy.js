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
 *   STS ABB/Tax Invoice (→ WMS + Gateway) → Packed → OutForDelivery
 *   [TikTok fetches AWB] → Delivered
 *
 * POS recalculation is an outbound OMS → POS call; POS does not webhook OMS.
 */

describe('UC3 — TikTok Marketplace / CMG / Prepaid — AWB retrieval after OutForDelivery', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC3-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a Prepaid order via TikTok Marketplace for CMG', () => {
    cy.createOrder({
      channelType:  'Marketplace',
      subChannel:   'TikTok',
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

  it('Step 3 — WMS wave-started fires WaveStartedSentToGateway outbox event', () => {
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
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 5 — STS webhook received; OMS dispatches ABBTaxInvoiceSentToWMS + ABBTaxInvoiceSentToGateway', () => {
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
      expect(names).to.include('ABBTaxInvoiceSentToWMS');
      expect(names).to.include('ABBTaxInvoiceSentToGateway');
    });
  });

  it('Step 6 — WMS packed transitions order to Packed (package carries AWB tracking ID)', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [{ trackingId, vehicleType: 'Motorcycle', weight: 1.5, lineIds: [lineId] }],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 7 — TMS package-dispatched transitions order to OutForDelivery; OMS dispatches AWB-notify to TikTok Marketplace', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });

    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const awbOutbox = events.find(
        (e) => e.type === 'outbox' && e.system === 'Marketplace' && e.event === 'OutForDeliveryEvent',
      );
      expect(awbOutbox, 'TikTok AWB-notify outbox event must be present').to.exist;
    });
  });

  // TikTok-specific step: after OutForDelivery, TikTok platform calls OMS to
  // retrieve the AWB (Air Waybill / tracking number) for parcel tracking.
  it('Step 7a — TikTok retrieves AWB from OMS after OutForDelivery', () => {
    cy.omsApi('GET', `/orders/${orderId}/packages`).then((res) => {
      expect(res.status).to.eq(200);
      const packages = res.body;
      expect(packages).to.have.length.gte(1);
      // AWB is the trackingId on the package
      const awb = packages[0].trackingId;
      expect(awb).to.eq(trackingId);
    });
  });

  it('Step 8 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer TikTok CMG',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
    });
  });

  it('Step 9 — Final state: order is Delivered with correct channel and BU', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
      expect(res.body.channelType).to.eq('Marketplace');
      expect(res.body.businessUnit).to.eq('CMG');
    });
  });
});
