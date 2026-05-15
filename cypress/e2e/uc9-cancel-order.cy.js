/**
 * UC9 — OMS operator cancels order via OMS Kanban UI
 *
 * Scenario A: OMS operator cancels a Pending order via the Kanban board.
 *   - Clicking "Cancel Order" triggers window.confirm() then PATCH /orders/{id}/cancel
 *   - OMS transitions → Cancelled and dispatches three outbox events:
 *       OrderCancelledSentToWMS  — reverse stock reservation
 *       OrderCancelledSentToTMS  — cancel delivery booking
 *       OrderCancelledSentToGW   — notify customer
 *   - GET /orders/{id}/timeline verifies all four events appear
 *
 * Scenario B: Cancel from PickStarted → 409 invalid_transition.
 *
 * Cancel invariants (docs/oms-overview.md):
 *   - PATCH /orders/{id}/cancel valid from Pending, BookingConfirmed, OnHold only
 *   - Cancelled is a terminal state
 */

describe('UC9 — OMS operator cancels order via OMS UI', () => {
  let orderId1;
  let orderId2;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates order via API; verifies Pending status', () => {
    cy.createOrder().then((order) => {
      expect(order.status).to.eq('Pending');
      orderId1 = order.id;
    });
  });

  it('Step 2 — OMS operator opens Kanban UI and clicks Cancel Order on the Pending card', () => {
    cy.visit('/');
    cy.on('window:confirm', () => true);
    cy.get(`[data-testid="order-card-${orderId1}"]`, { timeout: 10000 }).should('be.visible');
    cy.get(`[data-testid="order-card-${orderId1}"]`)
      .contains('button', 'Cancel Order')
      .click();
    cy.omsApi('GET', `/orders/${orderId1}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Cancelled');
    });
  });

  it('Step 3 — Timeline contains OrderCancelled domain event and outbox events to WMS, TMS, GW', () => {
    cy.omsApi('GET', `/orders/${orderId1}/timeline`).then((res) => {
      expect(res.status).to.eq(200);
      const events = res.body.events;

      const domain = events.find(e => e.event === 'OrderCancelled');
      expect(domain, 'OrderCancelled domain event').to.exist;
      expect(domain.type).to.eq('Domain');

      const toWms = events.find(e => e.event === 'OrderCancelledSentToWMS');
      expect(toWms, 'OrderCancelledSentToWMS outbox event').to.exist;
      expect(toWms.system).to.eq('WMS');
      expect(toWms.type).to.eq('outbox');

      const toTms = events.find(e => e.event === 'OrderCancelledSentToTMS');
      expect(toTms, 'OrderCancelledSentToTMS outbox event').to.exist;
      expect(toTms.system).to.eq('TMS');
      expect(toTms.type).to.eq('outbox');

      const toGw = events.find(e => e.event === 'OrderCancelledSentToGW');
      expect(toGw, 'OrderCancelledSentToGW outbox event').to.exist;
      expect(toGw.system).to.eq('GW');
      expect(toGw.type).to.eq('outbox');
    });
  });

  it('Step 4 — Creates second order; verifies Pending status', () => {
    cy.createOrder().then((order) => {
      expect(order.status).to.eq('Pending');
      orderId2 = order.id;
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
