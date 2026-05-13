/**
 * UC25–UC29: Prepaid Order End-to-End Flow
 *
 * Prerequisites:
 *   1. dotnet run inside api/ — API on http://localhost:5050
 *   2. npm run serve:ui       — web-ui on http://localhost:3000
 */

describe('Prepaid Order Flow (UC25–UC29)', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-PREPAID-${Date.now()}`;
  const invoiceNumber = `INV-${Date.now()}`;
  const now = () => new Date().toISOString();

  // ── Step 1: POST /api/orders → Pending ────────────────────────────────────
  it('Step 1 — POST /api/orders creates a Prepaid order in Pending status', () => {
    cy.createPrepaidOrder().then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('Prepaid');
      orderId = order.id;
      lineId  = order.lines[0].id;
    });
  });

  // ── Step 2: WMS pick-started → PickStarted ────────────────────────────────
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

  // ── Step 3: Trigger recalculate → posRecalcPending: true ─────────────────
  it('Step 3 — POST recalculate sets posRecalcPending to true', () => {
    cy.omsApi('POST', `/orders/${orderId}/recalculate`).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.posRecalcPending).to.be.true;
    });
  });

  // ── Step 4: POS recalc-completed → posRecalcPending: false ───────────────
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

  // ── Step 5: WMS pick-confirmed → PickConfirmed ───────────────────────────
  it('Step 5 — WMS pick-confirmed transitions order to PickConfirmed', () => {
    // lineId captured in Step 1; guard in case test order changed
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

  // ── Step 6: Pre-delivery invoice (status stays PickConfirmed) ─────────────
  it('Step 6 — POST prepaid invoice creates pre-delivery invoice without changing status', () => {
    cy.omsApi('POST', `/orders/${orderId}/invoice/prepaid`).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.invoiceNumber).to.match(/^INV-PRE-/);
    });

    // Status must still be PickConfirmed
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.body.status).to.eq('PickConfirmed');
    });
  });

  // ── Step 7: WMS packed → Packed ──────────────────────────────────────────
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

  // ── Step 8: TMS package-dispatched → OutForDelivery ──────────────────────
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

  // ── Step 9: TMS package-delivered → Delivered ────────────────────────────
  it('Step 9 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt: now(),
      recipientName: 'Test Customer',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Delivered');
    });
  });

  // ── Step 10: POS invoiced → Invoiced ─────────────────────────────────────
  it('Step 10 — POS invoiced transitions order to Invoiced', () => {
    cy.omsApi('POST', '/webhooks/pos/invoiced', {
      orderId,
      invoiceNumber,
      totalAmount: 19800,
      currency: 'THB',
      invoicedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Invoiced');
    });
  });

  // ── Step 11: POS payment-confirmed → Paid ────────────────────────────────
  it('Step 11 — POS payment-confirmed transitions order to Paid', () => {
    cy.omsApi('POST', '/webhooks/pos/payment-confirmed', {
      orderId,
      invoiceNumber,
      paymentMethod: 'Prepaid',
      paidAmount: 19800,
      currency: 'THB',
      paidAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Paid');
    });
  });

  // ── Step 12: Web UI — order appears in Paid Kanban column ─────────────────
  it('Step 12 — Kanban board shows the order in the Paid column', () => {
    cy.visit('/');

    // Wait for the UI to load data from the live API
    cy.contains('● API', { timeout: 15000 });

    // Find the "Paid" column header, then assert the order ID is visible within it
    cy.contains('.kanban-col', 'Paid').within(() => {
      cy.contains(orderId, { timeout: 10000 });
    });
  });
});
