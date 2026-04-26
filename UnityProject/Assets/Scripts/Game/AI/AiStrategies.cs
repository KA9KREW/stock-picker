using System.Collections.Generic;
using StockPicker.Game.Core;

namespace StockPicker.Game.AI
{
    public static class AiHelpers
    {
        public static int PickLargestAffordableLot(GameRules rules, int cashCents, int priceCents)
        {
            if (priceCents <= 0) return 0;
            for (var i = rules.allowedShareLots.Length - 1; i >= 0; i--)
            {
                var lot = rules.allowedShareLots[i];
                if (cashCents >= lot * priceCents)
                    return lot;
            }

            return 0;
        }
    }

    public sealed class SafeDividendStrategy : IAgentStrategy
    {
        public void DecideTrades(GameSession session, int playerIndex, List<TradeOrder> outOrders)
        {
            var rules = session.Rules;
            var pl = session.State.Players[playerIndex];
            var prices = session.State.Market.PricesCents;

            for (var c = 0; c < 6; c++)
            {
                if (pl.SharesByCommodity[c] <= 0) continue;
                if (prices[c] >= rules.dividendMinimumPriceCents) continue;
                for (var li = rules.allowedShareLots.Length - 1; li >= 0; li--)
                {
                    var lot = rules.allowedShareLots[li];
                    if (pl.SharesByCommodity[c] < lot) continue;
                    outOrders.Add(new TradeOrder
                        { PlayerIndex = playerIndex, Commodity = (CommodityId)c, ShareDelta = -lot });
                    return;
                }
            }

            var bestC = -1;
            var bestPrice = -1;
            for (var c = 0; c < 6; c++)
            {
                var price = prices[c];
                if (price < rules.dividendMinimumPriceCents) continue;
                if (price > bestPrice)
                {
                    bestPrice = price;
                    bestC = c;
                }
            }

            if (bestC < 0) return;

            var lotBuy = AiHelpers.PickLargestAffordableLot(rules, pl.CashCents, prices[bestC]);
            if (lotBuy > 0)
                outOrders.Add(new TradeOrder
                    { PlayerIndex = playerIndex, Commodity = (CommodityId)bestC, ShareDelta = lotBuy });
        }
    }

    public sealed class MomentumStrategy : IAgentStrategy
    {
        public void DecideTrades(GameSession session, int playerIndex, List<TradeOrder> outOrders)
        {
            var rules = session.Rules;
            var pl = session.State.Players[playerIndex];
            var prices = session.State.Market.PricesCents;
            var best = 0;
            for (var c = 1; c < 6; c++)
                if (prices[c] > prices[best])
                    best = c;
            var lot = AiHelpers.PickLargestAffordableLot(rules, pl.CashCents, prices[best]);
            if (lot > 0)
                outOrders.Add(new TradeOrder
                    { PlayerIndex = playerIndex, Commodity = (CommodityId)best, ShareDelta = lot });
        }
    }

    public sealed class ContrarianStrategy : IAgentStrategy
    {
        public void DecideTrades(GameSession session, int playerIndex, List<TradeOrder> outOrders)
        {
            var rules = session.Rules;
            var pl = session.State.Players[playerIndex];
            var prices = session.State.Market.PricesCents;
            var worst = -1;
            var worstPrice = int.MaxValue;
            for (var c = 0; c < 6; c++)
            {
                var p = prices[c];
                if (p <= rules.wipeoutAtCents + 5) continue;
                if (p < worstPrice)
                {
                    worstPrice = p;
                    worst = c;
                }
            }

            if (worst < 0) return;

            var lot = AiHelpers.PickLargestAffordableLot(rules, pl.CashCents, prices[worst]);
            if (lot > 0)
                outOrders.Add(new TradeOrder
                    { PlayerIndex = playerIndex, Commodity = (CommodityId)worst, ShareDelta = lot });
        }
    }

    public sealed class VolatilityHunterStrategy : IAgentStrategy
    {
        public void DecideTrades(GameSession session, int playerIndex, List<TradeOrder> outOrders)
        {
            var rules = session.Rules;
            var pl = session.State.Players[playerIndex];
            var prices = session.State.Market.PricesCents;
            var target = -1;
            var bestCheap = int.MaxValue;
            for (var c = 0; c < 6; c++)
            {
                var p = prices[c];
                if (p <= 0 || p >= rules.dividendMinimumPriceCents) continue;
                if (p < bestCheap)
                {
                    bestCheap = p;
                    target = c;
                }
            }

            if (target < 0) return;

            var lot = AiHelpers.PickLargestAffordableLot(rules, pl.CashCents, prices[target]);
            if (lot > 0)
                outOrders.Add(new TradeOrder
                    { PlayerIndex = playerIndex, Commodity = (CommodityId)target, ShareDelta = lot });
        }
    }

    public sealed class BalancedStrategy : IAgentStrategy
    {
        public void DecideTrades(GameSession session, int playerIndex, List<TradeOrder> outOrders)
        {
            if (session.State.SeasonIndex % 2 == 0)
                new MomentumStrategy().DecideTrades(session, playerIndex, outOrders);
            else
                new SafeDividendStrategy().DecideTrades(session, playerIndex, outOrders);
        }
    }

    public sealed class ChaosPlusStrategy : IAgentStrategy
    {
        public void DecideTrades(GameSession session, int playerIndex, List<TradeOrder> outOrders)
        {
            var rng = new SeededRng(session.State.RandomSeed ^ (playerIndex * 977) ^ session.State.RollIndexInSeason);
            var rules = session.Rules;
            var pl = session.State.Players[playerIndex];
            var prices = session.State.Market.PricesCents;
            var c = (CommodityId)rng.NextInt(6);
            var idx = (int)c;
            var buy = rng.NextInt(2) == 0;
            var lot = rules.allowedShareLots[rng.NextInt(rules.allowedShareLots.Length)];
            if (buy)
            {
                if (pl.CashCents >= lot * prices[idx])
                    outOrders.Add(new TradeOrder { PlayerIndex = playerIndex, Commodity = c, ShareDelta = lot });
            }
            else
            {
                if (pl.SharesByCommodity[idx] >= lot)
                    outOrders.Add(new TradeOrder { PlayerIndex = playerIndex, Commodity = c, ShareDelta = -lot });
            }
        }
    }

    public static class AgentStrategyFactory
    {
        public static IAgentStrategy ForPlayerIndex(int playerIndex)
        {
            return ((playerIndex - 1) % 6) switch
            {
                0 => new SafeDividendStrategy(),
                1 => new MomentumStrategy(),
                2 => new ContrarianStrategy(),
                3 => new VolatilityHunterStrategy(),
                4 => new BalancedStrategy(),
                _ => new ChaosPlusStrategy()
            };
        }
    }
}
