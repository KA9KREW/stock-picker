using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace StockPicker.Infrastructure.Backend
{
    [Serializable]
    public sealed class BackendConfig
    {
        public string PlayFabTitleId = "";
        public string GoogleWebClientId = "";
        public string LifetimeStatisticName = "BeatMarketLifetimeCents";
        public string BestSeasonStatisticName = "BeatMarketBestSeasonCents";
    }

    public sealed class PlayFabGoogleAuthService : IAuthService
    {
        private readonly BackendConfig _config;
        private AuthState _state;

        public PlayFabGoogleAuthService(BackendConfig config)
        {
            _config = config ?? new BackendConfig();
            _state = AuthState.SignedOut("Sign in to sync global leaderboard.");
        }

        public AuthState State => _state;
        public event Action<AuthState> StateChanged;

        public async Task<AuthResult> SignInAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_config.PlayFabTitleId))
            {
                _state = AuthState.SignedOut("PlayFab title id missing.");
                StateChanged?.Invoke(_state);
                return new AuthResult(false, _state.StatusMessage, _state);
            }

            SetState(AuthPhase.SigningIn, string.Empty, string.Empty, "Signing in...");

#if PLAYFAB_SDK && GOOGLE_SIGNIN_SDK
            try
            {
                // NOTE: guarded behind compile defines so projects without SDKs still compile.
                var token = await PlayFabGoogleSdkBridge.RequestGoogleServerAuthCodeAsync(_config.GoogleWebClientId, cancellationToken);
                if (string.IsNullOrWhiteSpace(token))
                {
                    SetState(AuthPhase.Error, string.Empty, "Guest", "Google sign-in canceled.");
                    return new AuthResult(false, "Google sign-in canceled.", _state);
                }

                var login = await PlayFabGoogleSdkBridge.LoginWithGoogleAsync(_config.PlayFabTitleId, token, cancellationToken);
                if (!login.Success)
                {
                    SetState(AuthPhase.Error, string.Empty, "Guest", login.Error);
                    return new AuthResult(false, login.Error, _state);
                }

                SetState(AuthPhase.SignedIn, login.PlayerId, login.DisplayName, "Connected.");
                return new AuthResult(true, string.Empty, _state);
            }
            catch (Exception ex)
            {
                SetState(AuthPhase.Error, string.Empty, "Guest", ex.Message);
                return new AuthResult(false, ex.Message, _state);
            }
#else
            await Task.Yield();
            SetState(AuthPhase.Error, string.Empty, "Guest",
                "PlayFab/Google SDK not installed. Define PLAYFAB_SDK and GOOGLE_SIGNIN_SDK after importing packages.");
            return new AuthResult(false, _state.StatusMessage, _state);
#endif
        }

        public void SignOut()
        {
            SetState(AuthPhase.SignedOut, string.Empty, "Guest", "Signed out.");
        }

        private void SetState(AuthPhase phase, string playerId, string displayName, string status)
        {
            _state = new AuthState
            {
                Phase = phase,
                PlayerId = playerId ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Guest" : displayName,
                StatusMessage = status ?? string.Empty
            };
            StateChanged?.Invoke(_state);
        }
    }

    public sealed class PlayFabScoreboardService : IScoreboardService
    {
        private readonly BackendConfig _config;
        private readonly IAuthService _auth;

        public PlayFabScoreboardService(BackendConfig config, IAuthService auth)
        {
            _config = config ?? new BackendConfig();
            _auth = auth;
        }

        public async Task<ScoreSubmissionResult> SubmitSeasonScoreAsync(SeasonScoreSubmission submission, CancellationToken cancellationToken = default)
        {
            if (_auth == null || !_auth.State.IsAuthenticated)
                return new ScoreSubmissionResult(false, "Not authenticated.");

#if PLAYFAB_SDK
            var submit = await PlayFabGoogleSdkBridge.UpsertPlayerBeatMarketStatsAsync(
                _config.PlayFabTitleId,
                _config.LifetimeStatisticName,
                _config.BestSeasonStatisticName,
                submission.BeatMarketCents,
                cancellationToken);
            return new ScoreSubmissionResult(submit.Success, submit.Error);
#else
            await Task.Yield();
            return new ScoreSubmissionResult(false, "PlayFab SDK not installed.");
#endif
        }

        public async Task<ScoreboardFetchResult> FetchTopAsync(int count, CancellationToken cancellationToken = default)
        {
            if (_auth == null || !_auth.State.IsAuthenticated)
                return new ScoreboardFetchResult(false, "Not authenticated.", Array.Empty<ScoreboardEntry>(), null);

#if PLAYFAB_SDK
            var rows = await PlayFabGoogleSdkBridge.FetchLeaderboardAsync(
                _config.LifetimeStatisticName,
                Mathf.Max(1, count),
                cancellationToken);
            if (!rows.Success)
                return new ScoreboardFetchResult(false, rows.Error, Array.Empty<ScoreboardEntry>(), null);

            var mapped = new List<ScoreboardEntry>(rows.Entries.Count);
            for (var i = 0; i < rows.Entries.Count; i++)
                mapped.Add(rows.Entries[i]);
            return new ScoreboardFetchResult(true, string.Empty, mapped, null);
#else
            await Task.Yield();
            return new ScoreboardFetchResult(false, "PlayFab SDK not installed.", Array.Empty<ScoreboardEntry>(), null);
#endif
        }
    }
}
