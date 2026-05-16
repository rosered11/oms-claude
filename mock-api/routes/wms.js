const express = require('express');
const router = express.Router();

function requireApiKey(req, res, next) {
  if (!req.headers['x-api-key']) {
    return res.status(401).json({ error: 'Missing x-api-key' });
  }
  next();
}

// POST /wms/oauth/token
// OAuth2 token — no auth check required
router.post('/oauth/token', (req, res) => {
  return res.status(200).json({
    access_token: `mock-wms-token-${Date.now()}`,
    token_type: 'Bearer',
    expires_in: 3600
  });
});

// POST /wms/api/invoices
// Spec: tms-wms-tax-invoice.md
router.post('/api/invoices', requireApiKey, (req, res) => {
  return res.status(204).send();
});

// POST /wms/api/credit-notes
// Spec: tws-wms-credit-note.md
router.post('/api/credit-notes', requireApiKey, (req, res) => {
  const { order_id, abb_id, reference_abb_id } = req.body;
  return res.status(200).json({
    order_id: order_id,
    abb_id: abb_id,
    reference_abb_id: reference_abb_id
  });
});

// POST /wms/api/orders
router.post('/api/orders', requireApiKey, (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /wms/api/orders/cancel
router.post('/api/orders/cancel', requireApiKey, (req, res) => {
  return res.status(200).json({ accepted: true });
});

module.exports = router;
