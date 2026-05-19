/**
 * 03-timeline.cy.js
 * Creates a Delivered order via the real API, then verifies timeline events.
 */

describe('Order Timeline', () => {
  before(() => {
    cy.resetStore();

    // ORD-001 → Delivered (generates OrderCreated, PickStarted, PickConfirmed, Packed,
    //            PackageDispatched, PackageDelivered timeline events)
    cy.request('POST', `${Cypress.env('apiBase')}/orders`, {
      sourceOrderId: 'SRC-TIMELINE-01', customerName: 'Timeline Test', customerPhone: '0810000099',
      channelType: 'App', businessUnit: 'CMG', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      lines: [
        { sku: 'APPLE-1KG', productName: 'Apple 1kg', barcode: '8850001001', requestedQty: 1, unitPrice: 120, unitOfMeasure: 'Unit' },
      ],
    }).its('status').should('eq', 201);

    cy.advanceOrder('ORD-001', 'Delivered', { lineCount: 1, trackingId: 'TRK-E2E-ORD-001', customerName: 'Timeline Test' });
  });

  beforeEach(() => {
    cy.visit('/');
    cy.contains('Timeline Test').parents('[data-testid^="order-card-"]')
      .find('button').first().click();
    cy.contains('View System Event Timeline').click();
  });

  it('opens the timeline view', () => {
    cy.contains('Timeline').should('be.visible');
  });

  it('shows OrderCreated domain event', () => {
    cy.contains('OrderCreated').should('be.visible');
  });

  it('shows PickStarted webhook event', () => {
    cy.contains('PickStarted').should('be.visible');
  });

  it('shows PickConfirmed event', () => {
    cy.contains('PickConfirmed').should('be.visible');
  });

  it('shows PackageDelivered event', () => {
    cy.contains('PackageDelivered').should('be.visible');
  });
});
