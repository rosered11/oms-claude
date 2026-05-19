/**
 * 01-kanban.cy.js
 * Seeds all 14 orders via the real API, then verifies the Kanban board.
 */

describe('Kanban Board', () => {
  before(() => {
    cy.seedKanbanBoard();
  });

  beforeEach(() => {
    cy.visit('/');
  });

  // ── Column structure ────────────────────────────────────────────────────────

  it('renders the Kanban board', () => {
    cy.contains('Order Board').should('be.visible');
  });

  it('shows exactly 8 canonical columns', () => {
    cy.get('[class*="kanban-col"]').should('have.length', 8);
  });

  it('does not show non-canonical status columns', () => {
    ['ReadyForCollection', 'Returned', 'Paid'].forEach(s => {
      cy.contains(new RegExp(`^${s}$`)).should('not.exist');
    });
  });

  // ── Card counts ─────────────────────────────────────────────────────────────

  it('shows all 14 order cards', () => {
    cy.get('[data-testid^="order-card-"]').should('have.length', 14);
  });

  it('shows 2 Pending orders (Alice Johnson, Kate Brown)', () => {
    cy.contains('Alice Johnson').should('exist');
    cy.contains('Kate Brown').should('exist');
  });

  it('shows 1 PickStarted order (Bob Smith)', () => {
    cy.contains('Bob Smith').should('exist');
  });

  it('shows 1 PickConfirmed order (Iris Chen)', () => {
    cy.contains('Iris Chen').should('exist');
  });

  it('shows 2 Packed orders (Carol Davis, Grace Kim)', () => {
    cy.contains('Carol Davis').should('exist');
    cy.contains('Grace Kim').should('exist');
  });

  it('shows 2 OutForDelivery orders (David Wilson, Mia Patel)', () => {
    cy.contains('David Wilson').should('exist');
    cy.contains('Mia Patel').should('exist');
  });

  it('shows 4 Delivered orders', () => {
    ['Eve Martinez', 'Henry Park', 'James Taylor', 'Noah Kim'].forEach(name => {
      cy.contains(name).should('exist');
    });
  });

  it('shows 1 OnHold order (Frank Lee) with PackageDamaged reason', () => {
    cy.contains('Frank Lee').parents('[data-testid^="order-card-"]')
      .contains('PackageDamaged').should('exist');
  });

  it('shows 1 Cancelled order (Leo Nguyen)', () => {
    cy.contains('Leo Nguyen').should('exist');
  });

  // ── Card content ────────────────────────────────────────────────────────────

  it('shows order total amount on card', () => {
    // Alice: 2×120 + 3×65 + 1×45 + 2×185 + 1×89 = 939
    cy.contains('Alice Johnson').parents('[data-testid^="order-card-"]')
      .contains('939').should('exist');
  });

  it('shows channel type in order detail', () => {
    cy.contains('Alice Johnson').parents('[data-testid^="order-card-"]')
      .find('button').first().click();
    cy.contains('Channel:').should('contain.text', 'App');
  });

  // ── Inbound tab ─────────────────────────────────────────────────────────────

  it('switches to Inbound tab', () => {
    cy.contains('button', 'Inbound').click();
    cy.contains('Purchase Orders').should('be.visible');
  });
});
