using System.Collections.Generic;

namespace StockPicker.Game.Core
{
    public static class MarketResolver
    {
        public sealed class RollOutcome
        {
            public DiceRoll Roll;
            public readonly List<string> Messages = new();
        }

        /// <summary>
        /// Apply one dice roll to market and portfolios. Mutates market and players.
        /// </summary>
        public static void ApplyRoll(
            GameRules rules,
            DiceRoll roll,
            MarketState market,
            IList<PlayerPortfolio> players,
            RollOutcome outcome)
        {
            outcome.Roll = roll;
            outcome.Messages.Clear();

            var idx = (int)roll.Commodity;
            var price = market.PricesCents[idx];

            switch (roll.Movement)
            {
                case MovementKind.Up:
                {
                    var next = price + roll.Cents;
                    if (next >= rules.splitThresholdCents)
                    {
                        outcome.Messages.Add($"{roll.Commodity} split at ${next / 100f:F2} -> shareholders doubled, price ${rules.priceAfterSplitCents / 100f:F2}");
                        for (var p = 0; p < players.Count; p++)
                        {
                            var pl = players[p];
                            pl.SharesByCommodity[idx] *= 2;
                            players[p] = pl;
                        }

                        market.PricesCents[idx] = rules.priceAfterSplitCents;
                    }
                    else
                    {
                        market.PricesCents[idx] = next;
                        outcome.Messages.Add($"{roll.Commodity} up {roll.Cents}c -> ${market.PricesCents[idx] / 100f:F2}");
                    }

                    break;
                }
                case MovementKind.Down:
                {
                    var next = price - roll.Cents;
                    if (next <= rules.wipeoutAtCents)
                    {
                        outcome.Messages.Add($"{roll.Commodity} wiped out at ${price / 100f:F2}; holdings lost; reset ${rules.resetPriceAfterWipeoutCents / 100f:F2}");
                        for (var p = 0; p < players.Count; p++)
                        {
                            var pl = players[p];
                            pl.SharesByCommodity[idx] = 0;
                            players[p] = pl;
                        }

                        market.PricesCents[idx] = rules.resetPriceAfterWipeoutCents;
                    }
                    else
                    {
                        market.PricesCents[idx] = next;
                        outcome.Messages.Add($"{roll.Commodity} down {roll.Cents}c -> ${market.PricesCents[idx] / 100f:F2}");
                    }

                    break;
                }
                case MovementKind.Dividend:
                {
                    if (price < rules.dividendMinimumPriceCents)
                    {
                        outcome.Messages.Add($"{roll.Commodity} dividend roll ignored (price ${price / 100f:F2} below ${rules.dividendMinimumPriceCents / 100f:F2})");
                        break;
                    }

                    long youPaidCents = 0;
                    for (var p = 0; p < players.Count; p++)
                    {
                        var pl = players[p];
                        var shares = pl.SharesByCommodity[idx];
                        if (shares <= 0) continue;
                        var pay = (long)shares * roll.Cents;
                        pl.CashCents += (int)System.Math.Min(int.MaxValue - pl.CashCents, pay);
                        players[p] = pl;
                        if (p == 0)
                            youPaidCents += pay;
                    }

                    outcome.Messages.Add($"You received ${youPaidCents / 100f:N0} from the dividend.");
                    break;
                }
            }
        }
    }
}
