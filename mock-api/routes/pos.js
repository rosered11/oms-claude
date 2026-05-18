const express = require('express');
const router = express.Router();

// POST /pos/api/recalculate
// Spec: pos-recalc.md
router.post('/api/recalculate', (req, res) => {
  const accessToken = req.headers['accesstoken'] || req.headers['accessToken'];
  if (!accessToken) {
    return res.status(401).json({
      errors: { code: 401, message: 'Missing accessToken' },
      data: null,
      dbitems: []
    });
  }

  const {
    Orderno,
    StoreCode,
    CustomerSegment,
    CustomerCDP_ID,
    SaleChannel,
    salesource,
    OrderItems = [],
    couponItem = []
  } = req.body;

  const now = new Date().toISOString();
  const totalAmt = OrderItems.reduce((sum, item) => sum + (Number(item.AMT) || 0), 0);
  const netAmt = totalAmt;
  const vatAmount = parseFloat((netAmt * 0.07).toFixed(2));

  const itemsData = OrderItems.map((item) => ({
    SEQ: item.SEQ,
    SKCODE: item.SK_CODE,
    PR_CODE: `${item.SK_CODE}-PR`,
    QNT: item.QNT,
    WeightItemFlag: item.WeightItemFlag,
    AvGatewayeight: item.AvGatewayeight || 0,
    QNTItem: item.QNTItem || item.QNT,
    itemUnit: item.itemUnit,
    AMT: item.AMT,
    NetAmt: item.AMT,
    UPC: item.UPC,
    CTLID: Array.isArray(item.CTLID) && item.CTLID.length > 0 ? item.CTLID[0] : '',
    PriceRequestValue: null,
    DiscountCode: item.DiscountCode || null,
    ExcludedBMGN: item.ExcludedBMGN || false,
    Remark: false,
    ReferenceSEQ: item.ReferenceSEQ
  }));

  return res.status(200).json({
    errors: null,
    data: { dbcode: 0, dbmessage: 'Success' },
    dbitems: [
      {
        OrderHead: {
          Orderno: Orderno,
          StoreCode: StoreCode,
          REF: Orderno,
          CustomerSegment: CustomerSegment,
          CustomerCDP_ID: CustomerCDP_ID,
          SaleChannel: SaleChannel,
          salesource: salesource,
          orderdate: now,
          nitem: OrderItems.length,
          amount: totalAmt,
          discount: 0,
          NetAmt: netAmt,
          Vatamount: vatAmount,
          DeliveryMethod: 'delivery',
          MarketingDiscount: 0,
          MerchandiseDiscount: 0
        },
        OrderItems: {
          ItemsData: itemsData
        },
        OrderPlpp: { PlppsData: [] },
        OrderCoupon: { CouponData: [] },
        InvalidPlpp: { PlppsData: [], CouponData: [] }
      }
    ]
  });
});

module.exports = router;
