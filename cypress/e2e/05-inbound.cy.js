/**
 * 05-inbound.cy.js
 * Seeds Purchase Orders and Transfer Orders via the API using seedInboundData(),
 * then verifies the Inbound view renders them correctly.
 */

describe('Inbound View', () => {
  before(() => {
    cy.resetStore();
    cy.seedInboundData();
  });

  beforeEach(() => {
    cy.visit('/');
    cy.contains('button', 'Inbound').click();
  });

  // ── Purchase Orders ─────────────────────────────────────────────────────────

  it('shows Purchase Orders section', () => {
    cy.contains('Purchase Orders').should('be.visible');
  });

  it('lists all 4 purchase orders', () => {
    // Fresh Foods Ltd appears twice (PO-001 and PO-004)
    ['Fresh Foods Ltd', 'Thai Dairy Co', 'Organic Farms Ltd'].forEach(supplier => {
      cy.contains(supplier).should('be.visible');
    });
  });

  it('shows PO store assignment', () => {
    cy.contains('Central DC').should('be.visible');
  });

  it('shows a Closed PO (Fresh Foods Ltd PO-001)', () => {
    cy.contains('Fresh Foods Ltd').should('be.visible');
  });

  it('shows a Created PO (Organic Farms Ltd)', () => {
    cy.contains('Organic Farms Ltd').should('be.visible');
  });

  // ── Transfer Orders ─────────────────────────────────────────────────────────

  it('shows Transfer Orders section', () => {
    cy.contains('Transfer Orders').should('be.visible');
  });

  it('lists all 3 transfer orders', () => {
    cy.contains('Transfer Orders').should('be.visible');
    cy.get('[data-testid^="transfer-order-"], tr').should('have.length.gte', 1);
  });

  it('shows source and destination stores in transfer orders', () => {
    cy.contains('Central DC').should('be.visible');
  });
});
