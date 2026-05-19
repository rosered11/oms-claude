const API = () => Cypress.env('apiBase');

// ─── Low-level helper ────────────────────────────────────────────────────────

Cypress.Commands.add('omsApi', (method, path, body) => {
  const opts = { method, url: `${API()}${path}`, failOnStatusCode: false };
  if (body !== undefined) opts.body = body;
  return cy.request(opts);
});

// ─── Store reset ──────────────────────────────────────────────────────────────
// After reset, NextId starts from ORD-001, PO-001, TR-001, etc. again.

Cypress.Commands.add('resetStore', () => {
  cy.request('POST', `${API()}/test/reset`).its('status').should('eq', 200);
});

// ─── State transition helpers (direct cy.request calls) ───────────────────────

Cypress.Commands.add('wmsPickStarted', (orderId) => {
  cy.request('POST', `${API()}/webhooks/wms/pick-started`, {
    orderId, pickerId: 'PICKER-01', startedAt: new Date().toISOString(),
  }).its('status').should('eq', 202);
});

Cypress.Commands.add('wmsPickConfirmed', (orderId, lines) => {
  cy.request('POST', `${API()}/webhooks/wms/pick-confirmed`, {
    orderId, lines, pickedAt: new Date().toISOString(),
  }).its('status').should('eq', 202);
});

Cypress.Commands.add('wmsPacked', (orderId, trackingId, lineIds) => {
  cy.request('POST', `${API()}/webhooks/wms/packed`, {
    orderId,
    packages: [{ trackingId, vehicleType: 'Van', weight: 1.0, lineIds }],
    packedAt: new Date().toISOString(),
  }).its('status').should('eq', 202);
});

Cypress.Commands.add('tmsDispatched', (trackingId) => {
  cy.request('POST', `${API()}/webhooks/tms/package-dispatched`, {
    trackingId, dispatchedAt: new Date().toISOString(),
  }).its('status').should('eq', 202);
});

Cypress.Commands.add('tmsDelivered', (trackingId, recipientName = 'Customer') => {
  cy.request('POST', `${API()}/webhooks/tms/package-delivered`, {
    trackingId, deliveredAt: new Date().toISOString(), recipientName,
  }).its('status').should('eq', 202);
});

Cypress.Commands.add('holdOrder', (orderId, holdReason = 'OperationalIssue') => {
  cy.request('PATCH', `${API()}/orders/${orderId}/hold`, {
    holdReason, heldBy: 'e2e-test',
  }).its('status').should('eq', 200);
});

Cypress.Commands.add('cancelOrder', (orderId) => {
  cy.request('PATCH', `${API()}/orders/${orderId}/cancel`, {
    reason: 'TestCancellation', cancelledBy: 'e2e-test',
  }).its('status').should('eq', 200);
});

// ─── Order creation ───────────────────────────────────────────────────────────

Cypress.Commands.add('createOrder', (overrides = {}) => {
  const slotStart = new Date(Date.now() + 3600000).toISOString();
  const slotEnd   = new Date(Date.now() + 7200000).toISOString();
  const payload = {
    sourceOrderId:   `SRC-${Date.now()}`,
    channelType:     'Web',
    businessUnit:    'CMG',
    storeId:         'STORE-001',
    fulfillmentType: 'Delivery',
    paymentMethod:   'Prepaid',
    paymentFlow:     'PRE_PAID',
    customerName:    'Test Customer',
    customerPhone:   '0812345678',
    customerEmail:   'test@example.com',
    deliverySlot:    { scheduledStart: slotStart, scheduledEnd: slotEnd },
    lines: [{
      sku: 'APPLE-1KG', productName: 'Apple 1 kg', barcode: '8851234560001',
      requestedQty: 2, unitPrice: 120, unitOfMeasure: 'Unit',
    }],
    ...overrides,
  };
  return cy.request('POST', `${API()}/orders`, payload).then(res => {
    expect(res.status).to.eq(201);
    return res.body;
  });
});

// ─── advanceOrder ─────────────────────────────────────────────────────────────
// Drives an order to targetStatus using pre-known line IDs (LINE-001, etc.).
// orderId and lineCount must be known before calling.

