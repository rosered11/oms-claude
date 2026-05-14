/**
 * UC6 — Customer places an order for beef + chicken via Web (BU: CFR);
 *        beef is not fresh at delivery → partial return with refund
 *
 * Scenario (Thai): สั่งเนื้อกับไก่ แต่เอาแค่ไก่ เนื้อไม่สด ลูกค้าเลยไม่เอา สามารถคืนเงินได้
 *   Customer orders beef (เนื้อ) and chicken (ไก่).
 *   At delivery, beef is rejected as not fresh.
 *   Customer keeps chicken; returns beef with refund.
 *
 * Use case refs: UC-PARTRETURN — Partial Item Return with Refund
 *   - Only allowed after Delivered status
 *   - Each returned item references the original orderLineId
 *   - POST /returns triggers partial refund calculation via POS
 *
 * Sequence:
 *   Pending → PickStarted → PickConfirmed → Packed → OutForDelivery → Delivered
 *   [customer rejects beef at door]
 *   POST /returns (returnType=PartialItem, items=[beef line only]) → ReturnRequested
 */

describe('UC6 — Web / CFR / POD — beef + chicken order, beef not fresh → partial return', () => {
  let orderId;
  let beefLineId;   // LINE-001 (beef)
  let chickenLineId; // LINE-002 (chicken)
  let returnId;
  const trackingId = `TRK-UC6-${Date.now()}`;
  const invoiceNum  = `INV-UC6-${Date.now()}`;
  // Prices in satang: beef 35000 satang/kg × 0.5 kg = 17500; chicken 15000 satang/kg × 0.5 kg = 7500
  const BEEF_PRICE_SATANG    = 17500;
  const CHICKEN_PRICE_SATANG = 7500;
  const TOTAL_SATANG         = BEEF_PRICE_SATANG + CHICKEN_PRICE_SATANG; // 25000
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a POD order with beef and chicken lines', () => {
    cy.createOrder({
      channelType:   'Web',
      businessUnit:  'CFR',
      paymentMethod: 'POD',
      isPrepaid:     false,
      lines: [
        {
          sku:           'BEEF-KG',
          productName:   'Beef (per kg)',
          barcode:       '8851234580001',
          requestedQty:  0.5,
          unitPrice:     35000, // 350 THB/kg in satang
          unitOfMeasure: 'KG',
        },
        {
          sku:           'CHICKEN-KG',
          productName:   'Chicken (per kg)',
          barcode:       '8851234580002',
          requestedQty:  0.5,
          unitPrice:     15000, // 150 THB/kg in satang
          unitOfMeasure: 'KG',
        },
      ],
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.lines).to.have.length(2);

      beefLineId    = order.lines[0].id; // LINE-001
      chickenLineId = order.lines[1].id; // LINE-002
      orderId = order.id;
    });
  });

  it('Step 2 — WMS pick-started transitions order to PickStarted', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-started', {
      orderId,
      pickerId:  'PICKER-001',
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('PickStarted');
    });
  });

  it('Step 3 — WMS wave-started fires WaveStartedSentToGW outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC6-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 4 — WMS requests POS recalculation; POS returns result', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'MidPickPriceCheck',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.posRecalcPending).to.be.true;
    });

    cy.omsApi('POST', '/webhooks/pos/pos-recalc-completed', {
      orderId,
      finalAmount: TOTAL_SATANG,
      currency:    'THB',
      completedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.posRecalcPending).to.be.false;
    });
  });

  it('Step 5 — WMS pick-confirmed with both beef and chicken lines', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines: [
        { orderLineId: beefLineId,    sku: 'BEEF-KG',    pickedQty: 0.5, substituted: false },
        { orderLineId: chickenLineId, sku: 'CHICKEN-KG', pickedQty: 0.5, substituted: false },
      ],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 6 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [
        {
          trackingId,
          vehicleType: 'Motorcycle',
          weight:      1.0,
          lineIds:     [beefLineId, chickenLineId],
        },
      ],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 7 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 8 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer CFR',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 9 — STS ABB/Tax Invoice received after Delivered (POD → TMS + GW)', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC6-${Date.now()}`,
      invoiceAmount: TOTAL_SATANG,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC6.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  it('Step 10 — POS invoiced transitions order to Invoiced', () => {
    cy.omsApi('POST', '/webhooks/pos/invoiced', {
      orderId,
      invoiceNumber: invoiceNum,
      totalAmount:   TOTAL_SATANG,
      currency:      'THB',
      invoiceType:   'Standard',
      invoicedAt:    now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Invoiced');
    });
  });

  it('Step 11 — POS payment-confirmed transitions order to Paid (customer pays for both items)', () => {
    cy.omsApi('POST', '/webhooks/pos/payment-confirmed', {
      orderId,
      invoiceNumber: invoiceNum,
      paymentMethod: 'Cash',
      paidAmount:    TOTAL_SATANG,
      currency:      'THB',
      paidAt:        now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Paid');
    });
  });

  // Customer rejects beef at the door (not fresh) — partial return for beef only
  it('Step 12 — Customer initiates partial return: beef not fresh (chicken kept)', () => {
    cy.omsApi('POST', '/returns', {
      orderId,
      returnReason: 'ItemNotFresh',
      items: [
        {
          orderLineId: beefLineId,
          sku:         'BEEF-KG',
          quantity:    0.5,
          itemReason:  'ItemNotFresh',
        },
      ],
      requestedBy: 'customer@cfr.example.com',
    }).then((res) => {
      expect(res.status).to.eq(201);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.status).to.eq('Requested');
      expect(res.body.items).to.have.length(1);
      expect(res.body.items[0].sku).to.eq('BEEF-KG');
      returnId = res.body.id;
    });
  });

  it('Step 13 — Partial return is recorded and references only the beef line', () => {
    cy.omsApi('GET', `/returns/${returnId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.returnReason).to.eq('ItemNotFresh');
      expect(res.body.status).to.eq('Requested');
    });
  });

  it('Step 14 — Return items contain only beef; chicken line is not returned', () => {
    cy.omsApi('GET', `/returns/${returnId}/items`).then((res) => {
      expect(res.status).to.eq(200);
      const items = res.body.items ?? res.body;
      const skus = items.map((i) => i.sku);
      expect(skus).to.include('BEEF-KG');
      expect(skus).not.to.include('CHICKEN-KG');
    });
  });
});
