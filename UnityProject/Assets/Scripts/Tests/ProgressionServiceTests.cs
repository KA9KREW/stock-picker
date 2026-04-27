using NUnit.Framework;
using StockPicker.Game.Progression;

namespace StockPicker.Tests
{
    public sealed class ProgressionServiceTests
    {
        [Test]
        public void OnSeasonEnd_TracksBeatMarketAndUpdatesGlobalLeaderboard()
        {
            var progression = ProgressionState.New();

            ProgressionService.OnSeasonEnd(progression, 0, 0, 12_500, "You");
            ProgressionService.OnSeasonEnd(progression, 0, 2, -5_000, "You");

            Assert.AreEqual(2, progression.SeasonsCompleted);
            Assert.AreEqual(1, progression.HumanSeasonWins);
            Assert.AreEqual(1, progression.HumanBeatMarketSeasons);
            Assert.AreEqual(7_500, progression.HumanLifetimeBeatMarketCents);
            Assert.AreEqual(12_500, progression.HumanBestBeatMarketCents);

            Assert.AreEqual(1, progression.HumanGlobalScoreboard.Count);
            var row = progression.HumanGlobalScoreboard[0];
            Assert.AreEqual("You", row.PlayerName);
            Assert.AreEqual(2, row.SeasonsPlayed);
            Assert.AreEqual(7_500, row.TotalBeatMarketCents);
            Assert.AreEqual(12_500, row.BestBeatMarketCents);
        }
    }
}
