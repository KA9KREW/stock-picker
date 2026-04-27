using UnityEngine;

namespace StockPicker.Game.Core
{
    [CreateAssetMenu(fileName = "GameRules", menuName = "StockPicker/Game Rules", order = 0)]
    public class GameRules : ScriptableObject
    {
        [Header("Economy")]
        [Tooltip("Starting cash per player in cents (5000 dollars = 500000 cents).")]
        public int startingCashCents = 500_000;

        [Tooltip("Starting price per commodity in cents ($1.00 = 100).")]
        public int startingPriceCents = 100;

        [Tooltip("Split threshold in cents ($2.00 = 200).")]
        public int splitThresholdCents = 200;

        [Tooltip("Price after split in cents.")]
        public int priceAfterSplitCents = 100;

        [Tooltip("Minimum price before wipeout (0 = bankrupt stock).")]
        public int wipeoutAtCents = 0;

        [Tooltip("Reset price after wipeout.")]
        public int resetPriceAfterWipeoutCents = 100;

        [Header("Trading")]
        public int[] allowedShareLots = { 500, 1000, 2000, 5000 };

        [Tooltip("Dice rolls between trading windows.")]
        public int rollsPerTradingWindow = 5;

        [Tooltip("Total dice rolls (days) in one season.")]
        public int rollsPerSeason = 365;

        [Header("Campaign win (runtime / new game)")]
        public CampaignWinMode campaignWinMode = CampaignWinMode.Seasons;

        [Tooltip("First-to win: net worth in cents ($100,000 = 10,000,000).")]
        public int netWorthGoalCents = 10_000_000;

        [Tooltip("Total roll limit mode: campaign ends after this many dice rolls (e.g. 600 or 12,000).")]
        public int totalDiceRollsGoal = 12_000;

        [Header("Dividends")]
        [Tooltip("Minimum share price in cents to pay dividends when dividend is rolled.")]
        public int dividendMinimumPriceCents = 100;

        public static GameRules CreateDefaultRuntime()
        {
            var r = CreateInstance<GameRules>();
            r.startingCashCents = 500_000;
            r.startingPriceCents = 100;
            r.splitThresholdCents = 200;
            r.priceAfterSplitCents = 100;
            r.wipeoutAtCents = 0;
            r.resetPriceAfterWipeoutCents = 100;
            r.allowedShareLots = new[] { 500, 1000, 2000, 5000 };
            r.rollsPerTradingWindow = 5;
            r.rollsPerSeason = 365;
            r.campaignWinMode = CampaignWinMode.Seasons;
            r.netWorthGoalCents = 10_000_000;
            r.totalDiceRollsGoal = 12_000;
            r.dividendMinimumPriceCents = 100;
            return r;
        }

        /// <summary>Runtime copy with win rules for a menu preset (do not persist the returned instance as an asset).</summary>
        public static GameRules ConfigurePreset(NewGamePreset preset, GameRules template)
        {
            var r = template != null ? Instantiate(template) : CreateDefaultRuntime();
            r.hideFlags = HideFlags.HideAndDontSave;
            switch (preset)
            {
                case NewGamePreset.ClassicSeasons:
                    r.campaignWinMode = CampaignWinMode.Seasons;
                    r.rollsPerSeason = 365;
                    break;
                case NewGamePreset.FirstTo100K:
                    r.campaignWinMode = CampaignWinMode.FirstToNetWorth;
                    r.netWorthGoalCents = 10_000_000;
                    break;
                case NewGamePreset.TwelveThousandRolls:
                    r.campaignWinMode = CampaignWinMode.TotalDiceRolls;
                    r.totalDiceRollsGoal = 12_000;
                    break;
                case NewGamePreset.Fast600Rolls:
                    r.campaignWinMode = CampaignWinMode.TotalDiceRolls;
                    r.totalDiceRollsGoal = 600;
                    break;
            }

            if (r.netWorthGoalCents <= 0) r.netWorthGoalCents = 10_000_000;
            if (r.totalDiceRollsGoal <= 0) r.totalDiceRollsGoal = 12_000;
            return r;
        }

        public static GameRules FromPersistedState(GameStateSnapshot s, GameRules template)
        {
            var r = template != null ? Instantiate(template) : CreateDefaultRuntime();
            r.hideFlags = HideFlags.HideAndDontSave;
            if (s.SchemaVersion >= 2)
            {
                r.campaignWinMode = s.CampaignWinMode;
                r.netWorthGoalCents = s.PersistedNetWorthGoalCents;
                r.totalDiceRollsGoal = s.PersistedTotalDiceRollsGoal;
            }
            else
                r.campaignWinMode = CampaignWinMode.Seasons;

            if (r.netWorthGoalCents <= 0) r.netWorthGoalCents = 10_000_000;
            if (r.totalDiceRollsGoal <= 0) r.totalDiceRollsGoal = 12_000;
            return r;
        }
    }
}