Cypress.Commands.add('advanceOrder', (orderId, targetStatus, { lineCount = 1, trackingId, holdReason, holdAfter, customerName = 'Customer' } = {}) => {
  const trkId    = trackingId || `TRK-E2E-${orderId}`;
  const lineIds  = Array.from({ length: lineCount }, (_, i) => `LINE-${String(i + 1).padStart(3, '0')}`);
  const pickLines = lineIds.map(id => ({ orderLineId: id, sku: 'SKU', pickedQty: 1, substituted: false }));
  const advanceTo = holdAfter || targetStatus;
  const path      = ['PickStarted', 'PickConfirmed', 'Packed', 'OutForDelivery', 'Delivered'];
  const stopAt    = path.indexOf(advanceTo);

  for (let i = 0; i <= stopAt; i++) {
    if (path[i] === 'PickStarted')    cy.wmsPickStarted(orderId);
    if (path[i] === 'PickConfirmed')  cy.wmsPickConfirmed(orderId, pickLines);
    if (path[i] === 'Packed')         cy.wmsPacked(orderId, trkId, lineIds);
    if (path[i] === 'OutForDelivery') cy.tmsDispatched(trkId);
    if (path[i] === 'Delivered')      cy.tmsDelivered(trkId, customerName);
  }
  if (targetStatus === 'OnHold')   cy.holdOrder(orderId, holdReason || 'OperationalIssue');
  if (targetStatus === 'Cancelled') cy.cancelOrder(orderId);
});

// ─── seedKanbanBoard ──────────────────────────────────────────────────────────
// Creates all 14 test orders and advances them to their target status.
//
// KEY: after cy.resetStore(), NextId always generates ORD-001 … ORD-014 in
// creation order, and line IDs within each order are always LINE-001, LINE-002…
// so we can pre-compute every ID without needing .then() callbacks.

