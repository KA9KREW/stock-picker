using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if GOOGLE_SIGNIN_SDK
using Google;
#endif
#if PLAYFAB_SDK
using PlayFab;
using PlayFab.ClientModels;
#endif

namespace StockPicker.Infrastructure.Backend
{
    internal static class PlayFabGoogleSdkBridge
    {
        internal readonly struct PlayFabLoginResult
        {
            public readonly bool Success;
            public readonly string Error;
            public readonly string PlayerId;
            public readonly string DisplayName;

            public PlayFabLoginResult(bool success, string error, string playerId, string displayName)
            {
                Success = success;
                Error = error ?? string.Empty;
                PlayerId = playerId ?? string.Empty;
                DisplayName = displayName ?? string.Empty;
            }
        }

        internal readonly struct BridgeResult
        {
            public readonly bool Success;
            public readonly string Error;

            public BridgeResult(bool success, string error)
            {
                Success = success;
                Error = error ?? string.Empty;
            }
        }

        internal readonly struct LeaderboardBridgeResult
        {
            public readonly bool Success;
            public readonly string Error;
            public readonly IReadOnlyList<ScoreboardEntry> Entries;

            public LeaderboardBridgeResult(bool success, string error, IReadOnlyList<ScoreboardEntry> entries)
            {
                Success = success;
                Error = error ?? string.Empty;
                Entries = entries ?? Array.Empty<ScoreboardEntry>();
            }
        }

        public static Task<string> RequestGoogleServerAuthCodeAsync(string googleWebClientId, CancellationToken cancellationToken)
        {
#if GOOGLE_SIGNIN_SDK
            if (string.IsNullOrWhiteSpace(googleWebClientId))
                return Task.FromResult(string.Empty);

            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = googleWebClientId,
                RequestIdToken = true,
                RequestEmail = true,
                RequestAuthCode = true,
                ForceTokenRefresh = false
            };
            GoogleSignIn.DefaultInstance.SignOut();

            var tcs = new TaskCompletionSource<string>();
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                if (task.IsFaulted || task.IsCanceled || task.Result == null)
                {
                    tcs.TrySetResult(string.Empty);
                    return;
                }

                tcs.TrySetResult(task.Result.AuthCode ?? string.Empty);
            });
            return tcs.Task;
#endif
            return Task.FromResult(string.Empty);
        }

        public static Task<PlayFabLoginResult> LoginWithGoogleAsync(string titleId, string googleServerAuthCode, CancellationToken cancellationToken)
        {
#if PLAYFAB_SDK
            if (string.IsNullOrWhiteSpace(titleId))
                return Task.FromResult(new PlayFabLoginResult(false, "PlayFab title id missing.", string.Empty, string.Empty));
            if (string.IsNullOrWhiteSpace(googleServerAuthCode))
                return Task.FromResult(new PlayFabLoginResult(false, "Google auth code missing.", string.Empty, string.Empty));

            PlayFabSettings.staticSettings.TitleId = titleId;
            var tcs = new TaskCompletionSource<PlayFabLoginResult>();
            var req = new LoginWithGoogleAccountRequest
            {
                ServerAuthCode = googleServerAuthCode,
                CreateAccount = true,
                TitleId = titleId,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };

            PlayFabClientAPI.LoginWithGoogleAccount(req, result =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                var displayName = result?.InfoResultPayload?.PlayerProfile?.DisplayName;
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = "Trader";
                tcs.TrySetResult(new PlayFabLoginResult(true, string.Empty, result?.PlayFabId ?? string.Empty, displayName));
            }, err =>
            {
                tcs.TrySetResult(new PlayFabLoginResult(false, err.GenerateErrorReport(), string.Empty, string.Empty));
            });
            return tcs.Task;
