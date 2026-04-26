using System.Collections.Generic;

namespace StockPicker.Game.Core
{
    public static class TradeResolver
    {
        public static bool IsAllowedLot(GameRules rules, int magnitude)
        {
            foreach (var lot in rules.allowedShareLots)
            {
                if (lot == magnitude) return true;
            }

            return false;
        }

        /// <summary>
        /// Apply a batch of orders: sells first (per player order in list), then buys.
        /// </summary>
        public static void ApplyOrders(
            GameRules rules,
            MarketState market,
            IList<PlayerPortfolio> players,
            IList<TradeOrder> orders,
            IList<string> log)
        {
            var sells = new List<TradeOrder>();
            var buys = new List<TradeOrder>();
            foreach (var o in orders)
            {
                if (o.ShareDelta < 0) sells.Add(o);
                else if (o.ShareDelta > 0) buys.Add(o);
            }

            foreach (var o in sells)
                TrySell(rules, market, players, o, log);

            foreach (var o in buys)
                TryBuy(rules, market, players, o, log);
        }

        private static void TrySell(GameRules rules, MarketState market, IList<PlayerPortfolio> players, TradeOrder o,
            IList<string> log)
        {
            var mag = -o.ShareDelta;
            if (!IsAllowedLot(rules, mag))
            {
                log.Add($"Player {o.PlayerIndex} invalid sell lot {mag}");
                return;
            }

            var pl = players[o.PlayerIndex];
            var idx = (int)o.Commodity;
            var owned = pl.SharesByCommodity[idx];
            if (owned < mag)
            {
                log.Add($"{pl.DisplayName} sell rejected (owns {owned}, tried {mag} {o.Commodity})");
                return;
            }

            var price = market.PricesCents[idx];
            pl.SharesByCommodity[idx] -= mag;
            pl.CashCents += mag * price;
            players[o.PlayerIndex] = pl;
            log.Add($"{pl.DisplayName} sold {mag} {o.Commodity} @ ${price / 100f:F2}");
        }

        private static void TryBuy(GameRules rules, MarketState market, IList<PlayerPortfolio> players, TradeOrder o,
            IList<string> log)
        {
            var mag = o.ShareDelta;
            if (!IsAllowedLot(rules, mag))
            {
                log.Add($"Player {o.PlayerIndex} invalid buy lot {mag}");
                return;
            }

            var pl = players[o.PlayerIndex];
            var idx = (int)o.Commodity;
            var price = market.PricesCents[idx];
            var cost = mag * price;
            if (pl.CashCents < cost)
            {
                log.Add($"{pl.DisplayName} buy rejected (need ${cost / 100f:F0}, have ${pl.CashCents / 100f:F0})");
                return;
            }

            pl.CashCents -= cost;
            pl.SharesByCommodity[idx] += mag;
            players[o.PlayerIndex] = pl;
            log.Add($"{pl.DisplayName} bought {mag} {o.Commodity} @ ${price / 100f:F2}");
        }
    }
}
