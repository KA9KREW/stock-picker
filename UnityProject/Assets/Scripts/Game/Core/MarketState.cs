using System;

namespace StockPicker.Game.Core
{
    [Serializable]
    public struct MarketState
    {
        public int[] PricesCents;

        public static MarketState NewStarting(GameRules rules)
        {
            var m = new MarketState { PricesCents = new int[6] };
            for (var i = 0; i < 6; i++)
                m.PricesCents[i] = rules.startingPriceCents;
            return m;
        }
    }
}
