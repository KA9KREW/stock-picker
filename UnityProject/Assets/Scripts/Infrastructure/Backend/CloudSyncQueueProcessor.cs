using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockPicker.Game.Progression;

namespace StockPicker.Infrastructure.Backend
{
    public readonly struct QueueFlushResult
    {
        public readonly int SubmittedCount;
        public readonly int RemainingCount;
        public readonly string LastError;

        public QueueFlushResult(int submittedCount, int remainingCount, string lastError)
        {
            SubmittedCount = submittedCount;
            RemainingCount = remainingCount;
            LastError = lastError ?? string.Empty;
        }
    }

    public static class CloudSyncQueueProcessor
    {
        public static async Task<QueueFlushResult> FlushAsync(
            List<PendingSeasonScoreSubmission> pending,
            IScoreboardService scoreboard,
            CancellationToken cancellationToken = default)
        {
            if (pending == null || scoreboard == null || pending.Count == 0)
                return new QueueFlushResult(0, pending?.Count ?? 0, string.Empty);

            var submitted = 0;
            var lastError = string.Empty;
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = pending[i];
                row.AttemptCount++;
                row.LastAttemptUtc = DateTime.UtcNow.ToString("O");
                pending[i] = row;

                var submit = await scoreboard.SubmitSeasonScoreAsync(new SeasonScoreSubmission
                {
                    PlayerName = row.PlayerName,
                    SeasonIndex = row.SeasonIndex,
                    BeatMarketCents = row.BeatMarketCents,
                    SeasonKey = row.SeasonKey
                }, cancellationToken);

                if (submit.Success)
                {
                    pending.RemoveAt(i);
                    submitted++;
                }
                else
                {
                    lastError = submit.Error;
                }
            }

            return new QueueFlushResult(submitted, pending.Count, lastError);
        }
    }
}
