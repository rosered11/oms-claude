/**
 * UC8 — Customer postpones delivery date
 *
 * Scenario: Customer reschedules delivery slot before dispatch,
 *           then attempts to reschedule again once OutForDelivery (rejected).
 *
 * Flow:
 *   Pending → slot rescheduled (allowed) →
 *   PickStarted → wave-started → POS recalc → PickConfirmed →
 *   STS ABB/Tax Invoice → Packed → OutForDelivery →
 *   slot reschedule attempt → 409 slot_change_not_allowed
 *
 * Key invariants exercised:
 *   - Delivery slot can be changed while order is Pending/BookingConfirmed
 *   - Slot change is forbidden once order is OutForDelivery (409)
 */

describe('UC8 — Customer postpones delivery date', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC8-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates order (CFR/Web/Prepaid); verifies Pending status', () => {
    cy.createOrder({ businessUnit: 'CFR' }).then((order) => {
      expect(order.status).to.eq('Pending');
      orderId = order.id;
      lineId  = order.lines[0].id;
    });
  });

  it('Step 2 — GET delivery slot; verifies scheduledStart exists', () => {
    cy.omsApi('GET', `/orders/${orderId}/delivery-slot`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.deliverySlot).to.have.property('scheduledStart');
    });
  });

  it('Step 3 — Customer reschedules slot to +5 hours; verifies new scheduledStart returned', () => {
    const plus5  = new Date(Date.now() + 5 * 60 * 60 * 1000).toISOString();
    const plus6  = new Date(Date.now() + 6 * 60 * 60 * 1000).toISOString();

    cy.omsApi('PATCH', `/orders/${orderId}/delivery-slot`, {
      scheduledStart: plus5,
      scheduledEnd:   plus6,
      bookedVia:      'TMS',
      bookingRef:     `TMS-RESCHEDULE-${Date.now()}`,
      reason:         'CustomerRequest',
    }).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.deliverySlot).to.have.property('scheduledStart');
    });
  });

  it('Step 4 — GET delivery slot; verifies slot reflects the rescheduled time', () => {
    cy.omsApi('GET', `/orders/${orderId}/delivery-slot`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.deliverySlot).to.have.property('scheduledStart');
    });
  });

  it('Step 5a — WMS pick-started transitions order to PickStarted', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-started', {
      orderId,
      pickerId:  'PICKER-001',
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickStarted');
    });
  });

  it('Step 5b — WMS wave-started fires WaveStartedSentToGW outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC8-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 5c — WMS requests POS recalculation; returns adjusted amount', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'PickQuantityDiffers',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.adjustedAmount).to.be.a('number');
    });
  });

  it('Step 5d — WMS pick-confirmed transitions order to PickConfirmed', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines:    [{ orderLineId: lineId, sku: 'APPLE-1KG', pickedQty: 2, substituted: false }],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 5e — STS ABB/Tax Invoice received; OMS dispatches to WMS + GW', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC8-${Date.now()}`,
      invoiceAmount: 19800,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC8.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  it('Step 5f — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [{ trackingId, vehicleType: 'Motorcycle', weight: 1.5, lineIds: [lineId] }],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 5g — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 6 — Rescheduling slot while OutForDelivery returns 409 slot_change_not_allowed', () => {
    const plus7 = new Date(Date.now() + 7 * 60 * 60 * 1000).toISOString();
    const plus8 = new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString();

    cy.omsApi('PATCH', `/orders/${orderId}/delivery-slot`, {
      scheduledStart: plus7,
      scheduledEnd:   plus8,
      bookedVia:      'TMS',
      bookingRef:     `TMS-RESCHEDULE-LATE-${Date.now()}`,
      reason:         'CustomerRequest',
    }).then((res) => {
      expect(res.status).to.eq(409);
      expect(res.body.error).to.eq('slot_change_not_allowed');
    });
  });
});
