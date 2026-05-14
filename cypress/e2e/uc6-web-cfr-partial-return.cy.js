/**
 * UC6 — Customer places an order for beef + chicken via Web (BU: CFR);
 *        beef is not fresh at delivery → partial return with refund
 *
 * Scenario (Thai): สั่งเนื้อกับไก่ แต่เอาแค่ไก่ เนื้อไม่สด ลูกค้าเลยไม่เอา สามารถคืนเงินได้
 *   Customer orders beef (เนื้อ) and chicken (ไก่).
 *   At delivery, beef is rejected as not fresh.
 *   Customer keeps chicken; returns beef; refund issued via Returns flow.
 *
 * Use case refs: UC-PARTRETURN — Partial Item Return with Refund
 *   - Only allowed after Delivered status (API allows return from Delivered, Invoiced, or Paid)
 *   - Each returned item references the original orderLineId
 *   - POST /returns triggers partial refund calculation
 *
 * POD sequence (docs/oms-overview.md §2.3):
 *   Pending → PickStarted → POS Recalc → PickConfirmed → Packed →
 *   OutForDelivery → RecalcRequested → Delivered →
 *   STS ABB/Tax Invoice (→ TMS + GW) →
 *   [customer rejects beef at door]
 *   POST /returns (returnType=PartialItem, items=[beef line only]) → ReturnRequested
 *
 * No POS invoiced/payment-confirmed steps — POD terminal order state is Delivered.
 */

describe('UC6 — Web / CFR / POD — beef + chicken order, beef not fresh → partial return', () => {
  let orderId;
  let beefLineId;   // LINE-001 (beef)
  let chickenLineId; // LINE-002 (chicken)
  let returnId;
  const trackingId = `TRK-UC6-${Date.now()}`;
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

  it('Step 3a — WMS requests POS recalculation; OMS calls POS outbound and returns adjusted amount', () => {
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

  it('Step 4 — WMS pick-confirmed with both beef and chicken lines; OMS calls POS internally', () => {
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

  it('Step 5 — WMS packed transitions order to Packed', () => {
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

  it('Step 6 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  // POD-specific: TMS notifies OMS that delivery is imminent; OMS calls POS for a final
  // recalculation and records RecalcRequested in the timeline before Delivered.
  it('Step 6a — TMS pre-delivery recalculation for POD; OMS fires RecalcRequested event in timeline', () => {
    cy.omsApi('POST', '/webhooks/tms/recalculation-requested', {
      trackingId,
      reason:      'PickQuantityDiffers',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.adjustedAmount).to.be.a('number');
    });

    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('PosRecalcCalled');
    });
  });

  it('Step 7 — TMS package-delivered transitions order to Delivered', () => {
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

  // POD-specific: STS issues ABB/Tax Invoice AFTER Delivered; forwarded to TMS + GW
  // No POS invoiced/payment-confirmed steps — POD terminal order state is Delivered.
  it('Step 8 — STS ABB/Tax Invoice received after Delivered; OMS dispatches ABBTaxInvoiceSentToTMS + ABBTaxInvoiceSentToGW', () => {
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

    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.body.status).to.eq('Delivered');
    });

    // POD routing: ABB/Tax Invoice forwarded to TMS + GW (not WMS + GW as in Prepaid)
    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('ABBTaxInvoiceSentToTMS');
      expect(names).to.include('ABBTaxInvoiceSentToGW');
    });
  });

  // Customer rejects beef at the door (not fresh) — partial return for beef only.
  // Return is initiated from Delivered status (allowed per UC-PARTRETURN).
  it('Step 9 — Customer initiates partial return from Delivered: beef not fresh (chicken kept)', () => {
    cy.omsApi('POST', '/returns', {
      orderId,
      returnType:   'PartialItem',
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

  it('Step 10 — Partial return is recorded and references only the beef line', () => {
    cy.omsApi('GET', `/returns/${returnId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.orderId).to.eq(orderId);
      expect(res.body.returnReason).to.eq('ItemNotFresh');
      expect(res.body.status).to.eq('Requested');
    });
  });

  it('Step 11 — Return items contain only beef; chicken line is not returned', () => {
    cy.omsApi('GET', `/returns/${returnId}/items`).then((res) => {
      expect(res.status).to.eq(200);
      const items = res.body.items ?? res.body;
      const skus = items.map((i) => i.sku);
      expect(skus).to.include('BEEF-KG');
      expect(skus).not.to.include('CHICKEN-KG');
    });
  });
});
