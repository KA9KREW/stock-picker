using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StockPicker.App
{
    /// <summary>
    /// Settings sheet using <see cref="Text"/> + <see cref="LegacyUiFont"/>.</summary>
    public sealed class GameSettingsOverlay : MonoBehaviour
    {
        private const string PrefsUiScale = "StockPicker_UiScale";
        private const string PrefsMasterVol = "StockPicker_MasterVol";
        private const string PrefsVibrate = "StockPicker_Vibrate";
        private const string PrefsQuality = "StockPicker_Quality";

        private CanvasGroup _rootGroup;
        private RectTransform _hudScaleRoot;
        private Font _font;
        private Text _uiScaleLabel;
        private Text _volLabel;
        private Text _qualityLabel;
        private Text _vibrateLabel;

        private float _uiScale = 1f;
        private float _volume = 1f;
        private int _qualityIndex;
        private bool _vibrate = true;

        public static GameSettingsOverlay Create(RectTransform canvasRoot, RectTransform hudScaleRoot)
        {
            var go = new GameObject("SettingsOverlay", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRoot, false);
            StretchFull(rt);
            var comp = go.AddComponent<GameSettingsOverlay>();
            comp._hudScaleRoot = hudScaleRoot;
            comp._font = LegacyUiFont.Get();
            comp.LoadPrefs();
            comp.Build(rt);
            comp.HideImmediate();
            return comp;
        }

        public void Toggle()
        {
            if (_rootGroup == null) return;
            var on = _rootGroup.alpha < 0.5f;
            _rootGroup.alpha = on ? 1f : 0f;
            _rootGroup.interactable = on;
            _rootGroup.blocksRaycasts = on;
        }

        private void HideImmediate()
        {
            _rootGroup.alpha = 0f;
            _rootGroup.interactable = false;
            _rootGroup.blocksRaycasts = false;
        }

        private void LoadPrefs()
        {
            _uiScale = PlayerPrefs.GetFloat(PrefsUiScale, 1f);
            _volume = PlayerPrefs.GetFloat(PrefsMasterVol, 1f);
            _vibrate = PlayerPrefs.GetInt(PrefsVibrate, 1) == 1;
            var names = QualitySettings.names;
            _qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefsQuality, QualitySettings.GetQualityLevel()), 0,
                Mathf.Max(0, names.Length - 1));
            QualitySettings.SetQualityLevel(_qualityIndex);
        }

        private void Build(RectTransform root)
        {
            _rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            var dim = new GameObject("Dim", typeof(RectTransform));
            var dimRt = dim.GetComponent<RectTransform>();
            dimRt.SetParent(root, false);
            StretchFull(dimRt);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0.05f, 0.07f, 0.1f, 0.55f);
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.onClick.AddListener(Toggle);

            var panel = new GameObject("Panel", typeof(RectTransform));
            var pr = panel.GetComponent<RectTransform>();
            pr.SetParent(root, false);
            pr.anchorMin = new Vector2(0.08f, 0.2f);
            pr.anchorMax = new Vector2(0.92f, 0.8f);
            pr.offsetMin = Vector2.zero;
            pr.offsetMax = Vector2.zero;
            var panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.96f, 0.97f, 0.99f, 1f);

            AddTitle(pr);
            var y = 0.76f;
            _uiScaleLabel = AddStepperRow(pr, "UI scale", y, () => AdjustUiScale(-0.06f), () => AdjustUiScale(0.06f));
            y -= 0.14f;
            _volLabel = AddStepperRow(pr, "Master volume", y, () => AdjustVolume(-0.1f), () => AdjustVolume(0.1f));
            y -= 0.14f;
            _qualityLabel = AddSingleButtonRow(pr, "Graphics", y, CycleQuality);
            y -= 0.14f;
            _vibrateLabel = AddSingleButtonRow(pr, "Vibration", y, ToggleVibrate);
            y -= 0.14f;
            AddQuitAppButton(pr, y);
            AddCloseButton(pr);

            RefreshLabels();
            ApplyUiScale();
            AudioListener.volume = Mathf.Clamp01(_volume);
        }

        private void RefreshLabels()
        {
            if (_uiScaleLabel != null)
                _uiScaleLabel.text = $"UI scale: {Mathf.RoundToInt(_uiScale * 100f)}%";
            if (_volLabel != null)
                _volLabel.text = $"Master volume: {Mathf.RoundToInt(_volume * 100f)}%";
            if (_qualityLabel != null)
            {
                var names = QualitySettings.names;
                var n = names[Mathf.Clamp(_qualityIndex, 0, names.Length - 1)];
                _qualityLabel.text = $"Graphics: {n} (tap to cycle)";
            }

            if (_vibrateLabel != null)
                _vibrateLabel.text = _vibrate ? "Vibration: On (tap)" : "Vibration: Off (tap)";
        }

        private void AdjustUiScale(float delta)
        {
            _uiScale = Mathf.Clamp(_uiScale + delta, 0.78f, 1.22f);
            PlayerPrefs.SetFloat(PrefsUiScale, _uiScale);
            ApplyUiScale();
            RefreshLabels();
        }

        private void ApplyUiScale()
        {
            if (_hudScaleRoot != null)
                _hudScaleRoot.localScale = Vector3.one * Mathf.Clamp(_uiScale, 0.78f, 1.22f);
        }

        private void AdjustVolume(float delta)
        {
            _volume = Mathf.Clamp01(_volume + delta);
            PlayerPrefs.SetFloat(PrefsMasterVol, _volume);
            AudioListener.volume = _volume;
            RefreshLabels();
        }

        private void CycleQuality()
        {
            var n = QualitySettings.names.Length;
            _qualityIndex = (_qualityIndex + 1) % n;
            QualitySettings.SetQualityLevel(_qualityIndex);
            PlayerPrefs.SetInt(PrefsQuality, _qualityIndex);
            RefreshLabels();
        }

        private void ToggleVibrate()
        {
            _vibrate = !_vibrate;
            PlayerPrefs.SetInt(PrefsVibrate, _vibrate ? 1 : 0);
            RefreshLabels();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void AddTitle(RectTransform panel)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0.06f, 0.86f);
            rt.anchorMax = new Vector2(0.94f, 0.96f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = "Settings";
            t.fontSize = 34;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(0.12f, 0.14f, 0.2f);
            t.alignment = TextAnchor.MiddleCenter;
        }

        private Text AddStepperRow(RectTransform panel, string title, float yNorm, UnityEngine.Events.UnityAction minus,
            UnityEngine.Events.UnityAction plus)
        {
            var row = Row(panel, yNorm, 0.12f);
            var cap = TxtOn(row, "title", new Vector2(0f, 0.58f), new Vector2(1f, 1f), 20, TextAnchor.UpperLeft);
            cap.text = title;

            MakeTinyBtn(row, "−", new Vector2(0.02f, 0.05f), new Vector2(0.18f, 0.38f), minus);
            MakeTinyBtn(row, "+", new Vector2(0.82f, 0.05f), new Vector2(0.98f, 0.38f), plus);

            return TxtOn(row, "value", new Vector2(0.2f, 0.05f), new Vector2(0.8f, 0.42f), 22, TextAnchor.MiddleCenter);
        }

        private Text AddSingleButtonRow(RectTransform panel, string title, float yNorm, UnityEngine.Events.UnityAction onTap)
        {
            var row = Row(panel, yNorm, 0.12f);
            var btnGo = new GameObject("Btn", typeof(RectTransform));
            var br = btnGo.GetComponent<RectTransform>();
            br.SetParent(row, false);
            br.anchorMin = new Vector2(0.04f, 0.08f);
            br.anchorMax = new Vector2(0.96f, 0.92f);
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.88f, 0.9f, 0.95f);
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(onTap);
            var t = TxtOn(br, "t", Vector2.zero, Vector2.one, 22, TextAnchor.MiddleCenter);
            t.text = title;
            return t;
        }

        private void AddQuitAppButton(RectTransform panel, float yNorm)
        {
            var row = Row(panel, yNorm, 0.11f);
            var btnGo = new GameObject("QuitApp", typeof(RectTransform));
            var br = btnGo.GetComponent<RectTransform>();
            br.SetParent(row, false);
            br.anchorMin = new Vector2(0.04f, 0.08f);
            br.anchorMax = new Vector2(0.96f, 0.92f);
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.62f, 0.22f, 0.22f);
            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.72f, 0.28f, 0.28f);
            colors.pressedColor = new Color(0.52f, 0.18f, 0.18f);
            btn.colors = colors;
            btn.onClick.AddListener(QuitApplication);
            var t = TxtOn(br, "t", Vector2.zero, Vector2.one, 22, TextAnchor.MiddleCenter);
            t.text = "Close app";
            t.color = Color.white;
            t.fontStyle = FontStyle.Bold;
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void AddCloseButton(RectTransform panel)
        {
            var go = new GameObject("Close", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0.28f, 0.04f);
            rt.anchorMax = new Vector2(0.72f, 0.11f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.42f, 0.72f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(Toggle);
            var ct = TxtOn(rt, "Close", Vector2.zero, Vector2.one, 26, TextAnchor.MiddleCenter);
            ct.text = "Close";
            ct.color = Color.white;
            ct.fontStyle = FontStyle.Bold;
        }

        private void MakeTinyBtn(RectTransform row, string label, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("B", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(row, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.75f, 0.8f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            TxtOn(rt, label, Vector2.zero, Vector2.one, 28, TextAnchor.MiddleCenter).text = label;
        }

        private Text TxtOn(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = align;
            t.color = new Color(0.15f, 0.16f, 0.22f);
            return t;
        }

        private static RectTransform Row(RectTransform panel, float yCenterNorm, float heightNorm)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0.06f, yCenterNorm - heightNorm * 0.5f);
            rt.anchorMax = new Vector2(0.94f, yCenterNorm + heightNorm * 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static void TryVibrate()
        {
            if (PlayerPrefs.GetInt(PrefsVibrate, 1) == 0) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
