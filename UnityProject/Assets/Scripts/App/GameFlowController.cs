using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockPicker.Game.AI;
using StockPicker.Game.Core;
using StockPicker.Game.Progression;
using StockPicker.Infrastructure.Backend;
using StockPicker.Infrastructure.Save;
using UnityEngine;

namespace StockPicker.App
{
    /// <summary>
    /// Session, save, and input actions — no UI building. Initialized by <see cref="StockPickerGameRoot"/>.
    /// </summary>
    public sealed class GameFlowController : MonoBehaviour
    {
        private GameRules _rulesTemplate;
        private GameRules _rules;
        private int _seasonsPerCampaign;
        private GameSession _session;
        private ProgressionState _progression;
        private readonly SaveService _save = new();
        private readonly List<TradeOrder> _humanOrders = new();
        private readonly List<TradeOrder> _scratchOrders = new();
        private IAuthService _authService;
        private IScoreboardService _scoreboardService;
        private bool _cloudBootstrapStarted;
        private bool _cloudSyncInFlight;
        private string _cloudStatus = "Cloud sync not initialized.";

        public event Action UiUpdated;

        public GameRules Rules => _rules;
        public GameSession Session => _session;
        public ProgressionState Progression => _progression;
        public IReadOnlyList<TradeOrder> HumanOrders => _humanOrders;
        public AuthState CloudAuthState => _authService?.State ?? AuthState.SignedOut("Auth unavailable.");
        public string CloudStatus => _cloudStatus;
        public bool CanBeginCloudSignIn => _authService != null && !_authService.State.IsAuthenticated &&
                                           _authService.State.Phase != AuthPhase.SigningIn;

        /// <summary>Last market dice outcome from <see cref="RollOrContinue"/>; null until the first roll of the session.</summary>
        public DiceRoll? LastDiceRoll => _lastDiceRoll;

        private DiceRoll? _lastDiceRoll;

        public void Initialize(GameRules rulesAsset, int seasonsPerCampaign, BackendConfig backendConfig, bool useLocalMockBackend)
        {
            _seasonsPerCampaign = seasonsPerCampaign;
            _rulesTemplate = rulesAsset;
            _rules = rulesAsset != null ? rulesAsset : GameRules.CreateDefaultRuntime();
            (_authService, _scoreboardService) = BackendServiceFactory.Create(backendConfig, useLocalMockBackend);
            if (_authService != null)
                _authService.StateChanged += HandleAuthStateChanged;
            _cloudStatus = "Sign in to post global scores.";
        }

        public void NewGameWithPreset(NewGamePreset preset)
        {
            _rules = GameRules.ConfigurePreset(preset, _rulesTemplate);
            NewGamePressed();
        }

        public void TryLoadOrNew()
        {
            if (_rules == null)
                _rules = GameRules.CreateDefaultRuntime();
            if (_save.Exists())
                LoadPressed();
            else
                NewGamePressed();
        }

        public void NewGamePressed()
        {
            var data = SaveService.NewDefault(_rules);
            _session = new GameSession(_rules, data.Game);
            _progression = data.Progression;
            _humanOrders.Clear();
            _lastDiceRoll = null;
            Persist();
            StartCloudBootstrap();
            NotifyUi();
        }

        public void LoadPressed()
        {
            var data = _save.LoadOrDefault(_rulesTemplate != null ? _rulesTemplate : _rules);
            NormalizeLoadedState(data.Game);
            NormalizeLoadedProgression(data.Progression);
            _rules = GameRules.FromPersistedState(data.Game, _rulesTemplate);
            _session = new GameSession(_rules, data.Game);
            _progression = data.Progression;
            _humanOrders.Clear();
            _lastDiceRoll = null;
            StartCloudBootstrap();
            NotifyUi();
        }

        public void Persist()
        {
            if (_session == null || _progression == null) return;
            _session.PersistRng();
            _save.Save(new PersistedCampaign { Game = _session.State, Progression = _progression });
        }

