/**
 * 04-stock-flow.cy.js
 *
 * Seeds stock movement data via the API, then verifies the ledger endpoint
 * and that the Stock Flow UI tab renders.
 *
 * Stock ledger generation:
 *   The GET /stock/{sku}/ledger endpoint derives ledger entries from orders
 *   that have PickedAmount > 0 for the requested SKU. Creating an order and
 *   advancing it to PickConfirmed is sufficient to populate the ledger.
 */

describe('Stock Flow', () => {
  const SKU = 'APPLE-1KG';
  let orderId;
  let lineId;

  before(() => {
    cy.resetStore();

    // Create a PO for APPLE-1KG at store-001, then receive and put away so
    // the stock ledger derives a real PurchaseOrderPutAwayConfirmed inbound event.
    cy.omsApi('POST', '/inbound/purchase-orders', {
      poNumber:   'PO-STOCK-001',
      supplierId: 'SUP-001',
      supplier:   'Fresh Foods Ltd',
      storeId:    'store-001',
      store:      'Central DC',
      lines: [{ sku: SKU, orderedQty: 20, unitCost: 80, currency: 'THB' }],
    }).then((res) => {
      expect(res.status).to.eq(201);
      const poId = res.body.id;

      cy.omsApi('POST', '/webhooks/wms/goods-receipt-confirmed', {
        purchaseOrderId: poId,
        goodsReceiveNo:  'GRN-STOCK-001',
        lines: [{ sku: SKU, receivedQty: 20, condition: 'Resellable', sloc: 'A-12' }],
        receivedAt: new Date().toISOString(),
      }).its('status').should('eq', 202);

      cy.omsApi('POST', '/webhooks/wms/purchase-order-put-away-confirmed', {
        purchaseOrderId: poId,
        items: [{ sku: SKU, condition: 'Resellable', sloc: 'A-12', qty: 20 }],
        putAwayAt: new Date().toISOString(),
      }).its('status').should('eq', 202);
    });

    // Create an order containing APPLE-1KG, then advance it to PickConfirmed.
    // PickedAmount > 0 on the line is the trigger for the stock ledger to
    // include a PickConfirmed outbound event.
    cy.createOrder({
      channelType:  'Web',
      businessUnit: 'CMG',
      storeId:      'store-001',
      lines: [{
        sku:           SKU,
        productName:   'Apple (1 kg bag)',
        barcode:       '8850001001',
        requestedQty:  3,
        unitPrice:     120,
        unitOfMeasure: 'Unit',
      }],
    }).then((order) => {
      orderId = order.id;
      lineId  = order.lines[0].id;

      cy.omsApi('POST', '/webhooks/wms/pick-started', {
        orderId, pickerId: 'PICKER-01', startedAt: new Date().toISOString(),
      }).its('status').should('eq', 202);

      cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
        orderId,
        lines: [{ orderLineId: lineId, sku: SKU, pickedQty: 3, substituted: false }],
        pickedAt: new Date().toISOString(),
      }).its('status').should('eq', 202);
    });
  });

  // ── API: stock ledger ────────────────────────────────────────────────────────

  it('GET /stock/{sku}/ledger returns 200 for a picked SKU', () => {
    cy.omsApi('GET', `/stock/${SKU}/ledger`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.sku).to.eq(SKU);
      expect(res.body.locations).to.have.length.gte(1);
    });
  });

  it('ledger contains a PurchaseOrderPutAwayConfirmed inbound event', () => {
    cy.omsApi('GET', `/stock/${SKU}/ledger`).then((res) => {
      const events = res.body.locations.flatMap((loc) => loc.events);
      const inbound = events.find((e) => e.event === 'PurchaseOrderPutAwayConfirmed');
      expect(inbound, 'PurchaseOrderPutAwayConfirmed event').to.exist;
      expect(inbound.dir).to.eq('in');
      expect(inbound.qtyChange).to.be.gt(0);
    });
  });

  it('ledger contains a PickConfirmed outbound event for the seeded order', () => {
    cy.omsApi('GET', `/stock/${SKU}/ledger`).then((res) => {
      const events = res.body.locations.flatMap((loc) => loc.events);
      const pick = events.find((e) => e.event === 'PickConfirmed');
      expect(pick, 'PickConfirmed event').to.exist;
      expect(pick.dir).to.eq('out');
      expect(pick.qtyChange).to.be.lt(0);
    });
  });

  it('ledger balance reflects inbound minus picked quantity', () => {
    cy.omsApi('GET', `/stock/${SKU}/ledger`).then((res) => {
      const loc = res.body.locations[0];
      expect(loc.balance).to.be.gte(0);
      const events    = loc.events;
      const totalIn   = events.filter(e => e.dir === 'in').reduce((s, e) => s + e.qtyChange, 0);
      const totalOut  = events.filter(e => e.dir === 'out').reduce((s, e) => s + Math.abs(e.qtyChange), 0);
      expect(loc.balance).to.eq(totalIn - totalOut);
    });
  });

  it('GET /stock/{sku}/ledger returns 404 for an unknown SKU', () => {
    cy.omsApi('GET', '/stock/UNKNOWN-SKU-XYZ/ledger').then((res) => {
      expect(res.status).to.eq(404);
    });
  });

  // ── UI smoke test ────────────────────────────────────────────────────────────

  it('Stock Flow tab renders the view', () => {
    cy.visit('/');
    cy.contains('button', 'Stock Flow').click();
    cy.contains('Stock Flow').should('be.visible');
  });

  it('Stock Flow view shows at least one case scenario', () => {
    cy.visit('/');
    cy.contains('button', 'Stock Flow').click();
    cy.contains('Case 1').should('be.visible');
  });
});
