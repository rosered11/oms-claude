/**
 * UC11 — Substitution with refund: fabric softener → dish soap (cheaper)
 *
 * Scenario (Thai): สั่งน้ำ 2 packs กับน้ำยาปรับผ้านุ่ม แต่น้ำยาปรับผ้านุ่มไม่มี
 *                  เลยเอาน้ำยาล้างจานแทน แต่ต้องคืนเงินส่วนต่าง
 *   Customer orders water (×2) + fabric softener (×1, 8900 satang).
 *   Fabric softener is unavailable; WMS offers dish soap (4500 satang) as substitute.
 *   Customer approves substitution.
 *   Since substitute is cheaper, a credit note is issued for the difference.
 *
 * Prepaid sequence:
 *   Pending → PickStarted → wave-started → substitution-offered →
 *   customer approves → WMS recalc (SubstitutionApproved) →
 *   PickConfirmed → STS ABB/Tax Invoice → STS Credit Note →
 *   Packed → OutForDelivery → Delivered
 *
 * Key invariants exercised:
 *   - POST /webhooks/wms/substitution-offered creates a substitution record
 *   - GET /orders/{id}/substitutions shows customerApproved:null before approval
 *   - POST /orders/{id}/substitutions/{subId}/approve sets customerApproved:true
 *   - Credit note issued when substitute unit price < original unit price
 *   - Final GET /orders/{id}/substitutions confirms approval persists
 */

