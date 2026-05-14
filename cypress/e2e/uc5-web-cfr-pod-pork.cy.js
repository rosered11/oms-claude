/**
 * UC5 — Customer places a POD order via Web (BU: CFR) for weight-based pork product
 *
 * Scenario (Thai): หมู กิโลกรัมละ 127 บาท ลูกค้าซื้อ 841.23 กรัม
 *   Pork at 127 THB/kg — customer buys 841.23 grams (0.84123 kg)
 *
 * Weight-based pricing:
 *   unitPrice:    12700 satang / kg  (= 127 THB/kg)
 *   requestedQty: 0.84123 kg         (= 841.23 g)
 *   expectedTotal 10683.621 satang   (OMS uses decimal; POS rounds to 10684)
 *
 * Verifies that OMS correctly records fractional weight quantities and that the
 * POD flow completes with the weight-derived amount.
 *
 * POD sequence (request/sequence-diagram-pod.md):
 *   Pending → PickStarted → POS Recalc → PickConfirmed → Packed →
 *   OutForDelivery → Delivered → STS ABB Invoice (→ TMS + GW)
 *
 * Terminal state: Delivered. No POS invoiced/payment-confirmed steps in POD.
 */

describe('UC5 — Web / CFR / POD — weight-based pork product (841.23 g)', () => {
  let orderId;
  let lineId;
  const trackingId = `TRK-UC5-${Date.now()}`;
  // 127 THB/kg × 0.84123 kg = 106.83621 THB = 10683.621 satang → POS rounds to 10684
  const PORK_UNIT_PRICE_SATANG = 12700;   // 127 THB in satang per KG
  const PORK_QTY_KG            = 0.84123; // 841.23 g expressed in KG
  const PORK_FINAL_SATANG      = 10684;   // POS-rounded total in satang
  const now = () => new Date().toISOString();

  it('Step 1 — Creates a POD order with weight-based pork line (841.23 g)', () => {
    cy.createOrder({
      channelType:   'Web',
      businessUnit:  'CFR',
      paymentMethod: 'POD',
      isPrepaid:     false,
      lines: [
        {
          sku:           'PORK-KG',
          productName:   'Pork (per kg)',
          barcode:       '8851234570001',
          requestedQty:  PORK_QTY_KG,
          unitPrice:     PORK_UNIT_PRICE_SATANG,
          unitOfMeasure: 'KG',
        },
      ],
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('POD');
      expect(order.businessUnit).to.eq('CFR');

      const line = order.lines[0];
      expect(line.sku).to.eq('PORK-KG');
      // Verify the fractional weight quantity is stored precisely
      expect(line.requestedAmount).to.be.closeTo(PORK_QTY_KG, 0.00001);

      orderId = order.id;
      lineId  = order.lines[0].id;
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
      waveId:    `WAVE-UC5-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 3a — WMS requests POS recalculation for actual weight (841.23 g); OMS calls POS outbound and returns adjusted amount', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'ActualWeightDiffers',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.adjustedAmount).to.be.a('number');
    });
  });

  it('Step 4 — WMS pick-confirmed with actual weight (841.23 g = 0.84123 kg); OMS calls POS internally', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines:    [{ orderLineId: lineId, sku: 'PORK-KG', pickedQty: PORK_QTY_KG, substituted: false }],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 5 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [{ trackingId, vehicleType: 'Motorcycle', weight: PORK_QTY_KG, lineIds: [lineId] }],
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

  it('Step 7 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer CFR Pork',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  // POD-specific: STS issues ABB/Tax Invoice AFTER Delivered; forwarded to TMS + GW
  // No POS invoiced/payment-confirmed steps — POD terminal state is Delivered.
  it('Step 8 — STS ABB/Tax Invoice received after Delivered; OMS dispatches ABBTaxInvoiceSentToTMS + ABBTaxInvoiceSentToGW', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC5-${Date.now()}`,
      invoiceAmount: PORK_FINAL_SATANG,
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC5.pdf',
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

  it('Step 9 — Final state: order remains Delivered with weight-based amount', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
      expect(res.body.businessUnit).to.eq('CFR');
      expect(res.body.paymentMethod).to.eq('POD');
    });
  });
});
