using System;
using System.Collections.Generic;

namespace StockPicker.Game.Core
{
    /// <summary>
    /// Serializable full state for save/load and simulation.
    /// </summary>
    [Serializable]
    public class GameStateSnapshot
    {
        public int SchemaVersion = 2;
        public int RandomSeed;
        /// <summary>Persisted RNG state for resume (0 = derive from seed only on first run).</summary>
        public long RngState;
        public int RollIndexInSeason;
        public int SeasonIndex;
        public GamePhase Phase;
        public MarketState Market;
        public List<PlayerPortfolio> Players = new();
        public List<string> RollLog = new();
        public List<string> SeasonSummaryLog = new();

        public CampaignWinMode CampaignWinMode = CampaignWinMode.Seasons;
        public int PersistedNetWorthGoalCents = 10_000_000;
        public int PersistedTotalDiceRollsGoal = 12_000;
        public int TotalDiceRollsCampaign;

        public GameStateSnapshot Clone()
        {
            var c = new GameStateSnapshot
            {
                SchemaVersion = SchemaVersion,
                RandomSeed = RandomSeed,
                RngState = RngState,
                RollIndexInSeason = RollIndexInSeason,
                SeasonIndex = SeasonIndex,
                Phase = Phase,
                Market = new MarketState
                {
                    PricesCents = Market.PricesCents != null ? (int[])Market.PricesCents.Clone() : new int[6]
                },
                Players = new List<PlayerPortfolio>(),
                RollLog = new List<string>(RollLog),
                SeasonSummaryLog = new List<string>(SeasonSummaryLog),
                CampaignWinMode = CampaignWinMode,
                PersistedNetWorthGoalCents = PersistedNetWorthGoalCents,
                PersistedTotalDiceRollsGoal = PersistedTotalDiceRollsGoal,
                TotalDiceRollsCampaign = TotalDiceRollsCampaign
            };
            foreach (var p in Players)
            {
                var pc = p;
                pc.SharesByCommodity = (int[])p.SharesByCommodity.Clone();
                c.Players.Add(pc);
            }

            return c;
        }
    }
}
