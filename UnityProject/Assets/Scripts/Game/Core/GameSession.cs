using System;
using System.Collections.Generic;

namespace StockPicker.Game.Core
{
    /// <summary>
    /// Orchestrates rolls, trading windows, and season boundaries.
    /// </summary>
    public sealed class GameSession
    {
        public GameRules Rules { get; }
        public GameStateSnapshot State { get; }
        private SeededRng _rng;

        public GameSession(GameRules rules, GameStateSnapshot state)
        {
            Rules = rules;
            State = state;
            _rng = new SeededRng(state.RandomSeed);
            if (state.RngState != 0)
                _rng.State = (ulong)state.RngState;
            else
                State.RngState = (long)_rng.State;
        }

        public void PersistRng()
        {
            State.RngState = (long)_rng.State;
        }

        /// <summary>Start a fresh campaign season 0.</summary>
        public static GameSession NewGame(GameRules rules, int seed, string humanName)
        {
            var state = new GameStateSnapshot
            {
                SchemaVersion = 2,
                RandomSeed = seed,
                RngState = 0,
                RollIndexInSeason = 0,
                SeasonIndex = 0,
                Phase = GamePhase.Rolling,
                Market = MarketState.NewStarting(rules),
                Players = new List<PlayerPortfolio>(),
                CampaignWinMode = rules.campaignWinMode,
                PersistedNetWorthGoalCents = rules.netWorthGoalCents,
                PersistedTotalDiceRollsGoal = rules.totalDiceRollsGoal,
                TotalDiceRollsCampaign = 0
            };
            state.Players.Add(PlayerPortfolio.CreateNew(humanName, true, rules.startingCashCents));
            state.Players.Add(PlayerPortfolio.CreateNew("Avery (Safe)", false, rules.startingCashCents));
            state.Players.Add(PlayerPortfolio.CreateNew("Blake (Momentum)", false, rules.startingCashCents));
            state.Players.Add(PlayerPortfolio.CreateNew("Casey (Contrarian)", false, rules.startingCashCents));
            state.Players.Add(PlayerPortfolio.CreateNew("Drew (Volatility)", false, rules.startingCashCents));
            state.Players.Add(PlayerPortfolio.CreateNew("Ellis (Balanced)", false, rules.startingCashCents));
            state.Players.Add(PlayerPortfolio.CreateNew("Finley (Chaos+)", false, rules.startingCashCents));
            var s = new GameSession(rules, state);
            s.PersistRng();
            return s;
        }

        /// <summary>Reset portfolios and market for next season (progression meta unchanged).</summary>
        public void ReseedMarketForNewSeason()
        {
            State.Market = MarketState.NewStarting(Rules);
            State.RollIndexInSeason = 0;
            State.Phase = GamePhase.Rolling;
            for (var i = 0; i < State.Players.Count; i++)
            {
                var pl = State.Players[i];
                pl.CashCents = Rules.startingCashCents;
                Array.Clear(pl.SharesByCommodity, 0, pl.SharesByCommodity.Length);
                State.Players[i] = pl;
            }

            PersistRng();
        }

        public bool IsTradingWindowDue()
        {
            if (State.Phase != GamePhase.Rolling) return false;
            if (State.RollIndexInSeason <= 0) return false;
            return State.RollIndexInSeason % Rules.rollsPerTradingWindow == 0;
        }

        public bool IsSeasonCompleteAfterRoll()
        {
            return State.RollIndexInSeason >= Rules.rollsPerSeason;
        }

        public DiceRoll AdvanceRoll(out MarketResolver.RollOutcome outcome)
        {
            outcome = new MarketResolver.RollOutcome();
            if (State.Phase != GamePhase.Rolling)
                return default;

            var roll = DiceService.Roll(ref _rng);
            State.RngState = (long)_rng.State;
            MarketResolver.ApplyRoll(Rules, roll, State.Market, State.Players, outcome);
            foreach (var m in outcome.Messages)
                State.RollLog.Add(m);
            State.RollIndexInSeason++;
            State.TotalDiceRollsCampaign++;

            if (Rules.campaignWinMode == CampaignWinMode.TotalDiceRolls && Rules.totalDiceRollsGoal > 0
                && State.TotalDiceRollsCampaign >= Rules.totalDiceRollsGoal)
            {
                State.Phase = GamePhase.CampaignComplete;
                State.RollLog.Add($"Campaign over — reached {Rules.totalDiceRollsGoal} rolls.");
                PersistRng();
                return roll;
            }

            if (Rules.campaignWinMode == CampaignWinMode.FirstToNetWorth && TryFirstToNetWorthWinner(out var winIdx))
            {
                State.Phase = GamePhase.CampaignComplete;
                AppendNetWorthWinLog(winIdx);
                PersistRng();
                return roll;
            }

            if (Rules.campaignWinMode == CampaignWinMode.Seasons && IsSeasonCompleteAfterRoll())
                State.Phase = GamePhase.SeasonComplete;
            else if (IsTradingWindowDue())
                State.Phase = GamePhase.Trading;

            PersistRng();
            return roll;
        }

        public void EnterTradingPhase()
        {
            State.Phase = GamePhase.Trading;
        }

        public void ApplyTradingWindow(IList<TradeOrder> orders, IList<string> tradeLog)
        {
            if (State.Phase != GamePhase.Trading)
                return;
            TradeResolver.ApplyOrders(Rules, State.Market, State.Players, orders, tradeLog);
            foreach (var line in tradeLog)
                State.RollLog.Add("[Trade] " + line);
            State.Phase = GamePhase.Rolling;

            if (Rules.campaignWinMode == CampaignWinMode.FirstToNetWorth && TryFirstToNetWorthWinner(out var winIdx))
            {
                State.Phase = GamePhase.CampaignComplete;
                AppendNetWorthWinLog(winIdx);
            }

            PersistRng();
        }

        public void SkipTradingWindow()
        {
            if (State.Phase == GamePhase.Trading)
                State.Phase = GamePhase.Rolling;
            PersistRng();
        }

        public int GetLeaderboardIndex()
        {
            var best = int.MinValue;
            var bestIdx = 0;
            for (var i = 0; i < State.Players.Count; i++)
            {
                var nw = State.Players[i].NetWorthCents(State.Market.PricesCents);
                if (nw > best)
                {
                    best = nw;
                    bestIdx = i;
                }
            }

            return bestIdx;
        }

        private bool TryFirstToNetWorthWinner(out int playerIndex)
        {
            playerIndex = 0;
            var prices = State.Market.PricesCents;
            var goal = Rules.netWorthGoalCents;
            if (goal <= 0) return false;
            for (var i = 0; i < State.Players.Count; i++)
            {
                if (State.Players[i].NetWorthCents(prices) < goal) continue;
                playerIndex = i;
                return true;
            }

            return false;
        }

        private void AppendNetWorthWinLog(int playerIndex)
        {
            var p = State.Players[playerIndex];
            var nw = p.NetWorthCents(State.Market.PricesCents);
            State.RollLog.Add(
                $"Campaign over — {p.DisplayName} hit ${nw / 100f:N0} net worth (goal ${Rules.netWorthGoalCents / 100f:N0}).");
        }
    }
}
