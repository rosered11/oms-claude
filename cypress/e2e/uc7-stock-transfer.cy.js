/**
 * UC7 — Stock transfer from Store A to Store B
 *
 * Scenario: Stock A transfer order to Stock B
 *   POST /inbound/transfer-orders (sourceStoreId=STORE-A, destStoreId=STORE-B) →
 *   WMS pick-confirmed (transfer) → WMS transfer-received → Completed
 *
 * Key invariants exercised:
 *   - Transfer order lifecycle: Created → PickConfirmed → Completed
 *   - Transfer uses its own WMS webhook endpoints separate from order flow
 *   - transferOrderId is the cross-context reference (no direct aggregate coupling)
 */

describe('UC7 — Stock transfer from Store A to Store B', () => {
  let transferId;
  const now = () => new Date().toISOString();
  const trackingId = `TRK-UC7-${Date.now()}`;

  it('Step 1 — Creates a transfer order from STORE-A to STORE-B', () => {
    cy.omsApi('POST', '/inbound/transfer-orders', {
      sourceStoreId: 'STORE-A',
      destStoreId:   'STORE-B',
      lines: [
        {
          sku:          'WATER-1L',
          requestedQty: 6,
        },
      ],
    }).then((res) => {
      expect(res.status).to.eq(201);
      expect(res.body.status).to.eq('Created');
      transferId = res.body.id;
    });
  });

  it('Step 2 — GET transfer order; verifies status Created and at least one line', () => {
    cy.omsApi('GET', `/inbound/transfer-orders/${transferId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Created');
      expect(res.body.lines).to.have.length.at.least(1);
    });
  });

  it('Step 3 — WMS transfer-pick-confirmed transitions transfer order to PickConfirmed', () => {
    cy.omsApi('POST', '/webhooks/wms/transfer-pick-confirmed', {
      transferOrderId: transferId,
      lines: [
        {
          sku:           'WATER-1L',
          transferredQty: 6,
        },
      ],
      confirmedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 4 — WMS transfer-received transitions transfer order to Completed', () => {
    cy.omsApi('POST', '/webhooks/wms/transfer-received', {
      transferOrderId: transferId,
      receivedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Completed');
    });
  });

  it('Step 5 — Final state: transfer order is Completed', () => {
    cy.omsApi('GET', `/inbound/transfer-orders/${transferId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Completed');
    });
  });
});