        public string RollOrContinue(out bool blocked)
        {
            blocked = false;
            if (_session == null) return "No session.";

            if (_session.State.Phase == GamePhase.Trading)
            {
                blocked = true;
                return "Finish trading window first (Resolve or Skip).";
            }

            if (_session.State.Phase == GamePhase.SeasonComplete)
            {
                AdvanceSeason();
                return "Season advanced.";
            }

            if (_session.State.Phase == GamePhase.CampaignComplete)
            {
                blocked = true;
                return "Campaign complete. Start New Game.";
            }

            var roll = _session.AdvanceRoll(out var outcome);
            _lastDiceRoll = roll;
            var msg = $"Rolled: {roll}";
            if (outcome.Messages.Count > 0)
                msg += "\n" + string.Join("\n", outcome.Messages);

            if (_session.State.Phase == GamePhase.Trading)
                msg += "\n--- Trading window: queue orders, then Resolve ---";

            if (_session.State.Phase == GamePhase.SeasonComplete)
                msg += "\n--- Season complete: tap Advance day / Continue ---";

            Persist();
            NotifyUi();
            return msg;
        }

        private void AdvanceSeason()
        {
            var winner = _session.GetLeaderboardIndex();
            var beatMarketCents = ComputeBeatMarketScoreCents(_session, 0);
            var humanName = _session.State.Players.Count > 0 ? _session.State.Players[0].DisplayName : "You";
            var seasonIndexJustCompleted = _session.State.SeasonIndex;
            ProgressionService.OnSeasonEnd(_progression, 0, winner, beatMarketCents, humanName);
            EnqueueCloudSubmission(humanName, seasonIndexJustCompleted, beatMarketCents, _session.State.RandomSeed);
            _session.State.SeasonIndex++;
            _session.State.SeasonSummaryLog.Add(
                $"Season {_session.State.SeasonIndex - 1} winner: {_session.State.Players[winner].DisplayName} • Beat market: ${(beatMarketCents / 100f):N0}");
            _session.ReseedMarketForNewSeason();

            if (_rules.campaignWinMode == CampaignWinMode.Seasons && _seasonsPerCampaign > 0 &&
                _session.State.SeasonIndex >= _seasonsPerCampaign)
                _session.State.Phase = GamePhase.CampaignComplete;
            else
                _session.State.Phase = GamePhase.Rolling;

            _humanOrders.Clear();
            Persist();
            NotifyUi();
            _ = TryFlushPendingScoresAsync();
        }

        public void BeginCloudSignIn()
        {
            _ = SignInAndSyncAsync();
        }

        public bool QueueHumanTrade(CommodityId c, int lot, bool buy)
        {
            if (_session == null || _session.State.Phase != GamePhase.Trading)
                return false;
            var delta = buy ? lot : -lot;
            _humanOrders.Add(new TradeOrder { PlayerIndex = 0, Commodity = c, ShareDelta = delta });
            NotifyUi();
            return true;
        }

        public string ResolveTrading()
        {
            if (_session == null || _session.State.Phase != GamePhase.Trading)
                return "Not in trading phase.";

            var all = new List<TradeOrder>();
            all.AddRange(_humanOrders);
            CollectAiOrders(all);
            var tradeLog = new List<string>();
            _session.ApplyTradingWindow(all, tradeLog);
            _humanOrders.Clear();
            Persist();
            NotifyUi();
            return string.Join("\n", tradeLog);
        }

        public void SkipTrading()
        {
            if (_session == null || _session.State.Phase != GamePhase.Trading) return;
            _session.SkipTradingWindow();
            _humanOrders.Clear();
            Persist();
            NotifyUi();
        }

        private void CollectAiOrders(List<TradeOrder> all)
        {
            for (var p = 1; p < _session.State.Players.Count; p++)
            {
                var strat = AgentStrategyFactory.ForPlayerIndex(p);
                _scratchOrders.Clear();
                strat.DecideTrades(_session, p, _scratchOrders);
                all.AddRange(_scratchOrders);
            }
        }

