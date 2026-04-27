using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StockPicker.Game.Progression;
using StockPicker.Infrastructure.Backend;

namespace StockPicker.Tests
{
    public sealed class BackendSyncTests
    {
        private sealed class FailThenSucceedScoreboardService : IScoreboardService
        {
            private int _calls;

            public Task<ScoreSubmissionResult> SubmitSeasonScoreAsync(SeasonScoreSubmission submission, CancellationToken cancellationToken = default)
            {
                _calls++;
                if (_calls == 1)
                    return Task.FromResult(new ScoreSubmissionResult(false, "Transient error"));
                return Task.FromResult(new ScoreSubmissionResult(true, string.Empty));
            }

            public Task<ScoreboardFetchResult> FetchTopAsync(int count, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ScoreboardFetchResult(true, string.Empty, new List<ScoreboardEntry>(), null));
            }
        }

        [Test]
        public async Task QueueFlush_RetainsFailedRows_ThenSucceedsOnRetry()
        {
            var pending = new List<PendingSeasonScoreSubmission>
            {
                new()
                {
                    PlayerName = "You",
                    SeasonIndex = 1,
                    BeatMarketCents = 1000,
                    SeasonKey = "seed:1"
                }
            };
            var scoreboard = new FailThenSucceedScoreboardService();

            var first = await CloudSyncQueueProcessor.FlushAsync(pending, scoreboard);
            Assert.AreEqual(0, first.SubmittedCount);
            Assert.AreEqual(1, first.RemainingCount);
            Assert.AreEqual(1, pending[0].AttemptCount);

            var second = await CloudSyncQueueProcessor.FlushAsync(pending, scoreboard);
            Assert.AreEqual(1, second.SubmittedCount);
            Assert.AreEqual(0, second.RemainingCount);
        }

        [Test]
        public async Task PlayFabAuth_FailsCleanly_WhenTitleIdMissing()
        {
            var auth = new PlayFabGoogleAuthService(new BackendConfig { PlayFabTitleId = "" });
            var result = await auth.SignInAsync();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(AuthPhase.SignedOut, result.State.Phase);
            Assert.That(result.Error, Is.EqualTo("PlayFab title id missing."));
        }
    }
}
