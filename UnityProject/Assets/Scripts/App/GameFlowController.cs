using System;
using System.Collections.Generic;
using StockPicker.Game.AI;
using StockPicker.Game.Core;
using StockPicker.Game.Progression;
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

        public event Action UiUpdated;

        public GameRules Rules => _rules;
        public GameSession Session => _session;
        public ProgressionState Progression => _progression;
        public IReadOnlyList<TradeOrder> HumanOrders => _humanOrders;

        /// <summary>Last market dice outcome from <see cref="RollOrContinue"/>; null until the first roll of the session.</summary>
        public DiceRoll? LastDiceRoll => _lastDiceRoll;

        private DiceRoll? _lastDiceRoll;

        public void Initialize(GameRules rulesAsset, int seasonsPerCampaign)
        {
            _seasonsPerCampaign = seasonsPerCampaign;
            _rulesTemplate = rulesAsset;
            _rules = rulesAsset != null ? rulesAsset : GameRules.CreateDefaultRuntime();
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
            NotifyUi();
        }

        public void LoadPressed()
        {
            var data = _save.LoadOrDefault(_rulesTemplate != null ? _rulesTemplate : _rules);
            NormalizeLoadedState(data.Game);
            _rules = GameRules.FromPersistedState(data.Game, _rulesTemplate);
            _session = new GameSession(_rules, data.Game);
            _progression = data.Progression;
            _humanOrders.Clear();
            _lastDiceRoll = null;
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
                msg += "\n--- Season complete: tap Roll / Continue ---";

            Persist();
            NotifyUi();
            return msg;
        }

        private void AdvanceSeason()
        {
            var winner = _session.GetLeaderboardIndex();
            ProgressionService.OnSeasonEnd(_progression, 0, winner);
            _session.State.SeasonIndex++;
            _session.State.SeasonSummaryLog.Add(
                $"Season {_session.State.SeasonIndex - 1} winner: {_session.State.Players[winner].DisplayName}");
            _session.ReseedMarketForNewSeason();

            if (_rules.campaignWinMode == CampaignWinMode.Seasons && _seasonsPerCampaign > 0 &&
                _session.State.SeasonIndex >= _seasonsPerCampaign)
                _session.State.Phase = GamePhase.CampaignComplete;
            else
                _session.State.Phase = GamePhase.Rolling;

            _humanOrders.Clear();
            Persist();
            NotifyUi();
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

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                Persist();
        }

        private void OnApplicationQuit()
        {
            Persist();
        }

        private void NotifyUi()
        {
            UiUpdated?.Invoke();
        }
    }
}
