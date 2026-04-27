using System.Collections.Generic;
using System;
using StockPicker.Game.Core;

namespace StockPicker.Game.Progression
{
    public static class ProgressionService
    {
        private static readonly (int winsNeeded, string id)[] Milestones =
        {
            (1, "theme_ocean"),
            (2, "table_wood_dark"),
            (3, "theme_neon"),
            (4, "sfx_pack_chime"),
            (5, "theme_paper"),
            (6, "announcer_minimal")
        };
        private const int MaxGlobalLeaderboardEntries = 50;

        public static void OnSeasonEnd(ProgressionState prog, int humanPlayerIndex, int winnerIndex, long beatMarketCents, string humanPlayerName)
        {
            prog.SeasonsCompleted++;
            if (winnerIndex == humanPlayerIndex)
                prog.HumanSeasonWins++;
            if (beatMarketCents > 0)
                prog.HumanBeatMarketSeasons++;
            prog.HumanLifetimeBeatMarketCents += beatMarketCents;
            if (beatMarketCents > prog.HumanBestBeatMarketCents)
                prog.HumanBestBeatMarketCents = beatMarketCents;

            foreach (var (wins, id) in Milestones)
            {
                if (prog.HumanSeasonWins >= wins && !prog.UnlockedCosmeticIds.Contains(id))
                    prog.UnlockedCosmeticIds.Add(id);
            }

            UpsertGlobalScoreboard(prog, humanPlayerName, beatMarketCents);
        }

        private static void UpsertGlobalScoreboard(ProgressionState prog, string playerName, long beatMarketCents)
        {
            if (prog.HumanGlobalScoreboard == null)
                prog.HumanGlobalScoreboard = new List<HumanTraderScoreEntry>();

            var normalizedName = string.IsNullOrWhiteSpace(playerName) ? "You" : playerName.Trim();
            HumanTraderScoreEntry row = null;
            for (var i = 0; i < prog.HumanGlobalScoreboard.Count; i++)
            {
                if (!string.Equals(prog.HumanGlobalScoreboard[i].PlayerName, normalizedName, StringComparison.OrdinalIgnoreCase))
                    continue;
                row = prog.HumanGlobalScoreboard[i];
                break;
            }

            if (row == null)
            {
                row = new HumanTraderScoreEntry
                {
                    PlayerName = normalizedName,
                    SeasonsPlayed = 0,
                    TotalBeatMarketCents = 0,
                    BestBeatMarketCents = long.MinValue
                };
                prog.HumanGlobalScoreboard.Add(row);
            }

            row.SeasonsPlayed++;
            row.TotalBeatMarketCents += beatMarketCents;
            if (beatMarketCents > row.BestBeatMarketCents)
                row.BestBeatMarketCents = beatMarketCents;
            row.LastUpdatedUtc = DateTime.UtcNow.ToString("O");

            prog.HumanGlobalScoreboard.Sort((a, b) =>
            {
                var totalCompare = b.TotalBeatMarketCents.CompareTo(a.TotalBeatMarketCents);
                if (totalCompare != 0) return totalCompare;
                return b.BestBeatMarketCents.CompareTo(a.BestBeatMarketCents);
            });

            if (prog.HumanGlobalScoreboard.Count > MaxGlobalLeaderboardEntries)
                prog.HumanGlobalScoreboard.RemoveRange(MaxGlobalLeaderboardEntries, prog.HumanGlobalScoreboard.Count - MaxGlobalLeaderboardEntries);
        }

        public static IReadOnlyList<string> ListUnlocks(ProgressionState prog) => prog.UnlockedCosmeticIds;
    }
}
