Cypress.Commands.add('omsApi', (method, path, body) => {
  const opts = {
    method,
    url: `${Cypress.env('apiBase')}${path}`,
    failOnStatusCode: false,
  };
  if (body !== undefined) opts.body = body;
  return cy.request(opts);
});

/**
 * General-purpose order creation command.
 * Defaults to Web / CMG / Prepaid with a single Apple line.
 * Pass overrides to customise BU, channel, payment method, or lines.
 */
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
    isPrepaid:       true,
    customerName:    'Test Customer',
    customerPhone:   '0812345678',
    customerEmail:   'test@example.com',
    deliverySlot: {
      scheduledStart: slotStart,
      scheduledEnd:   slotEnd,
    },
    lines: [
      {
        sku:           'APPLE-1KG',
        productName:   'Apple 1 kg',
        barcode:       '8851234560001',
        requestedQty:  2,
        unitPrice:     9900,
        unitOfMeasure: 'KG',
      },
    ],
    ...overrides,
  };

  return cy.omsApi('POST', '/orders', payload).then((res) => {
    expect(res.status).to.eq(201);
    return res.body;
  });
});

Cypress.Commands.add('createPrepaidOrder', (overrides = {}) => {
  const now = new Date().toISOString();
  const slotStart = new Date(Date.now() + 3600000).toISOString();
  const slotEnd   = new Date(Date.now() + 7200000).toISOString();

  const payload = {
    sourceOrderId:   `SRC-${Date.now()}`,
    channelType:     'Web',
    businessUnit:    'SGO',
    storeId:         'STORE-001',
    fulfillmentType: 'Delivery',
    paymentMethod:   'Prepaid',
    isPrepaid:       true,
    customerName:    'Test Customer',
    customerPhone:   '0812345678',
    customerEmail:   'test@example.com',
    deliverySlot: {
      scheduledStart: slotStart,
      scheduledEnd:   slotEnd,
    },
    lines: [
      {
        sku:          'APPLE-1KG',
        productName:  'Apple 1kg',
        barcode:      '8851234560001',
        requestedQty: 2,
        unitPrice:    9900,
        unitOfMeasure: 'KG',
      },
    ],
    ...overrides,
  };

  return cy.omsApi('POST', '/orders', payload).then((res) => {
    expect(res.status).to.eq(201);
    return res.body;
  });
});
