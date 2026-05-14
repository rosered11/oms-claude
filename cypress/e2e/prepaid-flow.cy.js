/**
 * Prepaid Order End-to-End Flow
 *
 * Follows sequence-diagram-prepaid.md:
 *   Sale Order → WMS PickStarted → WMS Recalculation (repeatable, sync OMS→POS) →
 *   WMS PickConfirmed → STS ABB/Tax Invoice → [STS Credit Note (alt)] →
 *   WMS Packed → TMS OutForDelivery → TMS Delivered
 *
 * Prerequisites:
 *   1. dotnet run inside api/ — API on http://localhost:5050
 *   2. npm run serve:ui       — web-ui on http://localhost:3000
 */

describe('Prepaid Order Flow', () => {
  let orderId;
  let orderNumber;
  let lineId;
  const trackingId = `TRK-PREPAID-${Date.now()}`;
  const invoiceNumber = `INV-${Date.now()}`;
  const idempotencyKey = `idem-${Date.now()}`;
  const now = () => new Date().toISOString();

  // ── Step 1: Sale Order → Pending ──────────────────────────────────────────
  // Sequence: GW->>PS->>SC: Sale Order → Order created
  it('Step 1 — Sale Order creates a Prepaid order in Pending status', () => {
    cy.createPrepaidOrder().then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('Prepaid');
      orderId     = order.id;
      orderNumber = order.orderNumber;
      lineId      = order.lines[0].id;
    });
  });

  // ── Step 2: WMS PickStarted ────────────────────────────────────────────────
  // Sequence: WMS->>SC: Pick Started (inbound webhook) → PickStartedEvent → outbox → TMS
  it('Step 2 — WMS pick-started transitions order to PickStarted', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-started', {
      orderId,
      pickerId: 'PICKER-001',
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickStarted');
    });
  });

  // ── Step 3: WMS requests POS recalculation (synchronous outbound call) ──────
  // Sequence: WMS->>SC: POS Recalculation → SC->>POS [outbound] → adjustedAmount returned
  it('Step 3 — WMS requests POS recalculation; OMS calls POS outbound and returns adjusted amount', () => {
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

  // ── Step 4: WMS PickConfirmed ─────────────────────────────────────────────
  // Sequence: WMS->>SC: Pick Confirmed (basket qty)
  it('Step 4 — WMS pick-confirmed transitions order to PickConfirmed', () => {
    const lid = lineId || 'LINE-001';
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines: [
        { orderLineId: lid, sku: 'APPLE-1KG', pickedQty: 2, substituted: false },
      ],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  // ── Step 5: STS ABB/Tax Invoice webhook after PickConfirmed (prepaid) ─────
  // Sequence: STS->>SC: Webhook ABB/Tax Invoice [link] → SC->>WMS, SC->>GW
  // Order status must remain PickConfirmed — invoice is generated before dispatch.
  it('Step 5 — STS ABB/Tax Invoice webhook: SC records pre-dispatch invoice, status stays PickConfirmed', () => {
    cy.omsApi('POST', `/orders/${orderId}/invoice/prepaid`).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.invoiceNumber).to.match(/^INV-PRE-/);
    });

    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.body.status).to.eq('PickConfirmed');
    });
  });

  // ── Step 5b: STS Credit Note (alt — has credit note) ─────────────────────
  // Sequence: alt Has Credit Note: STS->>SC: Webhook Credit Note → SC->>WMS
  it.skip('Step 5b — STS Credit Note webhook (alt): SC relays credit note to WMS', () => {
    cy.omsApi('POST', `/orders/${orderId}/credit-note`, {
      creditNoteNumber: `CN-${Date.now()}`,
      amount: 500,
      currency: 'THB',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  // ── Step 6: WMS Packed → Packed ───────────────────────────────────────────
  it('Step 6 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [
        {
          trackingId,
          vehicleType: 'Motorcycle',
          weight: 2.5,
          lineIds: [lineId || 'LINE-001'],
        },
      ],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Packed');
      expect(res.body.packagesCreated).to.eq(1);
    });
  });

  // ── Step 7: TMS PackageDispatched → OutForDelivery ────────────────────────
  it('Step 7 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  // ── Step 8: TMS PackageDelivered → Delivered ──────────────────────────────
  it('Step 8 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt: now(),
      recipientName: 'Test Customer',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  // ── Step 9: Final state ───────────────────────────────────────────────────
  it('Step 9 — Final state: order is Delivered', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
    });
  });
});
