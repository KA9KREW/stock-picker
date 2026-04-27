using System;
using System.Collections.Generic;
using System.Text;
using StockPicker.Game.Core;
using StockPicker.Game.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockPicker.App
{
    /// <summary>
    /// Structured mobile HUD with normal menu flow:
    /// Main menu overlay -> in-game HUD -> phase-specific action panels.
    /// </summary>
    public sealed class GameHudView : MonoBehaviour
    {
        private const string PrefsUiScale = "StockPicker_UiScale";

        private GameFlowController _flow;
        private Font _font;
        private GameSettingsOverlay _settings;
        private RectTransform _hudScaleRoot;

        private GameObject _mainMenuPanel;
        private GameObject _inGamePanel;
        private GameObject _rollingPanel;
        private GameObject _tradingPanel;
        private bool _menuOpen = true;

        private Text _phaseTitle;
        private Text _marketLine;
        private Text _humanSummary;
        private Text _eventText;
        private Text _tradeSummary;
        private Text _globalLeaderboardText;
        private Text _cloudStatusText;
        private Text _holdingsText;
        private Text _aiScoreboardText;
        private Text _rollButtonLabel;

        private Button _rollButton;
        private Button _buyButton;
        private Button _sellButton;
        private Button _prevCommodityButton;
        private Button _nextCommodityButton;
        private Button _lotDownButton;
        private Button _lotUpButton;
        private Button _queueButton;
        private Button _resolveButton;
        private Button _skipButton;
        private Button _cloudSignInButton;

        private int _commodityIndex;
        private int _lotIndex;
        private bool _humanBuyMode = true;
        private readonly List<string> _eventLines = new();

        private static readonly string[] CommodityLabels = { "Gold", "Silver", "Bonds", "Oil", "Industrials", "Grain" };

        private void Awake()
        {
            _flow = GetComponent<GameFlowController>();
            if (_flow == null)
            {
                Debug.LogError("GameHudView requires GameFlowController on the same GameObject.");
                return;
            }

            _font = LegacyUiFont.Get();
            BuildUi();
            _flow.UiUpdated += RefreshAll;
        }

        private void OnDestroy()
        {
            if (_flow != null)
                _flow.UiUpdated -= RefreshAll;
        }

        private void LateUpdate()
        {
            if (_menuOpen || _flow?.Session == null || _flow.Rules == null)
                return;
            var s = _flow.Session.State;
            var m = s.Market.PricesCents;
            if (m == null || m.Length < 6)
                return;
            RefreshHoldings(s, m);
            RefreshAiScoreboard(s, m);
        }

        private void BuildUi()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            var cam = Camera.main;
            canvas.renderMode = cam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            if (cam != null)
            {
                canvas.worldCamera = cam;
                canvas.planeDistance = 0.42f;
                canvas.sortingOrder = 100;
            }

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.55f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _hudScaleRoot = NewRect("HudScaleRoot", canvasGo.GetComponent<RectTransform>());
            StretchFull(_hudScaleRoot);
            ApplySafeArea(_hudScaleRoot);
            _hudScaleRoot.localScale = Vector3.one * Mathf.Clamp(PlayerPrefs.GetFloat(PrefsUiScale, 1f), 0.78f, 1.22f);

            _settings = GameSettingsOverlay.Create(canvasGo.GetComponent<RectTransform>(), _hudScaleRoot);

            _mainMenuPanel = BuildMainMenu(_hudScaleRoot);
            _inGamePanel = BuildInGameHud(_hudScaleRoot);
        }

        private GameObject BuildMainMenu(RectTransform root)
        {
            var panel = NewRect("MainMenuPanel", root).gameObject;
            StretchFull(panel.GetComponent<RectTransform>());
            panel.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.08f, 0.72f);

            var card = NewRect("Card", panel.GetComponent<RectTransform>());
            card.anchorMin = new Vector2(0.11f, 0.08f);
            card.anchorMax = new Vector2(0.89f, 0.86f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;
            card.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f, 1f);
            var cardOutline = card.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.68f, 0.72f, 0.8f, 1f);
            cardOutline.effectDistance = new Vector2(0f, 2f);

            var title = UiText(card, "Title", new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.97f), 48, TextAnchor.MiddleCenter,
                new Color(0.12f, 0.14f, 0.2f));
            title.text = "Stock Picker";
            title.fontStyle = FontStyle.Bold;

            var subtitle = UiText(card, "Subtitle", new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.78f), 22, TextAnchor.MiddleCenter,
                new Color(0.25f, 0.28f, 0.36f));
            subtitle.text = "Day • Trade • Pick a game length";

            void StartNewPreset(NewGamePreset preset)
            {
                _flow.NewGameWithPreset(preset);
                _eventLines.Clear();
                AppendEvent("New campaign started.");
                SetMenuOpen(false);
                RefreshAll();
            }

            MakePrimaryButton(card, "Continue", new Vector2(0.14f, 0.60f), new Vector2(0.86f, 0.68f), () => SetMenuOpen(false));
            MakeSecondaryButton(card, "First to $100k", new Vector2(0.14f, 0.50f), new Vector2(0.48f, 0.58f),
                () => StartNewPreset(NewGamePreset.FirstTo100K));
            MakeSecondaryButton(card, "12,000 rolls", new Vector2(0.52f, 0.50f), new Vector2(0.86f, 0.58f),
                () => StartNewPreset(NewGamePreset.TwelveThousandRolls));
            MakeSecondaryButton(card, "600 rolls (fast)", new Vector2(0.14f, 0.40f), new Vector2(0.48f, 0.48f),
                () => StartNewPreset(NewGamePreset.Fast600Rolls));
            MakeSecondaryButton(card, "Classic (seasons)", new Vector2(0.52f, 0.40f), new Vector2(0.86f, 0.48f),
                () => StartNewPreset(NewGamePreset.ClassicSeasons));
            MakeSecondaryButton(card, "Settings", new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.24f), () => _settings.Toggle());

            var globalStats = NewRect("GlobalStats", card);
            globalStats.anchorMin = new Vector2(0.12f, 0.02f);
            globalStats.anchorMax = new Vector2(0.88f, 0.15f);
            globalStats.offsetMin = Vector2.zero;
            globalStats.offsetMax = Vector2.zero;
            globalStats.gameObject.AddComponent<Image>().color = new Color(0.91f, 0.94f, 0.99f, 0.88f);
            var statsOutline = globalStats.gameObject.AddComponent<Outline>();
            statsOutline.effectColor = new Color(0.68f, 0.72f, 0.8f, 0.88f);
            statsOutline.effectDistance = new Vector2(0f, 1f);
            _globalLeaderboardText = UiText(globalStats, "GlobalStatsBody", new Vector2(0.02f, 0.32f), new Vector2(0.98f, 0.95f), 16,
                TextAnchor.UpperLeft, new Color(0.14f, 0.18f, 0.26f));
            _globalLeaderboardText.supportRichText = true;
            _globalLeaderboardText.resizeTextForBestFit = false;
            _globalLeaderboardText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _globalLeaderboardText.verticalOverflow = VerticalWrapMode.Truncate;
            _cloudStatusText = UiText(globalStats, "CloudStatus", new Vector2(0.02f, 0.02f), new Vector2(0.64f, 0.30f), 14,
                TextAnchor.MiddleLeft, new Color(0.18f, 0.25f, 0.35f));
            _cloudStatusText.resizeTextForBestFit = false;
            _cloudSignInButton = MakeSecondaryButton(globalStats, "Sign in", new Vector2(0.66f, 0.04f), new Vector2(0.98f, 0.30f), () =>
            {
                _flow.BeginCloudSignIn();
                RefreshAll();
            });

            return panel;
        }

        private GameObject BuildInGameHud(RectTransform root)
        {
            var panel = NewRect("InGamePanel", root).gameObject;
            StretchFull(panel.GetComponent<RectTransform>());

            var top = NewRect("TopHud", panel.GetComponent<RectTransform>());
            top.anchorMin = new Vector2(0.02f, 0.90f);
            top.anchorMax = new Vector2(0.98f, 0.985f);
            top.offsetMin = Vector2.zero;
            top.offsetMax = Vector2.zero;
            top.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.97f, 0.99f, 0.94f);
            top.gameObject.AddComponent<Outline>().effectColor = new Color(0.72f, 0.76f, 0.84f, 1f);

            _phaseTitle = UiText(top, "PhaseTitle", new Vector2(0.02f, 0.62f), new Vector2(0.37f, 0.98f), 28, TextAnchor.MiddleLeft,
                new Color(0.12f, 0.14f, 0.2f));
            _phaseTitle.fontStyle = FontStyle.Bold;
            _marketLine = UiText(top, "Market", new Vector2(0.02f, 0.30f), new Vector2(0.98f, 0.6f), 20, TextAnchor.MiddleLeft,
                new Color(0.2f, 0.24f, 0.3f));
            _humanSummary = UiText(top, "Human", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.30f), 20, TextAnchor.MiddleLeft,
                new Color(0.2f, 0.24f, 0.32f));

            MakeSecondaryButton(top, "Menu", new Vector2(0.87f, 0.70f), new Vector2(0.98f, 0.98f), () => SetMenuOpen(true));
            MakeSecondaryButton(top, "⚙", new Vector2(0.87f, 0.38f), new Vector2(0.98f, 0.66f), () => _settings.Toggle());

            var holdingsRt = NewRect("HoldingsPanel", top);
            holdingsRt.anchorMin = new Vector2(0.40f, 0.58f);
            holdingsRt.anchorMax = new Vector2(0.84f, 0.97f);
            holdingsRt.offsetMin = new Vector2(4f, 3f);
            holdingsRt.offsetMax = new Vector2(-4f, -3f);
            holdingsRt.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.95f, 0.99f, 0.78f);
            var holdingsOutline = holdingsRt.gameObject.AddComponent<Outline>();
            holdingsOutline.effectColor = new Color(0.68f, 0.72f, 0.8f, 0.85f);
            holdingsOutline.effectDistance = new Vector2(0f, 1f);
            const float holdingsScale = 1.8f;
            var holdingsFont = Mathf.RoundToInt(17 * holdingsScale);
            _holdingsText = UiText(holdingsRt, "HoldingsBody", Vector2.zero, Vector2.one, holdingsFont, TextAnchor.UpperRight,
                new Color(0.18f, 0.22f, 0.3f));
            _holdingsText.fontStyle = FontStyle.Normal;
            _holdingsText.lineSpacing = 0.88f;
            _holdingsText.verticalOverflow = VerticalWrapMode.Truncate;
            _holdingsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _holdingsText.resizeTextForBestFit = true;
            _holdingsText.resizeTextMinSize = Mathf.RoundToInt(11 * holdingsScale);
            _holdingsText.resizeTextMaxSize = Mathf.RoundToInt(18 * holdingsScale);
            var holdingsTxtRt = _holdingsText.rectTransform;
            holdingsTxtRt.offsetMin = new Vector2(4f, 3f);
            holdingsTxtRt.offsetMax = new Vector2(-4f, -3f);

            var panelRt = panel.GetComponent<RectTransform>();
            var aiBoardRt = NewRect("AiScoreboardPanel", panelRt);
            aiBoardRt.anchorMin = new Vector2(0.805f, 0.265f);
            aiBoardRt.anchorMax = new Vector2(0.988f, 0.825f);
            aiBoardRt.offsetMin = new Vector2(2f, 4f);
            aiBoardRt.offsetMax = new Vector2(-6f, -4f);
            var aiBg = aiBoardRt.gameObject.AddComponent<Image>();
            aiBg.color = new Color(1f, 1f, 1f, 0f);
            aiBg.raycastTarget = false;
            _aiScoreboardText = UiText(aiBoardRt, "AiScoreboardBody", new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f), 15,
                TextAnchor.UpperLeft, new Color(0.12f, 0.15f, 0.22f));
            _aiScoreboardText.supportRichText = true;
            _aiScoreboardText.lineSpacing = 1.05f;
            _aiScoreboardText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _aiScoreboardText.verticalOverflow = VerticalWrapMode.Overflow;
            _aiScoreboardText.resizeTextForBestFit = false;
            var aiSh = _aiScoreboardText.gameObject.AddComponent<Shadow>();
            aiSh.effectColor = new Color(1f, 1f, 1f, 0.55f);
            aiSh.effectDistance = new Vector2(1f, -1f);
            var aiTxtRt = _aiScoreboardText.rectTransform;
            aiTxtRt.offsetMin = new Vector2(2f, 2f);
            aiTxtRt.offsetMax = new Vector2(-2f, -2f);

            var eventBar = NewRect("EventBar", panel.GetComponent<RectTransform>());
            eventBar.anchorMin = new Vector2(0.02f, 0.845f);
            eventBar.anchorMax = new Vector2(0.98f, 0.895f);
            eventBar.offsetMin = Vector2.zero;
            eventBar.offsetMax = Vector2.zero;
            eventBar.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.88f);
            _eventText = UiText(eventBar, "Events", new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), 17, TextAnchor.MiddleLeft,
                new Color(0.92f, 0.95f, 1f));
            _eventText.fontStyle = FontStyle.Normal;

            _rollingPanel = NewRect("RollingPanel", panel.GetComponent<RectTransform>()).gameObject;
            var rollingRt = _rollingPanel.GetComponent<RectTransform>();
            rollingRt.anchorMin = new Vector2(0.18f, 0.065f);
            rollingRt.anchorMax = new Vector2(0.82f, 0.125f);
            rollingRt.offsetMin = Vector2.zero;
            rollingRt.offsetMax = Vector2.zero;
            _rollButton = MakePrimaryButton(rollingRt, "Advance day", new Vector2(0f, 0f), new Vector2(1f, 1f), OnRoll);
            _rollButtonLabel = _rollButton.GetComponentInChildren<Text>();

            _tradingPanel = NewRect("TradingPanel", panel.GetComponent<RectTransform>()).gameObject;
            var tradeRt = _tradingPanel.GetComponent<RectTransform>();
            tradeRt.anchorMin = new Vector2(0.02f, 0.055f);
            tradeRt.anchorMax = new Vector2(0.98f, 0.255f);
            tradeRt.offsetMin = Vector2.zero;
            tradeRt.offsetMax = Vector2.zero;
            tradeRt.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.96f, 0.98f, 0.98f);
            tradeRt.gameObject.AddComponent<Outline>().effectColor = new Color(0.7f, 0.74f, 0.82f, 1f);

            _buyButton = MakeChipButton(tradeRt, "Buy", new Vector2(0.02f, 0.72f), new Vector2(0.49f, 0.97f), () => SetBuyMode(true));
            _sellButton = MakeChipButton(tradeRt, "Sell", new Vector2(0.51f, 0.72f), new Vector2(0.98f, 0.97f), () => SetBuyMode(false));

            _tradeSummary = UiText(tradeRt, "TradeSummary", new Vector2(0.02f, 0.56f), new Vector2(0.98f, 0.72f), 19, TextAnchor.MiddleCenter,
                new Color(0.15f, 0.19f, 0.25f));
            _tradeSummary.fontStyle = FontStyle.Bold;

            _prevCommodityButton = MakeSecondaryButton(tradeRt, "◀ Stock", new Vector2(0.02f, 0.34f), new Vector2(0.27f, 0.54f), CycleCommodityBack);
            _nextCommodityButton = MakeSecondaryButton(tradeRt, "Stock ▶", new Vector2(0.29f, 0.34f), new Vector2(0.54f, 0.54f), CycleCommodity);
            _lotDownButton = MakeSecondaryButton(tradeRt, "− Lot", new Vector2(0.56f, 0.34f), new Vector2(0.76f, 0.54f), CycleLotBack);
            _lotUpButton = MakeSecondaryButton(tradeRt, "+ Lot", new Vector2(0.78f, 0.34f), new Vector2(0.98f, 0.54f), CycleLot);

            _queueButton = MakePrimaryButton(tradeRt, "Queue trade", new Vector2(0.02f, 0.17f), new Vector2(0.98f, 0.30f), OnQueue);
            _resolveButton = MakePrimaryButton(tradeRt, "Finish trades", new Vector2(0.02f, 0.02f), new Vector2(0.62f, 0.15f), OnResolve);
            _skipButton = MakeSecondaryButton(tradeRt, "Skip trading", new Vector2(0.64f, 0.02f), new Vector2(0.98f, 0.15f), OnSkipTrading);

            return panel;
        }

        private void SetMenuOpen(bool open)
        {
            _menuOpen = open;
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(open);
            if (_inGamePanel != null) _inGamePanel.SetActive(!open);
        }

        private void SetBuyMode(bool buy)
        {
            _humanBuyMode = buy;
            RefreshTradeLine();
            RefreshBuySellChrome();
        }

        private void RefreshBuySellChrome()
        {
            SetChipActive(_buyButton, _humanBuyMode);
            SetChipActive(_sellButton, !_humanBuyMode);
        }

        private static void SetChipActive(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = active ? new Color(0.13f, 0.52f, 0.4f) : new Color(0.39f, 0.45f, 0.56f);
        }

        private void OnRoll()
        {
            GameSettingsOverlay.TryVibrate();
            var msg = _flow.RollOrContinue(out var blocked);
            if (!blocked && !string.IsNullOrEmpty(msg))
                AppendEvent(msg);
            RefreshAll();
        }

        private void OnQueue()
        {
            if (_flow.Rules == null || _flow.Rules.allowedShareLots.Length == 0) return;
            var lot = _flow.Rules.allowedShareLots[_lotIndex];
            var c = (CommodityId)Mathf.Clamp(_commodityIndex, 0, 5);
            if (_flow.QueueHumanTrade(c, lot, _humanBuyMode))
                AppendEvent($"{(_humanBuyMode ? "Buy" : "Sell")} {lot} shares of {CommodityLabels[_commodityIndex]} queued.");
            else
                AppendEvent("Trading is only available during trade windows.");
            RefreshAll();
        }

        private void OnResolve()
        {
            var lines = _flow.ResolveTrading();
            if (!string.IsNullOrEmpty(lines))
            {
                foreach (var line in lines.Split('\n'))
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendEvent(line);
            }

            RefreshAll();
        }

        private void OnSkipTrading()
        {
            _flow.SkipTrading();
            AppendEvent("Trading skipped.");
            RefreshAll();
        }

        private void CycleCommodity() { _commodityIndex = (_commodityIndex + 1) % 6; RefreshTradeLine(); }
        private void CycleCommodityBack() { _commodityIndex = (_commodityIndex + 5) % 6; RefreshTradeLine(); }
        private void CycleLot() { if (_flow?.Rules?.allowedShareLots?.Length > 0) { _lotIndex = (_lotIndex + 1) % _flow.Rules.allowedShareLots.Length; RefreshTradeLine(); } }
        private void CycleLotBack() { if (_flow?.Rules?.allowedShareLots?.Length > 0) { _lotIndex = (_lotIndex + _flow.Rules.allowedShareLots.Length - 1) % _flow.Rules.allowedShareLots.Length; RefreshTradeLine(); } }

        private void RefreshTradeLine()
        {
            if (_flow?.Rules == null || _tradeSummary == null) return;
            var lot = _flow.Rules.allowedShareLots.Length > 0 ? _flow.Rules.allowedShareLots[_lotIndex] : 0;
            var stock = CommodityLabels[Mathf.Clamp(_commodityIndex, 0, 5)];
            _tradeSummary.text = $"{stock} • {lot} shares • {(_humanBuyMode ? "BUY" : "SELL")}";
        }

        private void RefreshAll()
        {
            if (_flow?.Session == null || _flow.Progression == null || _flow.Rules == null)
            {
                if (_phaseTitle != null)
                    _phaseTitle.text = "Loading...";
                return;
            }

            var s = _flow.Session.State;
            var rules = _flow.Rules;
            var phase = s.Phase;
            var m = s.Market.PricesCents;

            var rollingExtra = "";
            var rollCountSegment = "";
            if (phase == GamePhase.Rolling)
            {
                switch (rules.campaignWinMode)
                {
                    case CampaignWinMode.FirstToNetWorth:
                        rollCountSegment = "N/A";
                        if (s.Players.Count > 0)
                        {
                            var nw = s.Players[0].NetWorthCents(m);
                            rollingExtra = $" • net ${nw / 100f:N0} / ${rules.netWorthGoalCents / 100f:N0}";
                        }

                        break;
                    case CampaignWinMode.TotalDiceRolls:
                        rollCountSegment = $"{s.TotalDiceRollsCampaign}/{rules.totalDiceRollsGoal}";
                        break;
                    default:
                        rollCountSegment = $"{s.RollIndexInSeason}/{rules.rollsPerSeason}";
                        break;
                }
            }

            _phaseTitle.text = phase switch
            {
                GamePhase.Rolling => rules.campaignWinMode == CampaignWinMode.Seasons
                    ? $"Rolling • Day {rollCountSegment}{rollingExtra}"
                    : $"Rolling • Roll {rollCountSegment}{rollingExtra}",
                GamePhase.Trading => "Trading Window",
                GamePhase.SeasonComplete => "Season Complete",
                GamePhase.CampaignComplete => "Campaign complete",
                _ => phase.ToString()
            };
            // Top → bottom: GOLD SILVER OIL BONDS INDUSTRIALS GRAIN
            _marketLine.text =
                $"Prices (c): G {m[0]}  S {m[1]}  O {m[3]}  B {m[2]}  I {m[4]}  Gr {m[5]}";
            if (s.Players.Count > 0)
            {
                var human = s.Players[0];
                _humanSummary.text = $"You: cash ${human.CashCents / 100f:N0} • net ${human.NetWorthCents(m) / 100f:N0}";
            }

            RefreshHoldings(s, m ?? Array.Empty<int>());
            RefreshAiScoreboard(s, m ?? Array.Empty<int>());
            RefreshGlobalLeaderboard(_flow.Progression);

            var trading = phase == GamePhase.Trading;
            _tradingPanel.SetActive(trading);
            _rollingPanel.SetActive(!trading && phase != GamePhase.CampaignComplete);
            if (_rollButtonLabel != null)
                _rollButtonLabel.text = phase == GamePhase.SeasonComplete
                    ? "Continue"
                    : (rules.campaignWinMode == CampaignWinMode.Seasons ? "Advance day" : "Roll dice");
            _rollButton.interactable = phase is GamePhase.Rolling or GamePhase.SeasonComplete;

            _prevCommodityButton.interactable = trading;
            _nextCommodityButton.interactable = trading;
            _lotDownButton.interactable = trading;
            _lotUpButton.interactable = trading;
            _queueButton.interactable = trading;
            _buyButton.interactable = trading;
            _sellButton.interactable = trading;
            _resolveButton.interactable = trading;
            _skipButton.interactable = trading;
            RefreshBuySellChrome();
            RefreshTradeLine();

            SetMenuOpen(_menuOpen);
        }

        private void RefreshGlobalLeaderboard(ProgressionState progression)
        {
            if (_globalLeaderboardText == null || progression == null)
                return;

            var sb = new StringBuilder(192);
            sb.Append("<b>Global Human Leaderboard (Beat the Market)</b>\n");
            sb.Append("Lifetime: <b>$").Append((progression.HumanLifetimeBeatMarketCents / 100f).ToString("N0"))
                .Append("</b> • Best season: <b>$").Append((progression.HumanBestBeatMarketCents / 100f).ToString("N0"))
                .Append("</b> • Seasons +: ").Append(progression.HumanBeatMarketSeasons).Append('\n');

            var board = progression.HumanGlobalScoreboard;
            if (board == null || board.Count == 0)
            {
                sb.Append("No ranked traders yet. Complete a season to post your first score.");
                _globalLeaderboardText.text = sb.ToString();
                return;
            }

            var count = Mathf.Min(3, board.Count);
            for (var i = 0; i < count; i++)
            {
                var row = board[i];
                sb.Append(i + 1).Append(". ")
                    .Append(string.IsNullOrWhiteSpace(row.PlayerName) ? "You" : row.PlayerName)
                    .Append(" — <b>$").Append((row.TotalBeatMarketCents / 100f).ToString("N0")).Append("</b>")
                    .Append(" (best $").Append((row.BestBeatMarketCents / 100f).ToString("N0")).Append(")");
                if (i < count - 1)
                    sb.Append('\n');
            }

            _globalLeaderboardText.text = sb.ToString();
            if (_cloudStatusText != null)
                _cloudStatusText.text = _flow.CloudStatus;
            if (_cloudSignInButton != null)
                _cloudSignInButton.interactable = _flow.CanBeginCloudSignIn;
        }

        private void RefreshHoldings(GameStateSnapshot state, int[] pricesCents)
        {
            if (_holdingsText == null || state.Players == null || state.Players.Count == 0)
            {
                if (_holdingsText != null)
                    _holdingsText.text = "—\n—";
                return;
            }

            if (pricesCents == null || pricesCents.Length < 6)
            {
                _holdingsText.text = "—\n—";
                return;
            }

            var sh = state.Players[0].SharesByCommodity;
            if (sh == null || sh.Length < 6)
            {
                _holdingsText.text = "—\n—";
                return;
            }

            // Same commodity order as prices line: G S O B I Gr — two tight lines, no extra labels
            var stockValue = (long)sh[0] * pricesCents[0] + (long)sh[1] * pricesCents[1] + (long)sh[2] * pricesCents[2] +
                (long)sh[3] * pricesCents[3] + (long)sh[4] * pricesCents[4] + (long)sh[5] * pricesCents[5];
            var holdingsLine = $"G{sh[0]} S{sh[1]} O{sh[3]} B{sh[2]} I{sh[4]} Gr{sh[5]}\n${stockValue / 100f:N0}";
            if (_holdingsText.text != holdingsLine)
                _holdingsText.text = holdingsLine;
        }

        private void RefreshAiScoreboard(GameStateSnapshot state, int[] pricesCents)
        {
            if (_aiScoreboardText == null || state.Players == null || state.Players.Count < 2)
            {
                if (_aiScoreboardText != null)
                    _aiScoreboardText.text = "<b>AI — net worth</b>\n—";
                return;
            }

            if (pricesCents == null || pricesCents.Length < 6)
            {
                _aiScoreboardText.text = "<b>AI — net worth</b>\n—";
                return;
            }

            var entries = new List<(int playerIndex, int netCents)>();
            for (var i = 1; i < state.Players.Count; i++)
                entries.Add((i, state.Players[i].NetWorthCents(pricesCents)));
            entries.Sort((a, b) => b.netCents.CompareTo(a.netCents));

            var sb = new StringBuilder(128);
            sb.Append("<b>AI — net worth</b>\n");
            for (var r = 0; r < entries.Count; r++)
            {
                var p = state.Players[entries[r].playerIndex];
                var name = string.IsNullOrEmpty(p.DisplayName) ? $"Bot {entries[r].playerIndex}" : p.DisplayName;
                if (name.Length > 12)
                    name = name.Substring(0, 11) + "…";
                sb.Append(r + 1).Append(". ").Append(name).Append("  <b>$").Append((entries[r].netCents / 100f).ToString("N0"))
                    .Append("</b>\n");
            }

            var boardText = sb.ToString().TrimEnd();
            if (_aiScoreboardText.text != boardText)
                _aiScoreboardText.text = boardText;
        }

        private void AppendEvent(string line)
        {
            const int maxLines = 3;
            _eventLines.Add(line);
            while (_eventLines.Count > maxLines)
                _eventLines.RemoveAt(0);
            if (_eventText != null)
                _eventText.text = string.Join("   |   ", _eventLines);
        }

        private RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void ApplySafeArea(RectTransform root)
        {
            var safe = Screen.safeArea;
            root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        private Text UiText(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, int size, TextAnchor align, Color color)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = new Vector2(6f, 4f);
            rt.offsetMax = new Vector2(-6f, -4f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 12;
            t.resizeTextMaxSize = size + 6;
            return t;
        }

        private Button MakePrimaryButton(RectTransform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick) =>
            MakeButton(parent, label, aMin, aMax, onClick, new Color(0.14f, 0.55f, 0.43f), new Color(0.2f, 0.64f, 0.5f), 24);

        private Button MakeSecondaryButton(RectTransform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick) =>
            MakeButton(parent, label, aMin, aMax, onClick, new Color(0.32f, 0.38f, 0.5f), new Color(0.4f, 0.48f, 0.62f), 21);

        private Button MakeChipButton(RectTransform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick) =>
            MakeButton(parent, label, aMin, aMax, onClick, new Color(0.37f, 0.45f, 0.58f), new Color(0.44f, 0.54f, 0.68f), 22);

        private Button MakeButton(RectTransform parent, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick,
            Color normal, Color highlighted, int fontSize)
        {
            var rt = NewRect(label + "Btn", parent);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = normal;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = highlighted;
            colors.pressedColor = normal * 0.86f;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.48f, 0.55f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var t = UiText(rt, "Label", Vector2.zero, Vector2.one, fontSize, TextAnchor.MiddleCenter, Color.white);
            t.text = label;
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return btn;
        }
    }
}