const KANBAN_SEEDS = [
  // ── 1 ─ Alice  ─ Pending ────────────────────────────────────────────────────
  {
    targetStatus: 'Pending', lineCount: 5,
    payload: {
      customerName: 'Alice Johnson', customerPhone: '0810000001',
      channelType: 'App', businessUnit: 'TOPS', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-ALICE-001',
      deliverySlot: { scheduledStart: '2024-01-15T18:00:00Z', scheduledEnd: '2024-01-15T20:00:00Z' },
      lines: [
        { sku: 'APPLE-1KG',   productName: 'Apple (1 kg bag)',    barcode: '8850001001', requestedQty: 2, unitPrice: 120,  unitOfMeasure: 'Unit' },
        { sku: 'MILK-1L',     productName: 'Whole Milk 1L',       barcode: '8850001002', requestedQty: 3, unitPrice: 65,   unitOfMeasure: 'Unit' },
        { sku: 'BREAD-WH',    productName: 'Whole Wheat Bread',   barcode: '8850001003', requestedQty: 1, unitPrice: 45,   unitOfMeasure: 'Unit' },
        { sku: 'CHEESE-200G', productName: 'Cheddar Cheese 200g', barcode: '8850001004', requestedQty: 2, unitPrice: 185,  unitOfMeasure: 'Unit' },
        { sku: 'YOGURT-500G', productName: 'Greek Yogurt 500g',   barcode: '8850001005', requestedQty: 1, unitPrice: 89,   unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 2 ─ Bob  ─ PickStarted ──────────────────────────────────────────────────
  {
    targetStatus: 'PickStarted', lineCount: 2,
    payload: {
      customerName: 'Bob Smith', customerPhone: '0810000002',
      channelType: 'App', businessUnit: 'CFH', storeId: 'store-002',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-BOB-001',
      lines: [
        { sku: 'ORANGE-1KG',  productName: 'Orange (1 kg bag)',    barcode: '8850002001', requestedQty: 1, unitPrice: 110, unitOfMeasure: 'Unit' },
        { sku: 'BUTTER-250G', productName: 'Unsalted Butter 250g', barcode: '8850002002', requestedQty: 1, unitPrice: 89,  unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 3 ─ Carol  ─ Packed ─────────────────────────────────────────────────────
  {
    targetStatus: 'Packed', lineCount: 2,
    payload: {
      customerName: 'Carol Davis', customerPhone: '0810000003',
      channelType: 'Web', businessUnit: 'TOPS', storeId: 'store-003',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-CAROL-001',
      lines: [
        { sku: 'PASTA-500G', productName: 'Spaghetti 500g',    barcode: '8850003002', requestedQty: 2, unitPrice: 55, unitOfMeasure: 'Unit' },
        { sku: 'SAUCE-350G', productName: 'Tomato Sauce 350g', barcode: '8850003003', requestedQty: 3, unitPrice: 72, unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 4 ─ David  ─ OutForDelivery ─────────────────────────────────────────────
  {
    targetStatus: 'OutForDelivery', lineCount: 3,
    payload: {
      customerName: 'David Wilson', customerPhone: '0810000004',
      channelType: 'App', businessUnit: 'CMG', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PayOnDelivery', paymentFlow: 'PAY_ON_DELIVERY',
      sourceOrderId: 'SRC-DAVID-001',
      deliverySlot: { scheduledStart: '2024-01-15T15:00:00Z', scheduledEnd: '2024-01-15T17:00:00Z' },
      lines: [
        { sku: 'BEEF-1KG',   productName: 'Ground Beef 1kg',  barcode: '8850004001', requestedQty: 1, unitPrice: 320, unitOfMeasure: 'Unit' },
        { sku: 'POTATO-2KG', productName: 'Potato 2kg bag',   barcode: '8850004002', requestedQty: 1, unitPrice: 89,  unitOfMeasure: 'Unit' },
        { sku: 'OIL-1L',     productName: 'Sunflower Oil 1L', barcode: '8850004003', requestedQty: 1, unitPrice: 75,  unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 5 ─ Eve  ─ Delivered ────────────────────────────────────────────────────
  {
    targetStatus: 'Delivered', lineCount: 1,
    payload: {
      customerName: 'Eve Martinez', customerPhone: '0810000005',
      channelType: 'App', businessUnit: 'CFH', storeId: 'store-002',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-EVE-001',
      lines: [
        { sku: 'WINE-750ML', productName: 'Red Wine 750ml', barcode: '8850005001', requestedQty: 1, unitPrice: 750, unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 6 ─ Frank  ─ OnHold (preHoldStatus: OutForDelivery) ─────────────────────
  {
    targetStatus: 'OnHold', holdAfter: 'OutForDelivery', holdReason: 'PackageDamaged', lineCount: 2,
    payload: {
      customerName: 'Frank Lee', customerPhone: '0810000006',
      channelType: 'App', businessUnit: 'TOPS', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-FRANK-001',
      lines: [
        { sku: 'TV-55IN', productName: '55-inch Smart TV', barcode: '8850006001', requestedQty: 1, unitPrice: 4500, unitOfMeasure: 'Unit' },
        { sku: 'HDMI-2M', productName: 'HDMI Cable 2m',    barcode: '8850006002', requestedQty: 2, unitPrice: 199,  unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 7 ─ Grace  ─ Packed ─────────────────────────────────────────────────────
  {
    targetStatus: 'Packed', lineCount: 2,
    payload: {
      customerName: 'Grace Kim', customerPhone: '0810000007',
      channelType: 'App', businessUnit: 'TOPS', storeId: 'store-004',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-GRACE-001',
      lines: [
        { sku: 'COFFEE-250G', productName: 'Ground Coffee 250g', barcode: '8850007001', requestedQty: 2, unitPrice: 320, unitOfMeasure: 'Unit' },
        { sku: 'SUGAR-1KG',   productName: 'White Sugar 1kg',    barcode: '8850007002', requestedQty: 2, unitPrice: 35,  unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 8 ─ Henry  ─ Delivered ──────────────────────────────────────────────────
  {
    targetStatus: 'Delivered', lineCount: 1,
    payload: {
      customerName: 'Henry Park', customerPhone: '0810000008',
      channelType: 'Web', businessUnit: 'CMG', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-HENRY-001',
      lines: [
        { sku: 'SHAMPOO-400ML', productName: 'Shampoo 400ml', barcode: '8850008001', requestedQty: 2, unitPrice: 185, unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 9 ─ Iris  ─ PickConfirmed ───────────────────────────────────────────────
  {
    targetStatus: 'PickConfirmed', lineCount: 2,
    payload: {
      customerName: 'Iris Chen', customerPhone: '0810000009',
      channelType: 'App', businessUnit: 'CFH', storeId: 'store-003',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-IRIS-001',
      deliverySlot: { scheduledStart: '2024-01-15T18:00:00Z', scheduledEnd: '2024-01-15T20:00:00Z' },
      lines: [
        { sku: 'RICE-5KG',  productName: 'Jasmine Rice 5kg',    barcode: '8850009002', requestedQty: 1, unitPrice: 189, unitOfMeasure: 'Unit' },
        { sku: 'COCONUT-M', productName: 'Coconut Milk 400ml',  barcode: '8850009004', requestedQty: 2, unitPrice: 45,  unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 10 ─ James  ─ Delivered ─────────────────────────────────────────────────
  {
    targetStatus: 'Delivered', lineCount: 2,
    payload: {
      customerName: 'James Taylor', customerPhone: '0810000010',
      channelType: 'Web', businessUnit: 'TOPS', storeId: 'store-002',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-JAMES-001',
      lines: [
        { sku: 'WHISKEY-700', productName: 'Whiskey 700ml',        barcode: '8850010001', requestedQty: 1, unitPrice: 950, unitOfMeasure: 'Unit' },
        { sku: 'SODA-330ML',  productName: 'Soda Water 330ml x6', barcode: '8850010002', requestedQty: 1, unitPrice: 129, unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 11 ─ Kate  ─ Pending ────────────────────────────────────────────────────
  {
    targetStatus: 'Pending', lineCount: 2,
    payload: {
      customerName: 'Kate Brown', customerPhone: '0810000011',
      channelType: 'App', businessUnit: 'CMG', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PayOnDelivery', paymentFlow: 'PAY_ON_DELIVERY',
      sourceOrderId: 'SRC-KATE-001',
      deliverySlot: { scheduledStart: '2024-01-15T20:00:00Z', scheduledEnd: '2024-01-15T22:00:00Z' },
      lines: [
        { sku: 'SOFA-3SEAT',  productName: '3-Seat Fabric Sofa', barcode: '8850011001', requestedQty: 1, unitPrice: 3500, unitOfMeasure: 'Unit' },
        { sku: 'CUSHION-SET', productName: 'Cushion Set (4pcs)',  barcode: '8850011002', requestedQty: 1, unitPrice: 350,  unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 12 ─ Leo  ─ Cancelled ───────────────────────────────────────────────────
  {
    targetStatus: 'Cancelled', lineCount: 1,
    payload: {
      customerName: 'Leo Nguyen', customerPhone: '0810000012',
      channelType: 'App', businessUnit: 'CFH', storeId: 'store-004',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-LEO-001',
      lines: [
        { sku: 'ICECREAM-1L', productName: 'Ice Cream 1L Tub', barcode: '8850012001', requestedQty: 1, unitPrice: 340, unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 13 ─ Mia  ─ OutForDelivery ──────────────────────────────────────────────
  {
    targetStatus: 'OutForDelivery', lineCount: 2,
    payload: {
      customerName: 'Mia Patel', customerPhone: '0810000013',
      channelType: 'Web', businessUnit: 'TOPS', storeId: 'store-001',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-MIA-001',
      deliverySlot: { scheduledStart: '2024-01-15T13:00:00Z', scheduledEnd: '2024-01-15T15:00:00Z' },
      lines: [
        { sku: 'WARDROBE-2D', productName: '2-Door Wardrobe', barcode: '8850013001', requestedQty: 1, unitPrice: 4200, unitOfMeasure: 'Unit' },
        { sku: 'BED-QUEEN',   productName: 'Queen Bed Frame',  barcode: '8850013002', requestedQty: 1, unitPrice: 2600, unitOfMeasure: 'Unit' },
      ],
    },
  },
  // ── 14 ─ Noah  ─ Delivered ──────────────────────────────────────────────────
  {
    targetStatus: 'Delivered', lineCount: 1,
    payload: {
      customerName: 'Noah Kim', customerPhone: '0810000014',
      channelType: 'App', businessUnit: 'CMG', storeId: 'store-003',
      fulfillmentType: 'Delivery', paymentMethod: 'PrePaid', paymentFlow: 'PRE_PAID',
      sourceOrderId: 'SRC-NOAH-001',
      lines: [
        { sku: 'SNACK-MIX', productName: 'Snack Mix Pack', barcode: '8850014001', requestedQty: 4, unitPrice: 89, unitOfMeasure: 'Unit' },
      ],
    },
  },
];

Cypress.Commands.add('seedKanbanBoard', () => {
  cy.resetStore();

  KANBAN_SEEDS.forEach((seed, idx) => {
    // IDs are deterministic after reset: position 0 → ORD-001, 1 → ORD-002, …
    const orderId    = `ORD-${String(idx + 1).padStart(3, '0')}`;
    const trackingId = `TRK-E2E-${orderId}`;
    const lineIds    = Array.from({ length: seed.lineCount }, (_, i) => `LINE-${String(i + 1).padStart(3, '0')}`);
    const pickLines  = lineIds.map(id => ({ orderLineId: id, sku: 'SKU', pickedQty: 1, substituted: false }));
    const advanceTo  = seed.holdAfter || seed.targetStatus;
    const path       = ['PickStarted', 'PickConfirmed', 'Packed', 'OutForDelivery', 'Delivered'];
    const stopAt     = path.indexOf(advanceTo);

    // 1. Create
    cy.request('POST', `${API()}/orders`, seed.payload).its('status').should('eq', 201);

    // 2. Advance — flat cy.request() calls; Cypress queues them sequentially
    for (let i = 0; i <= stopAt; i++) {
      if (path[i] === 'PickStarted')
        cy.request('POST', `${API()}/webhooks/wms/pick-started`,
          { orderId, pickerId: 'PICKER-01', startedAt: new Date().toISOString() })
          .its('status').should('eq', 202);
      if (path[i] === 'PickConfirmed')
        cy.request('POST', `${API()}/webhooks/wms/pick-confirmed`,
          { orderId, lines: pickLines, pickedAt: new Date().toISOString() })
          .its('status').should('eq', 202);
      if (path[i] === 'Packed')
        cy.request('POST', `${API()}/webhooks/wms/packed`,
          { orderId, packages: [{ trackingId, vehicleType: 'Van', weight: 1.0, lineIds }], packedAt: new Date().toISOString() })
          .its('status').should('eq', 202);
      if (path[i] === 'OutForDelivery')
        cy.request('POST', `${API()}/webhooks/tms/package-dispatched`,
          { trackingId, dispatchedAt: new Date().toISOString() })
          .its('status').should('eq', 202);
      if (path[i] === 'Delivered')
        cy.request('POST', `${API()}/webhooks/tms/package-delivered`,
          { trackingId, deliveredAt: new Date().toISOString(), recipientName: seed.payload.customerName })
          .its('status').should('eq', 202);
    }

    // 3. Terminal transitions
    if (seed.targetStatus === 'OnHold')
      cy.request('PATCH', `${API()}/orders/${orderId}/hold`,
        { holdReason: seed.holdReason || 'OperationalIssue', heldBy: 'e2e-test' })
        .its('status').should('eq', 200);
    if (seed.targetStatus === 'Cancelled')
      cy.request('PATCH', `${API()}/orders/${orderId}/cancel`,
        { reason: 'TestCancellation', cancelledBy: 'e2e-test' })
        .its('status').should('eq', 200);
  });
});

// ─── seedInboundData ──────────────────────────────────────────────────────────
// Creates 4 Purchase Orders and 3 Transfer Orders via the API, advancing each
// to the appropriate status (Closed, FullyReceived, Created; Completed, PickConfirmed).

const PO_SEEDS = [
  // PO-001: Fresh Foods Ltd — fully received and put away → Closed
  {
    create: {
      poNumber: 'PO-2024-001', supplierId: 'SUP-001', supplier: 'Fresh Foods Ltd',
      storeId: 'store-001', store: 'Central DC',
      lines: [
        { sku: 'APPLE-1KG',  orderedQty: 100, unitCost: 80, currency: 'THB' },
        { sku: 'ORANGE-1KG', orderedQty: 80,  unitCost: 75, currency: 'THB' },
        { sku: 'BANANA-1KG', orderedQty: 60,  unitCost: 60, currency: 'THB' },
      ],
    },
    receipt: {
      goodsReceiveNo: 'GRN-2024-001',
      lines: [
        { sku: 'APPLE-1KG',  receivedQty: 100, condition: 'Resellable', sloc: 'A-12' },
        { sku: 'ORANGE-1KG', receivedQty: 80,  condition: 'Resellable', sloc: 'A-13' },
        { sku: 'BANANA-1KG', receivedQty: 60,  condition: 'Resellable', sloc: 'A-14' },
      ],
    },
    putAway: {
      items: [
        { sku: 'APPLE-1KG',  condition: 'Resellable', sloc: 'A-12', qty: 100 },
        { sku: 'ORANGE-1KG', condition: 'Resellable', sloc: 'A-13', qty: 80  },
        { sku: 'BANANA-1KG', condition: 'Resellable', sloc: 'A-14', qty: 60  },
      ],
    },
  },
  // PO-002: Thai Dairy Co — MILK partially received, YOGURT not yet received → FullyReceived
  {
    create: {
      poNumber: 'PO-2024-002', supplierId: 'SUP-002', supplier: 'Thai Dairy Co',
      storeId: 'store-001', store: 'Central DC',
      lines: [
        { sku: 'MILK-1L',     orderedQty: 200, unitCost: 35, currency: 'THB' },
        { sku: 'YOGURT-500G', orderedQty: 100, unitCost: 50, currency: 'THB' },
      ],
    },
    receipt: {
      goodsReceiveNo: 'GRN-2024-002',
      lines: [{ sku: 'MILK-1L', receivedQty: 120, condition: 'Resellable', sloc: 'B-01' }],
    },
  },
  // PO-003: Organic Farms Ltd — created only, goods not yet received
  {
    create: {
      poNumber: 'PO-2024-003', supplierId: 'SUP-003', supplier: 'Organic Farms Ltd',
      storeId: 'store-002', store: 'Store A',
      lines: [
        { sku: 'SPINACH-250G', orderedQty: 150, unitCost: 25, currency: 'THB' },
        { sku: 'KALE-250G',    orderedQty: 150, unitCost: 15, currency: 'THB' },
      ],
    },
  },
  // PO-004: Fresh Foods Ltd — fully received and put away → Closed
  {
    create: {
      poNumber: 'PO-2024-004', supplierId: 'SUP-001', supplier: 'Fresh Foods Ltd',
      storeId: 'store-001', store: 'Central DC',
      lines: [{ sku: 'APPLE-1KG', orderedQty: 120, unitCost: 80, currency: 'THB' }],
    },
    receipt: {
      goodsReceiveNo: 'GRN-2024-000',
      lines: [{ sku: 'APPLE-1KG', receivedQty: 120, condition: 'Resellable', sloc: 'A-12' }],
    },
    putAway: {
      items: [{ sku: 'APPLE-1KG', condition: 'Resellable', sloc: 'A-12', qty: 120 }],
    },
  },
];

const TO_SEEDS = [
  // TO-001: Central DC → Store A, APPLE + ORANGE — Completed
  {
    create: {
      sourceStoreId: 'store-001', source: 'Central DC',
      destStoreId: 'store-002',   dest: 'Store A',
      lines: [{ sku: 'APPLE-1KG', requestedQty: 4 }, { sku: 'ORANGE-1KG', requestedQty: 3 }],
    },
    pickConfirm: { lines: [{ sku: 'APPLE-1KG', transferredQty: 4 }, { sku: 'ORANGE-1KG', transferredQty: 3 }] },
    receive: true,
  },
  // TO-002: Central DC → Store B, MILK — PickConfirmed
  {
    create: {
      sourceStoreId: 'store-001', source: 'Central DC',
      destStoreId: 'store-003',   dest: 'Store B',
      lines: [{ sku: 'MILK-1L', requestedQty: 50 }],
    },
    pickConfirm: { lines: [{ sku: 'MILK-1L', transferredQty: 50 }] },
  },
  // TO-003: Store A → Store B, APPLE + BANANA — PickConfirmed
  {
    create: {
      sourceStoreId: 'store-002', source: 'Store A',
      destStoreId: 'store-003',   dest: 'Store B',
      lines: [{ sku: 'APPLE-1KG', requestedQty: 10 }, { sku: 'BANANA-1KG', requestedQty: 8 }],
    },
    pickConfirm: { lines: [{ sku: 'APPLE-1KG', transferredQty: 10 }, { sku: 'BANANA-1KG', transferredQty: 8 }] },
  },
];

Cypress.Commands.add('seedInboundData', () => {
  PO_SEEDS.forEach((seed) => {
    cy.request('POST', `${API()}/inbound/purchase-orders`, seed.create)
      .then((res) => {
        expect(res.status).to.eq(201);
        const poId = res.body.id;
        if (seed.receipt) {
          cy.request('POST', `${API()}/webhooks/wms/goods-receipt-confirmed`, {
            purchaseOrderId: poId,
            goodsReceiveNo:  seed.receipt.goodsReceiveNo,
            lines:           seed.receipt.lines,
            receivedAt:      new Date().toISOString(),
          }).its('status').should('eq', 202);
        }
        if (seed.putAway) {
          cy.request('POST', `${API()}/webhooks/wms/purchase-order-put-away-confirmed`, {
            purchaseOrderId: poId,
            items:           seed.putAway.items,
            putAwayAt:       new Date().toISOString(),
          }).its('status').should('eq', 202);
        }
      });
  });

  TO_SEEDS.forEach((seed) => {
    cy.request('POST', `${API()}/inbound/transfer-orders`, seed.create)
      .then((res) => {
        expect(res.status).to.eq(201);
        const toId = res.body.id;
        if (seed.pickConfirm) {
          cy.request('POST', `${API()}/webhooks/wms/transfer-pick-confirmed`, {
            transferOrderId: toId,
            lines:           seed.pickConfirm.lines,
            confirmedAt:     new Date().toISOString(),
          }).its('status').should('eq', 202);
        }
        if (seed.receive) {
          cy.request('POST', `${API()}/webhooks/wms/transfer-received`, {
            transferOrderId: toId,
            receivedAt:      new Date().toISOString(),
          }).its('status').should('eq', 202);
        }
      });
  });
});

// ─── seedReturnsData ──────────────────────────────────────────────────────────
// Creates 3 Delivered orders then initiates returns on them:
//   RET-1: APPLE-1KG — QualityIssue → put-away-confirmed (PutAway + refund)
//   RET-2: MILK-1L + YOGURT-500G — WrongItem → put-away-confirmed (PutAway + refund)
//   RET-3: SPINACH-250G — NotFresh → Requested only (not yet put away)

Cypress.Commands.add('seedReturnsData', () => {
  const now = () => new Date().toISOString();

  // ── Return 1: APPLE-1KG, QualityIssue → PutAway ──────────────────────────────
  cy.createOrder({
    channelType: 'App', businessUnit: 'CMG',
    lines: [{
      sku: 'APPLE-1KG', productName: 'Apple (1 kg bag)', barcode: '8850001001',
      requestedQty: 5, unitPrice: 120, unitOfMeasure: 'Unit',
    }],
  }).then((order) => {
    const orderId = order.id;
    const lineId  = order.lines[0].id;
    const trkId   = `TRK-RET-A-${orderId}`;
    cy.wmsPickStarted(orderId);
    cy.wmsPickConfirmed(orderId, [{ orderLineId: lineId, sku: 'APPLE-1KG', pickedQty: 5, substituted: false }]);
    cy.wmsPacked(orderId, trkId, [lineId]);
    cy.tmsDispatched(trkId);
    cy.tmsDelivered(trkId, 'Alice Johnson');
    cy.request('POST', `${API()}/returns`, {
      orderId, returnReason: 'QualityIssue',
      items: [
        { orderLineId: lineId, sku: 'APPLE-1KG', quantity: 2, itemReason: 'DamagedOnArrival' },
        { orderLineId: lineId, sku: 'APPLE-1KG', quantity: 1, itemReason: 'Spoiled' },
      ],
      requestedBy: 'customer-portal',
    }).then((retRes) => {
      expect(retRes.status).to.eq(201);
      cy.request('POST', `${API()}/webhooks/wms/put-away-confirmed`, {
        returnId: retRes.body.id,
        items: [
          { sku: 'APPLE-1KG', condition: 'Resellable', sloc: 'A-12', quantity: 2, performedBy: 'wms-staff' },
          { sku: 'APPLE-1KG', condition: 'Dispose',    sloc: null,   quantity: 1, performedBy: 'wms-staff' },
        ],
        putAwayAt: now(),
      }).its('status').should('eq', 202);
    });
  });

  // ── Return 2: MILK-1L + YOGURT-500G, WrongItem → PutAway ─────────────────────
  cy.createOrder({
    channelType: 'App', businessUnit: 'CMG',
    lines: [
      { sku: 'MILK-1L',     productName: 'Fresh Milk 1L',     barcode: '8850002001', requestedQty: 3, unitPrice: 45, unitOfMeasure: 'Unit' },
      { sku: 'YOGURT-500G', productName: 'Greek Yogurt 500g', barcode: '8850002002', requestedQty: 2, unitPrice: 65, unitOfMeasure: 'Unit' },
    ],
  }).then((order) => {
    const orderId      = order.id;
    const milkLineId   = order.lines[0].id;
    const yogurtLineId = order.lines[1].id;
    const trkId        = `TRK-RET-B-${orderId}`;
    cy.wmsPickStarted(orderId);
    cy.wmsPickConfirmed(orderId, [
      { orderLineId: milkLineId,   sku: 'MILK-1L',     pickedQty: 3, substituted: false },
      { orderLineId: yogurtLineId, sku: 'YOGURT-500G', pickedQty: 2, substituted: false },
    ]);
    cy.wmsPacked(orderId, trkId, [milkLineId, yogurtLineId]);
    cy.tmsDispatched(trkId);
    cy.tmsDelivered(trkId, 'Bob Smith');
    cy.request('POST', `${API()}/returns`, {
      orderId, returnReason: 'WrongItem',
      items: [
        { orderLineId: milkLineId,   sku: 'MILK-1L',     quantity: 3, itemReason: 'WrongItem' },
        { orderLineId: yogurtLineId, sku: 'YOGURT-500G', quantity: 2, itemReason: 'WrongItem' },
      ],
      requestedBy: 'customer-portal',
    }).then((retRes) => {
      expect(retRes.status).to.eq(201);
      cy.request('POST', `${API()}/webhooks/wms/put-away-confirmed`, {
        returnId: retRes.body.id,
        items: [
          { sku: 'MILK-1L',     condition: 'Resellable', sloc: 'B-01', quantity: 3, performedBy: 'wms-staff' },
          { sku: 'YOGURT-500G', condition: 'Repairable', sloc: 'B-02', quantity: 2, performedBy: 'wms-staff' },
        ],
        putAwayAt: now(),
      }).its('status').should('eq', 202);
    });
  });

  // ── Return 3: SPINACH-250G, NotFresh → Requested only ────────────────────────
  cy.createOrder({
    channelType: 'Web', businessUnit: 'CFR',
    lines: [{
      sku: 'SPINACH-250G', productName: 'Fresh Spinach 250g', barcode: '8850003001',
      requestedQty: 2, unitPrice: 35, unitOfMeasure: 'Unit',
    }],
  }).then((order) => {
    const orderId = order.id;
    const lineId  = order.lines[0].id;
    const trkId   = `TRK-RET-C-${orderId}`;
    cy.wmsPickStarted(orderId);
    cy.wmsPickConfirmed(orderId, [{ orderLineId: lineId, sku: 'SPINACH-250G', pickedQty: 2, substituted: false }]);
    cy.wmsPacked(orderId, trkId, [lineId]);
    cy.tmsDispatched(trkId);
    cy.tmsDelivered(trkId, 'Charlie Wong');
    cy.request('POST', `${API()}/returns`, {
      orderId, returnReason: 'NotFresh',
      items: [{ orderLineId: lineId, sku: 'SPINACH-250G', quantity: 2, itemReason: 'NotFresh' }],
      requestedBy: 'customer-portal',
    }).its('status').should('eq', 201);
  });
});
