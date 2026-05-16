const express = require('express');
const router = express.Router();

// POST /tiktok/oauth/token
// OAuth2 token — no strict auth check
router.post('/oauth/token', (req, res) => {
  return res.status(200).json({
    access_token: `mock-tiktok-token-${Date.now()}`,
    token_type: 'Bearer',
    expires_in: 3600
  });
});

// POST /tiktok/api/orders
router.post('/api/orders', (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /tiktok/api/picks
router.post('/api/picks', (req, res) => {
  return res.status(200).json({ accepted: true });
});

// POST /tiktok/api/awb
router.post('/api/awb', (req, res) => {
  return res.status(200).json({ accepted: true });
});

module.exports = router;
