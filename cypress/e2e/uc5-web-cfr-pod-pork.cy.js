/**
 * UC5 — Customer places a POD order via Web (BU: CFR) for weight-based fresh products
 *
 * Scenario (Thai):
 *   1. ซื้อหมู กิโลกรัมละ 127 บาท ลูกค้าซื้อ 841.23 กรัม
 *      Pork at 127 THB/kg — customer buys 841.23 grams (0.84123 kg)
 *      Total: 10684 satang (POS-rounded from 10683.621)
 *
 *   2. ซื้อเป็ด 2.5 kg 99 บาท ลูกค้าซื้อ 1.23 kg
 *      Duck sold in 2.5 kg packs at 99 THB/pack (= 39.6 THB/kg = 3960 satang/kg);
 *      customer buys 1.23 kg
 *      Total: 4871 satang (POS-rounded from 4870.8)
 *
 * Combined order total: 15555 satang (10684 + 4871)
 *
 * Weight-based pricing:
 *   Pork: unitPrice 12700 satang/kg × 0.84123 kg → 10683.621 satang → POS rounds to 10684
 *   Duck: unitPrice  3960 satang/kg × 1.23 kg    →  4870.8 satang   → POS rounds to 4871
 *
 * POD sequence (docs/oms-overview.md §2.3):
 *   Pending → PickStarted → POS Recalc → PickConfirmed → Packed →
 *   OutForDelivery → RecalcRequested → Delivered → STS ABB Invoice (→ TMS + Gateway)
 *
 * Terminal state: Delivered. No POS invoiced/payment-confirmed steps in POD.
 */

describe('UC5 — Web / CFR / POD — weight-based fresh products (pork 841.23 g + duck 1.23 kg)', () => {
  let orderId;
  let porkLineId;
  let duckLineId;
  const trackingId = `TRK-UC5-${Date.now()}`;

  // Pork: 127 THB/kg = 12700 satang/kg; customer buys 841.23 g = 0.84123 kg
  const PORK_UNIT_PRICE_SATANG = 12700;
  const PORK_QTY_KG            = 0.84123;
  const PORK_FINAL_SATANG      = 10684;  // 0.84123 × 12700 = 10683.621 → rounds to 10684

  // Duck: 99 THB per 2.5 kg pack = 39.6 THB/kg = 3960 satang/kg; customer buys 1.23 kg
  const DUCK_UNIT_PRICE_SATANG = 3960;
  const DUCK_QTY_KG            = 1.23;
  const DUCK_FINAL_SATANG      = 4871;   // 1.23 × 3960 = 4870.8 → rounds to 4871

  const TOTAL_SATANG = PORK_FINAL_SATANG + DUCK_FINAL_SATANG; // 15555
  const TOTAL_QTY_KG = PORK_QTY_KG + DUCK_QTY_KG;            // 2.07123

  const now = () => new Date().toISOString();

  it('Step 1 — Creates a POD order with weight-based pork (841.23 g) and duck (1.23 kg) lines', () => {
    cy.createOrder({
      channelType:   'Web',
      businessUnit:  'CFR',
      paymentMethod: 'POD',
      paymentFlow:   'PAY_ON_DELIVERY',
      lines: [
        {
          sku:           'PORK-KG',
          productName:   'Pork (per kg)',
          barcode:       '8851234570001',
          requestedQty:  PORK_QTY_KG,
          unitPrice:     PORK_UNIT_PRICE_SATANG,
          unitOfMeasure: 'KG',
        },
        {
          sku:           'DUCK-KG',
          productName:   'Duck (per kg, sold in 2.5 kg packs)',
          barcode:       '8851234570002',
          requestedQty:  DUCK_QTY_KG,
          unitPrice:     DUCK_UNIT_PRICE_SATANG,
          unitOfMeasure: 'KG',
        },
      ],
    }).then((order) => {
      expect(order.status).to.eq('Pending');
      expect(order.paymentMethod).to.eq('POD');
      expect(order.businessUnit).to.eq('CFR');
      expect(order.lines).to.have.length(2);

      const porkLine = order.lines.find((l) => l.sku === 'PORK-KG');
      const duckLine = order.lines.find((l) => l.sku === 'DUCK-KG');
      expect(porkLine.requestedAmount).to.be.closeTo(PORK_QTY_KG, 0.00001);
      expect(duckLine.requestedAmount).to.be.closeTo(DUCK_QTY_KG, 0.00001);

      orderId    = order.id;
      porkLineId = porkLine.id;
      duckLineId = duckLine.id;
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

  it('Step 3 — WMS wave-started fires WaveStartedSentToGateway outbox event', () => {
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

  it('Step 3a — WMS requests POS recalculation for actual weights; OMS calls POS outbound and returns adjusted amount', () => {
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

  it('Step 4 — WMS pick-confirmed with actual weights (pork 841.23 g + duck 1.23 kg); OMS calls POS internally', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines: [
        { orderLineId: porkLineId, sku: 'PORK-KG', pickedQty: PORK_QTY_KG, substituted: false },
        { orderLineId: duckLineId, sku: 'DUCK-KG', pickedQty: DUCK_QTY_KG, substituted: false },
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
      packages: [{
        trackingId,
        vehicleType: 'Motorcycle',
        weight:      TOTAL_QTY_KG,
        lineIds:     [porkLineId, duckLineId],
      }],
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

  // POD-specific: TMS notifies OMS that delivery is imminent; for weight-based products
  // the driver confirms the final weight, OMS calls POS for a final recalculation and
  // records RecalcRequested in the timeline before Delivered.
  it('Step 6a — TMS pre-delivery recalculation for POD (actual weights confirmed); OMS fires RecalcRequested in timeline', () => {
    cy.omsApi('POST', '/webhooks/tms/recalculation-requested', {
      trackingId,
      reason:       'ActualWeightDiffers',
      actualWeight: TOTAL_QTY_KG,
      requestedAt:  now(),
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
      recipientName: 'Test Customer CFR Pork Duck',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  // POD-specific: STS issues ABB/Tax Invoice AFTER Delivered; forwarded to TMS + Gateway
  // No POS invoiced/payment-confirmed steps — POD terminal state is Delivered.
  it('Step 8 — STS ABB/Tax Invoice received after Delivered; OMS dispatches ABBTaxInvoiceSentToTMS + ABBTaxInvoiceSentToGateway', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC5-${Date.now()}`,
      invoiceAmount: TOTAL_SATANG,
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

    // POD routing: ABB/Tax Invoice forwarded to TMS + Gateway (not WMS + Gateway as in Prepaid)
    cy.omsApi('GET', `/orders/${orderId}/timeline`).then((res) => {
      const events = res.body.events ?? res.body;
      const names  = events.map((e) => e.event);
      expect(names).to.include('ABBTaxInvoiceSentToTMS');
      expect(names).to.include('ABBTaxInvoiceSentToGateway');
    });
  });

  it('Step 9 — Final state: order remains Delivered with weight-based amounts for both products', () => {
    cy.omsApi('GET', `/orders/${orderId}`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.status).to.eq('Delivered');
      expect(res.body.businessUnit).to.eq('CFR');
      expect(res.body.paymentMethod).to.eq('POD');
    });
  });
});
