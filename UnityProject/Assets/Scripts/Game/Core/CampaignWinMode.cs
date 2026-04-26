namespace StockPicker.Game.Core
{
    /// <summary>How a campaign ends (persisted on <see cref="GameStateSnapshot"/>).</summary>
    public enum CampaignWinMode : byte
    {
        /// <summary>Ends after a configured number of seasons (host / inspector).</summary>
        Seasons = 0,

        /// <summary>First player to reach <see cref="GameRules.netWorthGoalCents"/> net worth wins.</summary>
        FirstToNetWorth = 1,

        /// <summary>Campaign ends after <see cref="GameRules.totalDiceRollsGoal"/> dice rolls (cumulative).</summary>
        TotalDiceRolls = 2
    }
}
