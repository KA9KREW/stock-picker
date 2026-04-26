using StockPicker.Game.Core;
using StockPicker.Game.Progression;

namespace StockPicker.Infrastructure.Save
{
    /// <summary>
    /// Root DTO persisted to disk (Newtonsoft JSON).
    /// </summary>
    public sealed class PersistedCampaign
    {
        public int SaveVersion = 1;
        public GameStateSnapshot Game = new();
        public ProgressionState Progression = ProgressionState.New();
    }
}
