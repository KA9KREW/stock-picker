using System;
using System.Collections.Generic;

namespace StockPicker.Infrastructure.Backend
{
    [Serializable]
    public struct SeasonScoreSubmission
    {
        public string PlayerName;
        public int SeasonIndex;
        public long BeatMarketCents;
        public string SeasonKey;
    }

    [Serializable]
    public struct ScoreboardEntry
    {
        public string PlayerId;
        public string DisplayName;
        public int SeasonsPlayed;
        public long LifetimeBeatMarketCents;
        public long BestSeasonBeatMarketCents;
        public int GlobalRank;
    }

    public readonly struct ScoreSubmissionResult
    {
        public readonly bool Success;
        public readonly string Error;

        public ScoreSubmissionResult(bool success, string error)
        {
            Success = success;
            Error = error ?? string.Empty;
        }
    }

    public readonly struct ScoreboardFetchResult
    {
        public readonly bool Success;
        public readonly string Error;
        public readonly IReadOnlyList<ScoreboardEntry> Entries;
        public readonly ScoreboardEntry? CurrentPlayer;

        public ScoreboardFetchResult(bool success, string error, IReadOnlyList<ScoreboardEntry> entries, ScoreboardEntry? currentPlayer)
        {
            Success = success;
            Error = error ?? string.Empty;
            Entries = entries ?? Array.Empty<ScoreboardEntry>();
            CurrentPlayer = currentPlayer;
        }
    }
}
