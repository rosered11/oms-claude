const express = require('express');

const wmsRoutes = require('./routes/wms');
const tmsRoutes = require('./routes/tms');
const GatewayRoutes = require('./routes/Gateway');
const posRoutes = require('./routes/pos');
const tiktokRoutes = require('./routes/tiktok');
const lazadaRoutes = require('./routes/lazada');

const app = express();

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Request logger middleware
app.use((req, res, next) => {
  console.log(`[MOCK] ${req.method} ${req.originalUrl}`);
  if (req.body && Object.keys(req.body).length > 0) {
    console.log('Body:', JSON.stringify(req.body, null, 2));
  }
  next();
});

// Health check
app.get('/health', (req, res) => {
  res.json({ status: 'ok' });
});

// Mount routes
app.use('/wms', wmsRoutes);
app.use('/tms', tmsRoutes);
app.use('/Gateway', GatewayRoutes);
app.use('/pos', posRoutes);
app.use('/tiktok', tiktokRoutes);
app.use('/lazada', lazadaRoutes);

const PORT = 3001;
app.listen(PORT, () => {
  console.log(`Mock API running on http://localhost:${PORT}`);
});