#endif
            return Task.FromResult(new PlayFabLoginResult(false, "PlayFab SDK bridge not wired.", string.Empty, string.Empty));
        }

        public static Task<BridgeResult> UpsertPlayerBeatMarketStatsAsync(
            string titleId,
            string lifetimeStatisticName,
            string bestSeasonStatisticName,
            long beatMarketCents,
            CancellationToken cancellationToken)
        {
#if PLAYFAB_SDK
            if (string.IsNullOrWhiteSpace(titleId))
                return Task.FromResult(new BridgeResult(false, "PlayFab title id missing."));
            PlayFabSettings.staticSettings.TitleId = titleId;

            var getStatsTcs = new TaskCompletionSource<GetPlayerStatisticsResult>();
            PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), res => getStatsTcs.TrySetResult(res),
                err => getStatsTcs.TrySetException(new Exception(err.GenerateErrorReport())));

            return UpsertStatsInternalAsync(
                lifetimeStatisticName,
                bestSeasonStatisticName,
                beatMarketCents,
                getStatsTcs.Task,
                cancellationToken);
#endif
            return Task.FromResult(new BridgeResult(false, "PlayFab SDK bridge not wired."));
        }

        public static Task<LeaderboardBridgeResult> FetchLeaderboardAsync(string statisticName, int maxResults, CancellationToken cancellationToken)
        {
#if PLAYFAB_SDK
            var tcs = new TaskCompletionSource<LeaderboardBridgeResult>();
            var request = new GetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = 0,
                MaxResultsCount = Math.Max(1, maxResults),
                ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
            };
            PlayFabClientAPI.GetLeaderboard(request, result =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                var rows = new List<ScoreboardEntry>(result.Leaderboard?.Count ?? 0);
                if (result.Leaderboard != null)
                {
                    for (var i = 0; i < result.Leaderboard.Count; i++)
                    {
                        var row = result.Leaderboard[i];
                        rows.Add(new ScoreboardEntry
                        {
                            PlayerId = row.PlayFabId ?? string.Empty,
                            DisplayName = string.IsNullOrWhiteSpace(row.DisplayName) ? "Trader" : row.DisplayName,
                            SeasonsPlayed = 0,
                            LifetimeBeatMarketCents = row.StatValue,
                            BestSeasonBeatMarketCents = 0,
                            GlobalRank = row.Position + 1
                        });
                    }
                }

                tcs.TrySetResult(new LeaderboardBridgeResult(true, string.Empty, rows));
            }, err => { tcs.TrySetResult(new LeaderboardBridgeResult(false, err.GenerateErrorReport(), Array.Empty<ScoreboardEntry>())); });
            return tcs.Task;
#endif
            return Task.FromResult(new LeaderboardBridgeResult(false, "PlayFab SDK bridge not wired.", Array.Empty<ScoreboardEntry>()));
        }

#if PLAYFAB_SDK
        private static async Task<BridgeResult> UpsertStatsInternalAsync(
            string lifetimeStatisticName,
            string bestSeasonStatisticName,
            long beatMarketCents,
            Task<GetPlayerStatisticsResult> statsTask,
            CancellationToken cancellationToken)
        {
            try
            {
                var stats = await statsTask;
                cancellationToken.ThrowIfCancellationRequested();

                var currentLifetime = 0;
                var currentBest = int.MinValue;
                if (stats?.Statistics != null)
                {
                    var lifetime = stats.Statistics.FirstOrDefault(s => s.StatisticName == lifetimeStatisticName);
                    var best = stats.Statistics.FirstOrDefault(s => s.StatisticName == bestSeasonStatisticName);
                    if (lifetime != null) currentLifetime = lifetime.Value;
                    if (best != null) currentBest = best.Value;
                }

                var targetLifetime = ClampToInt(currentLifetime + beatMarketCents);
                var targetBest = ClampToInt(Math.Max(currentBest, beatMarketCents));

                var updateTcs = new TaskCompletionSource<bool>();
                var updateRequest = new UpdatePlayerStatisticsRequest
                {
                    Statistics = new List<StatisticUpdate>
                    {
                        new() { StatisticName = lifetimeStatisticName, Value = targetLifetime },
                        new() { StatisticName = bestSeasonStatisticName, Value = targetBest }
                    }
                };
                PlayFabClientAPI.UpdatePlayerStatistics(updateRequest,
                    _ => updateTcs.TrySetResult(true),
                    err => updateTcs.TrySetException(new Exception(err.GenerateErrorReport())));

                await updateTcs.Task;
                return new BridgeResult(true, string.Empty);
            }
            catch (Exception ex)
            {
                return new BridgeResult(false, ex.Message);
            }
        }

        private static int ClampToInt(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }
#endif
    }
}
