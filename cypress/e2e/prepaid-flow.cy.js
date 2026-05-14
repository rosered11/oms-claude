/**
 * Prepaid Order End-to-End Flow
 *
 * Follows sequence-diagram-prepaid.md:
 *   Sale Order → WMS PickStarted → POS Recalculation (repeatable) →
 *   WMS PickConfirmed → STS ABB/Tax Invoice → [STS Credit Note (alt)] →
 *   WMS Packed → TMS OutForDelivery → TMS Delivered → POS Invoiced → POS Paid
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

  // ── Step 3: WMS triggers POS recalculation → posRecalcPending: true ───────
  // Sequence: WMS->>SC: POS Recalculation → SC->>POS: POS Recalculation
  // WMS calls SC's /orders/{id}/recalculate; SC internally triggers POS.
  // Note: sequence marks this block "Can be recalculation every time" (repeatable).
  it('Step 3 — WMS triggers POS recalculation, setting posRecalcPending to true', () => {
    cy.omsApi('POST', `/orders/${orderId}/recalculate`).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.posRecalcPending).to.be.true;
    });
  });

  // ── Step 4: POS amount recalculation result → posRecalcPending: false ─────
  // Sequence: POS-->>SC: Amount Recalculation → SC-->>WMS: Amount Recalculation
  it('Step 4 — POS recalc-completed clears posRecalcPending', () => {
    cy.omsApi('POST', '/webhooks/pos/pos-recalc-completed', {
      orderId,
      finalAmount: 19800,
      currency: 'THB',
      completedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.posRecalcPending).to.be.false;
    });
  });

  // ── Step 5: WMS PickConfirmed ──────────────────────────────────────────────
  // Sequence: WMS->>SC: Pick Confirmed (basket qty) → PickConfirmedEvent → outbox → POS
  it('Step 5 — WMS pick-confirmed transitions order to PickConfirmed', () => {
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

  // ── Step 6: STS ABB/Tax Invoice webhook after PickConfirmed (prepaid) ─────
  // Sequence: STS->>SC: Webhook ABB/Tax Invoice [link] → SC->>WMS, SC->>GW
  // The /orders/{id}/invoice/prepaid endpoint simulates the STS inbound webhook
  // (no dedicated STS webhook controller exists; SC processes the invoice link here).
  // Order status must remain PickConfirmed — invoice is generated before dispatch.
  it('Step 6 — STS ABB/Tax Invoice webhook: SC records pre-dispatch invoice, status stays PickConfirmed', () => {
    cy.omsApi('POST', `/orders/${orderId}/invoice/prepaid`).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.invoiceNumber).to.match(/^INV-PRE-/);
    });

    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.body.status).to.eq('PickConfirmed');
    });
  });

  // ── Step 6b: STS Credit Note (alt — has credit note) ──────────────────────
  // Sequence: alt Has Credit Note: STS->>SC: Webhook Credit Note → SC->>WMS
  // No dedicated STS credit-note webhook endpoint exists yet; skipped until implemented.
  it.skip('Step 6b — STS Credit Note webhook (alt): SC relays credit note to WMS', () => {
    cy.omsApi('POST', `/orders/${orderId}/credit-note`, {
      creditNoteNumber: `CN-${Date.now()}`,
      amount: 500,
      currency: 'THB',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  // ── Step 7: WMS Packed → Packed ───────────────────────────────────────────
  // Required by state machine (PickConfirmed → Packed → OutForDelivery).
  // TMS package-dispatched resolves order by trackingId; packages must exist first.
  it('Step 7 — WMS packed transitions order to Packed', () => {
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

  // ── Step 8: TMS PackageDispatched → OutForDelivery ────────────────────────
  // Sequence: TMS->>SC: Out for Delivery [/tms/package-dispatched] → SC->>GW
  it('Step 8 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  // ── Step 9: TMS PackageDelivered → Delivered ──────────────────────────────
  // Sequence: TMS->>SC: Delivered [/tms/package-delivered]
  //           DeliveredSentToGW → outbox → GW
  it('Step 9 — TMS package-delivered transitions order to Delivered', () => {
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

  // ── Step 10: POS Invoiced → Invoiced ──────────────────────────────────────
  // invoiceType 'ABB' matches the ABB/Tax Invoice sent by STS in Step 6.
  it('Step 10 — POS invoiced transitions order to Invoiced', () => {
    cy.omsApi('POST', '/webhooks/pos/invoiced', {
      orderId,
      invoiceNumber,
      totalAmount:   19800,
      currency:      'THB',
      invoiceType:   'ABB',
      invoicedAt:    now(),
      idempotencyKey: `${idempotencyKey}-inv`,
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Invoiced');
    });
  });

  // ── Step 11: POS PaymentConfirmed → Paid ──────────────────────────────────
  it('Step 11 — POS payment-confirmed transitions order to Paid', () => {
    cy.omsApi('POST', '/webhooks/pos/payment-confirmed', {
      orderId,
      invoiceNumber,
      paymentMethod:  'Prepaid',
      paidAmount:     19800,
      currency:       'THB',
      paidAt:         now(),
      idempotencyKey: `${idempotencyKey}-pay`,
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Paid');
    });
  });

  // ── Step 12: Web UI — order appears in Paid Kanban column ─────────────────
  it('Step 12 — Kanban board shows the order in the Paid column', () => {
    cy.visit('/');

    // Wait for the Kanban to render with live API data (columns appear after fetch)
    cy.get('.kanban-col', { timeout: 15000 }).should('have.length.gt', 0);

    // OrderCard renders order.orderNumber (SC-XXX), not order.id (ORD-XXX)
    cy.contains('.kanban-col', 'Paid').within(() => {
      cy.contains(orderNumber, { timeout: 10000 });
    });
  });
});
