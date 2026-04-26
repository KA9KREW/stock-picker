using System;

namespace StockPicker.Game.Core
{
    [Serializable]
    public struct TradeOrder
    {
        public int PlayerIndex;
        public CommodityId Commodity;
        /// <summary>Positive = buy shares, negative = sell shares. Magnitude must be allowed lot size.</summary>
        public int ShareDelta;
    }
}
