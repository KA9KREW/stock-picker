using System.IO;
using Newtonsoft.Json;
using StockPicker.Game.Core;
using StockPicker.Game.Progression;
using UnityEngine;

namespace StockPicker.Infrastructure.Save
{
    public sealed class SaveService
    {
        private const string FileName = "stockpicker_campaign.json";

        /// <summary>Previous save filename; still loaded if present so upgrades do not lose progress.</summary>
        private const string LegacyFileName = "stockticker_campaign.json";
        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private static string LegacyFilePath => Path.Combine(Application.persistentDataPath, LegacyFileName);

        public bool Exists() => File.Exists(FilePath) || File.Exists(LegacyFilePath);

        public void Save(PersistedCampaign data)
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            var json = JsonConvert.SerializeObject(data, Settings);
            File.WriteAllText(FilePath, json);
        }

        public PersistedCampaign LoadOrDefault(GameRules rulesFallback)
        {
            if (!Exists())
                return NewDefault(rulesFallback);
            var path = File.Exists(FilePath) ? FilePath : LegacyFilePath;
            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<PersistedCampaign>(json, Settings);
            if (data?.Game == null || data.Progression == null)
                return NewDefault(rulesFallback);
            if (data.Game.Players == null || data.Game.Players.Count == 0)
                return NewDefault(rulesFallback);
            return data;
        }

        public static PersistedCampaign NewDefault(GameRules rules)
        {
            var session = GameSession.NewGame(rules, Random.Range(1, int.MaxValue), "You");
            return new PersistedCampaign
            {
                SaveVersion = 1,
                Game = session.State,
                Progression = ProgressionState.New()
            };
        }
    }
}