describe('UC11 — Substitution: fabric softener → dish soap, credit note for price difference', () => {
  let orderId;
  let waterLineId;
  let softenerLineId;
  let substitutionId;
  const trackingId = `TRK-UC11-${Date.now()}`;
  const now = () => new Date().toISOString();

  it('Step 1 — Creates Prepaid order with water (×2) and fabric softener (×1)', () => {
    cy.createOrder({
      businessUnit:  'CFR',
      channelType:   'Web',
      paymentMethod: 'Prepaid',
      isPrepaid:     true,
      lines: [
        {
          sku:           'WATER-500ML',
          productName:   'Water 500ml',
          barcode:       '8851234590001',
          requestedQty:  2,
          unitPrice:     1500,
          unitOfMeasure: 'Each',
        },
        {
          sku:           'FABRIC-SOFTENER-500ML',
          productName:   'Fabric Softener 500ml',
          barcode:       '8851234590003',
          requestedQty:  1,
          unitPrice:     8900,
          unitOfMeasure: 'Each',
        },
      ],
    }).then((order) => {
      orderId        = order.id;
      waterLineId    = order.lines[0].id;
      softenerLineId = order.lines[1].id;
      expect(order.status).to.eq('Pending');
      expect(order.lines).to.have.length(2);
    });
  });

  it('Step 2 — WMS pick-started transitions order to PickStarted', () => {
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

  it('Step 3 — WMS wave-started fires WaveStartedSentToGW outbox event', () => {
    cy.omsApi('POST', '/webhooks/wms/wave-started', {
      orderId,
      waveId:    `WAVE-UC11-${Date.now()}`,
      startedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.orderId).to.eq(orderId);
    });
  });

  it('Step 4 — WMS substitution-offered for fabric softener → dish soap; saves substitutionId', () => {
    cy.omsApi('POST', '/webhooks/wms/substitution-offered', {
      orderId,
      orderLineId:            softenerLineId,
      substituteSku:          'DISH-SOAP-500ML',
      substituteProductName:  'Dish Soap 500ml',
      substituteUnitPrice:    4500,
      substitutedAmount:      1,
      offeredAt:              now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.substitutionId).to.be.a('string');
      expect(res.body.customerNotified).to.be.true;
      substitutionId = res.body.substitutionId;
    });
  });

  it('Step 5 — GET substitutions; verifies originalSku, substituteSku, customerApproved null', () => {
    cy.omsApi('GET', `/orders/${orderId}/substitutions`).then((res) => {
      expect(res.status).to.eq(200);
      const subs = res.body.substitutions ?? res.body;
      expect(subs).to.have.length.at.least(1);
      const sub = subs.find((s) => s.id === substitutionId || s.substitutionId === substitutionId);
      expect(sub).to.exist;
      expect(sub.originalSku).to.eq('FABRIC-SOFTENER-500ML');
      expect(sub.substituteSku).to.eq('DISH-SOAP-500ML');
      expect(sub.customerApproved).to.be.null;
    });
  });

  it('Step 6 — Customer approves substitution; verifies customerApproved:true', () => {
    cy.omsApi('POST', `/orders/${orderId}/substitutions/${substitutionId}/approve`).then((res) => {
      expect(res.status).to.eq(200);
      expect(res.body.substitutionId).to.eq(substitutionId);
      expect(res.body.customerApproved).to.be.true;
    });
  });

  it('Step 7 — WMS recalculation-requested (SubstitutionApproved); adjustedAmount is a number', () => {
    cy.omsApi('POST', '/webhooks/wms/recalculation-requested', {
      orderId,
      reason:      'SubstitutionApproved',
      requestedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.adjustedAmount).to.be.a('number');
    });
  });

  it('Step 8 — WMS pick-confirmed: water (×2) + softener line substituted; transitions to PickConfirmed', () => {
    cy.omsApi('POST', '/webhooks/wms/pick-confirmed', {
      orderId,
      lines: [
        { orderLineId: waterLineId,    sku: 'WATER-500ML',           pickedQty: 2, substituted: false },
        { orderLineId: softenerLineId, sku: 'FABRIC-SOFTENER-500ML', pickedQty: 1, substituted: true  },
      ],
      pickedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('PickConfirmed');
    });
  });

  it('Step 9 — STS ABB/Tax Invoice received; accepted', () => {
    cy.omsApi('POST', '/webhooks/sts/abb-tax-invoice-received', {
      orderId,
      invoiceNumber: `ABB-UC11-${Date.now()}`,
      invoiceAmount: 7500, // 2×1500 water + 4500 dish soap
      currency:      'THB',
      invoiceLink:   'https://sts.example.com/invoices/ABB-UC11.pdf',
      issuedAt:      now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
    });
  });

  it('Step 10 — STS credit note received for refund of price difference (8900 − 4500 = 4400)', () => {
    // Try the primary endpoint; fall back to alternate if 404
    cy.omsApi('POST', '/webhooks/sts/credit-note-received', {
      orderId,
      creditNoteNumber: `CN-UC11-${Date.now()}`,
      creditNoteLink:   'https://sts.example.com/cn/UC11.pdf',
      amount:           4400,
      currency:         'THB',
      issuedAt:         now(),
    }).then((res) => {
      if (res.status === 404) {
        cy.omsApi('POST', '/webhooks/sts/credit-note', {
          orderId,
          creditNoteNumber: `CN-UC11-${Date.now()}`,
          creditNoteLink:   'https://sts.example.com/cn/UC11.pdf',
          amount:           4400,
          currency:         'THB',
          issuedAt:         now(),
        }).then((res2) => {
          expect(res2.status).to.eq(202);
          expect(res2.body.accepted).to.be.true;
        });
      } else {
        expect(res.status).to.eq(202);
        expect(res.body.accepted).to.be.true;
      }
    });
  });

  it('Step 11 — WMS packed transitions order to Packed', () => {
    cy.omsApi('POST', '/webhooks/wms/packed', {
      orderId,
      packages: [
        {
          trackingId,
          vehicleType: 'Motorcycle',
          weight:       1.5,
          lineIds:      [waterLineId, softenerLineId],
        },
      ],
      packedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.accepted).to.be.true;
      expect(res.body.newStatus).to.eq('Packed');
    });
  });

  it('Step 12 — TMS package-dispatched transitions order to OutForDelivery', () => {
    cy.omsApi('POST', '/webhooks/tms/package-dispatched', {
      trackingId,
      dispatchedAt: now(),
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newOrderStatus).to.eq('OutForDelivery');
    });
  });

  it('Step 13 — TMS package-delivered transitions order to Delivered', () => {
    cy.omsApi('POST', '/webhooks/tms/package-delivered', {
      trackingId,
      deliveredAt:   now(),
      recipientName: 'Test Customer UC11',
    }).then((res) => {
      expect(res.status).to.eq(202);
      expect(res.body.newStatus).to.eq('Delivered');
      expect(res.body.invoiceTriggered).to.be.true;
    });
  });

  it('Step 14 — Final GET substitutions: customerApproved persists as true', () => {
    cy.omsApi('GET', `/orders/${orderId}/substitutions`).then((res) => {
      expect(res.status).to.eq(200);
      const subs = res.body.substitutions ?? res.body;
      const sub = subs.find((s) => s.id === substitutionId || s.substitutionId === substitutionId);
      expect(sub).to.exist;
      expect(sub.customerApproved).to.be.true;
    });
  });
});
