using System.Collections.Generic;
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

        public static void OnSeasonEnd(ProgressionState prog, int humanPlayerIndex, int winnerIndex)
        {
            prog.SeasonsCompleted++;
            if (winnerIndex == humanPlayerIndex)
                prog.HumanSeasonWins++;

            foreach (var (wins, id) in Milestones)
            {
                if (prog.HumanSeasonWins >= wins && !prog.UnlockedCosmeticIds.Contains(id))
                    prog.UnlockedCosmeticIds.Add(id);
            }
        }

        public static IReadOnlyList<string> ListUnlocks(ProgressionState prog) => prog.UnlockedCosmeticIds;
    }
}
