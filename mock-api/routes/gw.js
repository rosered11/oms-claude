const express = require('express');
const router = express.Router();

function requireApiKey(req, res, next) {
  if (!req.headers['x-api-key']) {
    return res.status(401).json({ error: 'Missing x-api-key' });
  }
  next();
}

// POST /gw/api/status-update
// Spec: gw-update-status.md
router.post('/api/status-update', requireApiKey, (req, res) => {
  return res.status(204).send();
});

// POST /gw/api/invoices
router.post('/api/invoices', requireApiKey, (req, res) => {
  return res.status(204).send();
});

// POST /gw/api/credit-notes
router.post('/api/credit-notes', requireApiKey, (req, res) => {
  return res.status(204).send();
});

// POST /gw/api/orders/cancel
router.post('/api/orders/cancel', requireApiKey, (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /gw/api/wave-started
router.post('/api/wave-started', requireApiKey, (req, res) => {
  return res.status(200).json({ accepted: true });
});

module.exports = router;
