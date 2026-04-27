using System;
using System.Collections.Generic;

namespace StockPicker.Game.Progression
{
    [Serializable]
    public class HumanTraderScoreEntry
    {
        public string PlayerName = "You";
        public int SeasonsPlayed;
        public long TotalBeatMarketCents;
        public long BestBeatMarketCents;
        public string LastUpdatedUtc = string.Empty;
    }

    [Serializable]
    public class PendingSeasonScoreSubmission
    {
        public string PlayerName = "You";
        public int SeasonIndex;
        public long BeatMarketCents;
        public string SeasonKey = string.Empty;
        public int AttemptCount;
        public string LastAttemptUtc = string.Empty;
    }

    [Serializable]
    public class ProgressionState
    {
        public int SchemaVersion = 2;
        public int SeasonsCompleted;
        public int HumanSeasonWins;
        public int HumanBeatMarketSeasons;
        public long HumanLifetimeBeatMarketCents;
        public long HumanBestBeatMarketCents;
        public List<string> UnlockedCosmeticIds = new();
        public List<HumanTraderScoreEntry> HumanGlobalScoreboard = new();
        public List<PendingSeasonScoreSubmission> PendingCloudScoreSubmissions = new();

        public static ProgressionState New()
        {
            var p = new ProgressionState();
            p.UnlockedCosmeticIds.Add("theme_default");
            return p;
        }
    }
}
