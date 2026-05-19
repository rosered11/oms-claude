/**
 * 02-order-detail.cy.js
 * Creates specific orders via the API and verifies the detail panel.
 * After resetStore(), IDs are predictable: first order = ORD-001, etc.
 */

describe('Order Detail View', () => {
  before(() => {
    cy.resetStore();

    // ORD-001 — Alice, Pending, 5 lines, delivery slot
    cy.request('POST', `${Cypress.env('apiBase')}/orders`, {
      sourceOrderId: 'SRC-ALICE-DET', customerName: 'Alice Johnson', customerPhone: '0810000001',
      channelType: 'App', businessUnit: 'TOPS', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      deliverySlot: { scheduledStart: '2024-01-15T18:00:00Z', scheduledEnd: '2024-01-15T20:00:00Z' },
      lines: [
        { sku: 'APPLE-1KG',   productName: 'Apple (1 kg bag)',    barcode: '8850001001', requestedQty: 2, unitPrice: 120,  unitOfMeasure: 'Unit' },
        { sku: 'MILK-1L',     productName: 'Whole Milk 1L',       barcode: '8850001002', requestedQty: 3, unitPrice: 65,   unitOfMeasure: 'Unit' },
        { sku: 'BREAD-WH',    productName: 'Whole Wheat Bread',   barcode: '8850001003', requestedQty: 1, unitPrice: 45,   unitOfMeasure: 'Unit' },
        { sku: 'CHEESE-200G', productName: 'Cheddar Cheese 200g', barcode: '8850001004', requestedQty: 2, unitPrice: 185,  unitOfMeasure: 'Unit' },
        { sku: 'YOGURT-500G', productName: 'Greek Yogurt 500g',   barcode: '8850001005', requestedQty: 1, unitPrice: 89,   unitOfMeasure: 'Unit' },
      ],
    }).its('status').should('eq', 201);

    // ORD-002 — David, OutForDelivery, delivery slot, package TRK-E2E-ORD-002
    cy.request('POST', `${Cypress.env('apiBase')}/orders`, {
      sourceOrderId: 'SRC-DAVID-DET', customerName: 'David Wilson', customerPhone: '0810000004',
      channelType: 'App', businessUnit: 'CMG', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PayOnDelivery', paymentFlow: 'PAY_ON_DELIVERY',
      deliverySlot: { scheduledStart: '2024-01-15T15:00:00Z', scheduledEnd: '2024-01-15T17:00:00Z' },
      lines: [
        { sku: 'BEEF-1KG',   productName: 'Ground Beef 1kg', barcode: '8850004001', requestedQty: 1, unitPrice: 320, unitOfMeasure: 'Unit' },
        { sku: 'POTATO-2KG', productName: 'Potato 2kg bag',  barcode: '8850004002', requestedQty: 1, unitPrice: 89,  unitOfMeasure: 'Unit' },
      ],
    }).its('status').should('eq', 201);
    cy.advanceOrder('ORD-002', 'OutForDelivery', { lineCount: 2, trackingId: 'TRK-E2E-ORD-002', customerName: 'David Wilson' });

    // ORD-003 — Frank, OnHold (preHoldStatus OutForDelivery), holdReason PackageDamaged
    cy.request('POST', `${Cypress.env('apiBase')}/orders`, {
      sourceOrderId: 'SRC-FRANK-DET', customerName: 'Frank Lee', customerPhone: '0810000006',
      channelType: 'App', businessUnit: 'TOPS', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      lines: [
        { sku: 'TV-55IN', productName: '55-inch Smart TV', barcode: '8850006001', requestedQty: 1, unitPrice: 4500, unitOfMeasure: 'Unit' },
      ],
    }).its('status').should('eq', 201);
    cy.advanceOrder('ORD-003', 'OnHold', { lineCount: 1, trackingId: 'TRK-E2E-ORD-003', holdAfter: 'OutForDelivery', holdReason: 'PackageDamaged', customerName: 'Frank Lee' });
  });

  beforeEach(() => {
    cy.visit('/');
  });

  // ── Alice (ORD-001): Pending, 5 lines ────────────────────────────────────────

  context('Alice Johnson — Pending, 5 lines, delivery slot', () => {
    beforeEach(() => {
      cy.contains('Alice Johnson').parents('[data-testid^="order-card-"]')
        .find('button').first().click();
    });

    it('opens the detail panel', () => {
      cy.contains('Alice Johnson').should('be.visible');
    });

    it('shows Pending status', () => {
      cy.contains('Pending').should('be.visible');
    });

    it('shows channel App', () => {
      cy.contains('Channel:').should('contain.text', 'App');
    });

    it('shows correct total (939 THB: 2×120+3×65+1×45+2×185+1×89)', () => {
      cy.contains('939').should('be.visible');
    });

    it('shows delivery slot', () => {
      cy.contains('Delivery Slot').should('be.visible');
    });

    it('lists all 5 order lines', () => {
      cy.get('table tbody tr').should('have.length', 5);
    });

    it('shows correct SKUs', () => {
      ['APPLE-1KG', 'MILK-1L', 'BREAD-WH', 'CHEESE-200G', 'YOGURT-500G'].forEach(sku => {
        cy.contains(sku).should('be.visible');
      });
    });
  });

  // ── David (ORD-002): OutForDelivery ──────────────────────────────────────────

  context('David Wilson — OutForDelivery with package', () => {
    beforeEach(() => {
      cy.contains('David Wilson').parents('[data-testid^="order-card-"]')
        .find('button').first().click();
    });

    it('shows OutForDelivery status', () => {
      cy.contains('OutForDelivery').should('be.visible');
    });

    it('shows package tracking ID TRK-E2E-ORD-002', () => {
      cy.contains('TRK-E2E-ORD-002').should('be.visible');
    });

    it('shows delivery slot', () => {
      cy.contains('Delivery Slot').should('be.visible');
    });

    it('lists 2 order lines', () => {
      cy.get('table tbody tr').should('have.length', 2);
    });
  });

  // ── Frank (ORD-003): OnHold ───────────────────────────────────────────────────

  context('Frank Lee — OnHold, PackageDamaged', () => {
    beforeEach(() => {
      cy.contains('Frank Lee').parents('[data-testid^="order-card-"]')
        .find('button').first().click();
    });

    it('shows hold reason PackageDamaged', () => {
      cy.contains('PackageDamaged').should('be.visible');
    });

    it('shows Release Hold button', () => {
      cy.contains('Release Hold').should('be.visible');
    });
  });

  // ── Navigation ───────────────────────────────────────────────────────────────

  it('returns to board via Back button', () => {
    cy.contains('Alice Johnson').parents('[data-testid^="order-card-"]')
      .find('button').first().click();
    cy.contains('← Back to Board').click();
    cy.contains('Order Board').should('be.visible');
  });
});
