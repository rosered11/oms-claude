/**
 * UC9 — Customer cancels order
 *
 * Scenario A: Order cancelled from Pending (allowed).
 * Scenario B: Order advanced to PickStarted, cancel attempt rejected (409).
 *
 * Cancel invariants (docs/oms-overview.md):
 *   - PATCH /orders/{id}/cancel is valid from Pending, BookingConfirmed, OnHold
 *   - Cancel from PickStarted → 409 invalid_transition
 *   - Cancelled is a terminal state; GET confirms status persists
 */

describe('UC9 — Customer cancels order', () => {
  let orderId1;
  let orderId2;
  let lineId2;
  const now = () => new Date().toISOString();
  const trackingId = `TRK-UC9-${Date.now()}`;

  it('Step 1 — Creates first order; verifies Pending status', () => {
    cy.createOrder().then((order) => {
      expect(order.status).to.eq('Pending');
      orderId1 = order.id;
    });
  });

  it('Step 2 — Cancels first order from Pending; verifies newStatus Cancelled', () => {
    cy.omsApi('PATCH', `/orders/${orderId1}/cancel`, {
      reason:      'CustomerRequest',
      cancelledBy: 'customer@cfr.example.com',
    }).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.id).to.eq(orderId1);
      expect(res.body.newStatus).to.eq('Cancelled');
    });
  });

  it('Step 3 — GET first order; confirms status is Cancelled', () => {
    cy.omsApi('GET', `/orders/${orderId1}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Cancelled');
    });
  });

  it('Step 4 — Creates second order; verifies Pending status', () => {
    cy.createOrder().then((order) => {
      expect(order.status).to.eq('Pending');
      orderId2 = order.id;
      lineId2  = order.lines[0].id;
    });
  });

  it('Step 5 — WMS pick-started transitions second order to PickStarted', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-started', {
      orderId:   orderId2,
      pickerId:  'PICKER-002',
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickStarted');
    });
  });

  it('Step 6 — Cancel attempt from PickStarted returns 409 invalid_transition', () => {
    cy.omsApi('PATCH', `/orders/${orderId2}/cancel`, {
      reason:      'CustomerRequest',
      cancelledBy: 'customer@cfr.example.com',
    }).then((res) => {
      expect(res.status).to.eq(409);
      expect(res.body.error).to.eq('invalid_transition');
    });
  });
});
