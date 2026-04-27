using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StockPicker.Infrastructure.Backend
{
    public sealed class LocalMockAuthService : IAuthService
    {
        private AuthState _state = AuthState.SignedOut("Offline mock mode.");

        public AuthState State => _state;
        public event Action<AuthState> StateChanged;

        public Task<AuthResult> SignInAsync(CancellationToken cancellationToken = default)
        {
            _state = new AuthState
            {
                Phase = AuthPhase.SignedIn,
                PlayerId = "local_mock_player",
                DisplayName = "Mock Trader",
                StatusMessage = "Signed in (mock)."
            };
            StateChanged?.Invoke(_state);
            return Task.FromResult(new AuthResult(true, string.Empty, _state));
        }

        public void SignOut()
        {
            _state = AuthState.SignedOut("Signed out.");
            StateChanged?.Invoke(_state);
        }
    }

    public sealed class LocalMockScoreboardService : IScoreboardService
    {
        private readonly List<ScoreboardEntry> _entries = new();

        public Task<ScoreSubmissionResult> SubmitSeasonScoreAsync(SeasonScoreSubmission submission, CancellationToken cancellationToken = default)
        {
            var idx = _entries.FindIndex(e => string.Equals(e.DisplayName, submission.PlayerName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                _entries.Add(new ScoreboardEntry
                {
                    PlayerId = "local_" + Guid.NewGuid().ToString("N"),
                    DisplayName = submission.PlayerName,
                    SeasonsPlayed = 1,
                    LifetimeBeatMarketCents = submission.BeatMarketCents,
                    BestSeasonBeatMarketCents = submission.BeatMarketCents
                });
            }
            else
            {
                var row = _entries[idx];
                row.SeasonsPlayed++;
                row.LifetimeBeatMarketCents += submission.BeatMarketCents;
                row.BestSeasonBeatMarketCents = Math.Max(row.BestSeasonBeatMarketCents, submission.BeatMarketCents);
                _entries[idx] = row;
            }

            _entries.Sort((a, b) => b.LifetimeBeatMarketCents.CompareTo(a.LifetimeBeatMarketCents));
            for (var i = 0; i < _entries.Count; i++)
            {
                var row = _entries[i];
                row.GlobalRank = i + 1;
                _entries[i] = row;
            }

            return Task.FromResult(new ScoreSubmissionResult(true, string.Empty));
        }

        public Task<ScoreboardFetchResult> FetchTopAsync(int count, CancellationToken cancellationToken = default)
        {
            var take = Math.Max(0, Math.Min(count, _entries.Count));
            var top = new List<ScoreboardEntry>(take);
            for (var i = 0; i < take; i++)
                top.Add(_entries[i]);
            return Task.FromResult(new ScoreboardFetchResult(true, string.Empty, top, null));
        }
    }
}