        private static void NormalizeLoadedState(GameStateSnapshot s)
        {
            if (s.SchemaVersion < 2)
            {
                s.SchemaVersion = 2;
                s.CampaignWinMode = CampaignWinMode.Seasons;
                s.PersistedNetWorthGoalCents = 10_000_000;
                s.PersistedTotalDiceRollsGoal = 12_000;
            }

            s.Players ??= new List<PlayerPortfolio>();
            s.RollLog ??= new List<string>();
            s.SeasonSummaryLog ??= new List<string>();
            s.Market.PricesCents ??= new int[6];
            for (var i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                p.SharesByCommodity ??= new int[6];
                s.Players[i] = p;
            }
        }

        private static void NormalizeLoadedProgression(ProgressionState p)
        {
            if (p.SchemaVersion < 2)
                p.SchemaVersion = 2;
            p.UnlockedCosmeticIds ??= new List<string>();
            p.HumanGlobalScoreboard ??= new List<HumanTraderScoreEntry>();
            p.PendingCloudScoreSubmissions ??= new List<PendingSeasonScoreSubmission>();
            if (!p.UnlockedCosmeticIds.Contains("theme_default"))
                p.UnlockedCosmeticIds.Add("theme_default");
            for (var i = 0; i < p.HumanGlobalScoreboard.Count; i++)
            {
                var entry = p.HumanGlobalScoreboard[i];
                if (string.IsNullOrWhiteSpace(entry.PlayerName))
                    entry.PlayerName = "You";
                if (entry.SeasonsPlayed < 0)
                    entry.SeasonsPlayed = 0;
                entry.LastUpdatedUtc ??= string.Empty;
                p.HumanGlobalScoreboard[i] = entry;
            }

            for (var i = 0; i < p.PendingCloudScoreSubmissions.Count; i++)
            {
                var pending = p.PendingCloudScoreSubmissions[i];
                pending.PlayerName = string.IsNullOrWhiteSpace(pending.PlayerName) ? "You" : pending.PlayerName;
                pending.SeasonKey ??= string.Empty;
                pending.LastAttemptUtc ??= string.Empty;
                if (pending.AttemptCount < 0) pending.AttemptCount = 0;
                p.PendingCloudScoreSubmissions[i] = pending;
            }
        }

        private static long ComputeBeatMarketScoreCents(GameSession session, int humanPlayerIndex)
        {
            if (session?.State?.Players == null || session.State.Players.Count == 0 || humanPlayerIndex < 0 ||
                humanPlayerIndex >= session.State.Players.Count)
                return 0;

            var prices = session.State.Market.PricesCents;
            var startingCash = session.Rules.startingCashCents;
            var humanProfit = (long)session.State.Players[humanPlayerIndex].NetWorthCents(prices) - startingCash;

            long aiProfitTotal = 0;
            var aiCount = 0;
            for (var i = 0; i < session.State.Players.Count; i++)
            {
                if (i == humanPlayerIndex) continue;
                aiProfitTotal += (long)session.State.Players[i].NetWorthCents(prices) - startingCash;
                aiCount++;
            }

            if (aiCount <= 0)
                return humanProfit;

            var aiAverageProfit = aiProfitTotal / aiCount;
            return humanProfit - aiAverageProfit;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                Persist();
        }

        private void OnApplicationQuit()
        {
            Persist();
        }

        private void OnDestroy()
        {
            if (_authService != null)
                _authService.StateChanged -= HandleAuthStateChanged;
        }

        private void NotifyUi()
        {
            UiUpdated?.Invoke();
        }

        private void StartCloudBootstrap()
        {
            if (_cloudBootstrapStarted || _authService == null)
                return;
            _cloudBootstrapStarted = true;
            _ = SignInAndSyncAsync();
        }

