using System;
using System.Collections.Generic;

namespace StockPicker.Game.Progression
{
    [Serializable]
    public class ProgressionState
    {
        public int SchemaVersion = 1;
        public int SeasonsCompleted;
        public int HumanSeasonWins;
        public List<string> UnlockedCosmeticIds = new();

        public static ProgressionState New()
        {
            var p = new ProgressionState();
            p.UnlockedCosmeticIds.Add("theme_default");
            return p;
        }
    }
}
