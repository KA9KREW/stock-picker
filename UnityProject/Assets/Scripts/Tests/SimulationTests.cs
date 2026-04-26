using NUnit.Framework;
using StockPicker.Game.Core;

namespace StockPicker.Tests
{
    public sealed class SimulationTests
    {
        [Test]
        public void AdvanceRolls_DoNotThrow_AndSeasonCompletes()
        {
            var rules = GameRules.CreateDefaultRuntime();
            rules.rollsPerSeason = 20;
            rules.rollsPerTradingWindow = 4;
            var session = GameSession.NewGame(rules, 42, "Human");

            for (var i = 0; i < 500; i++)
            {
                if (session.State.Phase == GamePhase.SeasonComplete)
                    break;

                if (session.State.Phase == GamePhase.Trading)
                {
                    session.SkipTradingWindow();
                    continue;
                }

                session.AdvanceRoll(out _);
            }

            Assert.AreEqual(GamePhase.SeasonComplete, session.State.Phase);
            Assert.AreEqual(7, session.State.Players.Count);
        }

        [Test]
        public void AdvanceRolls_FirstToNetWorth_DoesNotEndSeasonAtRollsPerSeason()
        {
            var rules = GameRules.CreateDefaultRuntime();
            rules.campaignWinMode = CampaignWinMode.FirstToNetWorth;
            rules.rollsPerSeason = 50;
            rules.rollsPerTradingWindow = 5;
            var session = GameSession.NewGame(rules, 42, "Human");

            for (var i = 0; i < 55; i++)
            {
                if (session.State.Phase == GamePhase.Trading)
                    session.SkipTradingWindow();
                else if (session.State.Phase == GamePhase.Rolling)
                    session.AdvanceRoll(out _);
                else
                    Assert.Fail($"Unexpected phase {session.State.Phase} at iteration {i}");
            }

            Assert.AreNotEqual(GamePhase.SeasonComplete, session.State.Phase);
            Assert.That(session.State.RollIndexInSeason, Is.GreaterThanOrEqualTo(rules.rollsPerSeason));
        }

        [Test]
        public void Split_IncreasesShares_AndResetsPrice()
        {
            var rules = GameRules.CreateDefaultRuntime();
            var session = GameSession.NewGame(rules, 99, "Human");
            var p0 = session.State.Players[0];
            p0.SharesByCommodity[(int)CommodityId.Gold] = 1000;
            session.State.Players[0] = p0;
            session.State.Market.PricesCents[(int)CommodityId.Gold] = 190;
            var roll = new DiceRoll(CommodityId.Gold, MovementKind.Up, 10);
            MarketResolver.ApplyRoll(rules, roll, session.State.Market, session.State.Players, new MarketResolver.RollOutcome());
            Assert.AreEqual(100, session.State.Market.PricesCents[(int)CommodityId.Gold]);
            Assert.AreEqual(2000, session.State.Players[0].SharesByCommodity[(int)CommodityId.Gold]);
        }
    }
}