        private async Task SignInAndSyncAsync()
        {
            if (_authService == null)
            {
                _cloudStatus = "Cloud auth unavailable.";
                NotifyUi();
                return;
            }

            var result = await _authService.SignInAsync();
            if (!result.Success)
            {
                _cloudStatus = string.IsNullOrWhiteSpace(result.Error) ? "Sign-in failed." : result.Error;
                NotifyUi();
                return;
            }

            _cloudStatus = "Connected. Syncing leaderboard...";
            NotifyUi();
            await TryFlushPendingScoresAsync();
            await RefreshCloudLeaderboardAsync();
        }

        private void HandleAuthStateChanged(AuthState state)
        {
            if (state.IsAuthenticated)
                _cloudStatus = "Connected.";
            else if (!string.IsNullOrWhiteSpace(state.StatusMessage))
                _cloudStatus = state.StatusMessage;
            else
                _cloudStatus = "Signed out.";
            NotifyUi();
        }

        private void EnqueueCloudSubmission(string playerName, int seasonIndex, long beatMarketCents, int campaignSeed)
        {
            if (_progression == null)
                return;
            _progression.PendingCloudScoreSubmissions ??= new List<PendingSeasonScoreSubmission>();

            var seasonKey = $"{campaignSeed}:{seasonIndex}";
            for (var i = 0; i < _progression.PendingCloudScoreSubmissions.Count; i++)
            {
                if (!string.Equals(_progression.PendingCloudScoreSubmissions[i].SeasonKey, seasonKey, StringComparison.Ordinal))
                    continue;
                return;
            }

            _progression.PendingCloudScoreSubmissions.Add(new PendingSeasonScoreSubmission
            {
                PlayerName = string.IsNullOrWhiteSpace(playerName) ? "You" : playerName.Trim(),
                SeasonIndex = seasonIndex,
                BeatMarketCents = beatMarketCents,
                SeasonKey = seasonKey,
                AttemptCount = 0,
                LastAttemptUtc = string.Empty
            });
        }

        private async Task TryFlushPendingScoresAsync()
        {
            if (_cloudSyncInFlight || _progression == null || _scoreboardService == null || _authService == null ||
                !_authService.State.IsAuthenticated)
                return;
            if (_progression.PendingCloudScoreSubmissions == null || _progression.PendingCloudScoreSubmissions.Count == 0)
                return;

            _cloudSyncInFlight = true;
            try
            {
                var flush = await CloudSyncQueueProcessor.FlushAsync(_progression.PendingCloudScoreSubmissions, _scoreboardService);
                if (!string.IsNullOrWhiteSpace(flush.LastError))
                    _cloudStatus = flush.LastError;

                Persist();
            }
            finally
            {
                _cloudSyncInFlight = false;
            }
        }

        private async Task RefreshCloudLeaderboardAsync()
        {
            if (_progression == null || _scoreboardService == null || _authService == null || !_authService.State.IsAuthenticated)
                return;

            var fetch = await _scoreboardService.FetchTopAsync(10);
            if (!fetch.Success)
            {
                _cloudStatus = string.IsNullOrWhiteSpace(fetch.Error) ? "Could not refresh global leaderboard." : fetch.Error;
                NotifyUi();
                return;
            }

            _progression.HumanGlobalScoreboard ??= new List<HumanTraderScoreEntry>();
            _progression.HumanGlobalScoreboard.Clear();
            for (var i = 0; i < fetch.Entries.Count; i++)
            {
                var src = fetch.Entries[i];
                _progression.HumanGlobalScoreboard.Add(new HumanTraderScoreEntry
                {
                    PlayerName = string.IsNullOrWhiteSpace(src.DisplayName) ? $"Trader {i + 1}" : src.DisplayName,
                    SeasonsPlayed = src.SeasonsPlayed,
                    TotalBeatMarketCents = src.LifetimeBeatMarketCents,
                    BestBeatMarketCents = src.BestSeasonBeatMarketCents,
                    LastUpdatedUtc = DateTime.UtcNow.ToString("O")
                });
            }

            _cloudStatus = "Global leaderboard synced.";
            Persist();
            NotifyUi();
        }
    }
}
