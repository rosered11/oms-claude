const express = require('express');
const router = express.Router();

// POST /lazada/oauth/token
// OAuth2 token — no strict auth check
router.post('/oauth/token', (req, res) => {
  return res.status(200).json({
    access_token: `mock-lazada-token-${Date.now()}`,
    token_type: 'Bearer',
    expires_in: 3600
  });
});

// POST /lazada/api/orders
router.post('/api/orders', (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /lazada/api/picks
router.post('/api/picks', (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /lazada/api/packs
router.post('/api/packs', (req, res) => {
  return res.status(200).json({ accepted: true });
});

module.exports = router;
