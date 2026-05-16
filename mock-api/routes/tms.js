const express = require('express');
const router = express.Router();

function requireApiKey(req, res, next) {
  if (!req.headers['x-api-key']) {
    return res.status(401).json({ error: 'Missing x-api-key' });
  }
  next();
}

// POST /tms/api/invoices
// Spec: tms-wms-tax-invoice.md
router.post('/api/invoices', requireApiKey, (req, res) => {
  return res.status(204).send();
});

// POST /tms/api/picks
router.post('/api/picks', requireApiKey, (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /tms/api/packs
router.post('/api/packs', requireApiKey, (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /tms/api/bookings/cancel
router.post('/api/bookings/cancel', requireApiKey, (req, res) => {
  return res.status(200).json({ accepted: true });
});

module.exports = router;
