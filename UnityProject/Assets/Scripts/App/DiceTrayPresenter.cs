using System.Collections;
using StockPicker.Game.Core;
using UnityEngine;

namespace StockPicker.App
{
    /// <summary>
    /// Three cubes + <see cref="TextMesh"/> labels for commodity / movement / magnitude (order matches <see cref="DiceRoll"/> fields).
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class DiceTrayPresenter : MonoBehaviour
    {
        /// <summary>First die labels; matches <see cref="CommodityId"/> order (Industrials shortened for the cube).</summary>
        private static readonly string[] DiceCommodityLabels = { "GOLD", "SILVER", "BONDS", "OIL", "IND", "GRAIN" };

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private BoardWorldPresenter _board;

        [Tooltip("Tray position in board-face space (parent = BoardContent when vintage). +X = right, −Y = down the board.")]
        [SerializeField]
        private Vector3 _trayLocalPositionVintage = new Vector3(1.72f, -1.44f, 0.065f);

        [Tooltip("Tray position in table space (parent = BoardRoot when legacy / no face).")]
        [SerializeField]
        private Vector3 _trayLocalPositionTable = new Vector3(1.95f, 0.02f, -1.76f);

        [SerializeField] private float _dieSize = 0.16f;
        [SerializeField] private float _dieSpacing = 0.42f;

        [Tooltip("Push labels along board +Z (toward camera) so TextMesh draws in front of the cubes.")]
        [SerializeField]
        private float _labelZInFrontOfDice = 0.1f;

        private Transform _tray;
        private Transform[] _dieRoots = new Transform[3];
        private TextMesh[] _labels = new TextMesh[3];
        private DiceRoll? _lastShownRoll;
        private Coroutine _spinRoutine;
        private Material _dieMat;
        private Font _font;

        private void Reset()
        {
            _flow = GetComponent<GameFlowController>();
            _board = GetComponent<BoardWorldPresenter>();
        }

        private void Awake()
        {
            if (_flow == null)
                _flow = GetComponent<GameFlowController>();
            if (_board == null)
                _board = GetComponent<BoardWorldPresenter>();

            _font = LegacyUiFont.Get();
            _dieMat = CreateIvoryMaterial();
            BuildTray();
        }

        private void OnEnable()
        {
            if (_flow != null)
                _flow.UiUpdated += OnUiUpdated;
        }

        private void OnDisable()
        {
            if (_flow != null)
                _flow.UiUpdated -= OnUiUpdated;
        }

        private void OnUiUpdated()
        {
            var roll = _flow != null ? _flow.LastDiceRoll : null;
            if (!roll.HasValue)
            {
                _lastShownRoll = null;
                SetLabels("?", "?", "?");
                return;
            }

            if (_lastShownRoll.HasValue && _lastShownRoll.Value.Equals(roll.Value))
                return;
            _lastShownRoll = roll;

            if (_spinRoutine != null)
                StopCoroutine(_spinRoutine);
            _spinRoutine = StartCoroutine(SpinThenShow(roll.Value));
        }

        private IEnumerator SpinThenShow(DiceRoll target)
        {
            var elapsed = 0f;
            const float dur = 0.55f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                for (var i = 0; i < 3; i++)
                {
                    if (_dieRoots[i] == null) continue;
                    _dieRoots[i].localRotation = Quaternion.Euler(
                        Random.Range(0f, 360f),
                        Random.Range(0f, 360f),
                        Random.Range(0f, 360f));
                }

                yield return null;
            }

            for (var i = 0; i < 3; i++)
            {
                if (_dieRoots[i] != null)
                    _dieRoots[i].localRotation = Quaternion.Euler(12f + i * 7f, -18f + i * 9f, 6f);
            }

            ApplyRollToLabels(target);
            _spinRoutine = null;
        }

        private void LateUpdate()
        {
            if (_labels == null || Camera.main == null) return;
            var camRot = Camera.main.transform.rotation;
            for (var i = 0; i < _labels.Length; i++)
            {
                if (_labels[i] == null) continue;
                _labels[i].transform.rotation = camRot;
            }
        }

        private void BuildTray()
        {
            if (_tray != null) return;

            Transform parent;
            Vector3 pos;
            if (_board != null && _board.BoardRoot != null)
            {
                parent = _board.DiceTrayAttachPoint;
                pos = _board.UsesVintageBoard ? _trayLocalPositionVintage : _trayLocalPositionTable;
            }
            else
            {
                parent = transform;
                pos = _trayLocalPositionTable;
            }

            _tray = new GameObject("DiceTray").transform;
            _tray.SetParent(parent, false);
            _tray.localPosition = pos;
            _tray.localRotation = Quaternion.identity;

            for (var i = 0; i < 3; i++)
            {
                var die = GameObject.CreatePrimitive(PrimitiveType.Cube);
                die.name = "Die_" + i;
                die.transform.SetParent(_tray, false);
                die.transform.localPosition = new Vector3((i - 1) * _dieSpacing, _dieSize * 0.5f, 0f);
                die.transform.localScale = Vector3.one * _dieSize;
                var col = die.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                var r = die.GetComponent<MeshRenderer>();
                r.sharedMaterial = _dieMat;
                _dieRoots[i] = die.transform;

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(_tray, false);
                labelGo.transform.localPosition = die.transform.localPosition +
                    new Vector3(0f, _dieSize * 0.62f, _labelZInFrontOfDice);
                labelGo.transform.localScale = Vector3.one;
                var tm = labelGo.AddComponent<TextMesh>();
                tm.font = _font;
                tm.characterSize = 0.025f;
                tm.fontSize = 48;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(0.1f, 0.1f, 0.12f);
                tm.fontStyle = FontStyle.Bold;
                var lr = labelGo.GetComponent<MeshRenderer>();
                if (lr != null)
                    lr.sortingOrder = 12;
                _labels[i] = tm;
            }

            SetLabels("?", "?", "?");
        }

        private void ApplyRollToLabels(DiceRoll r)
        {
            var idx = Mathf.Clamp((int)r.Commodity, 0, 5);
            var c = DiceCommodityLabels[idx];
            var m = r.Movement switch
            {
                MovementKind.Up => "UP",
                MovementKind.Down => "DOWN",
                MovementKind.Dividend => "DIV",
                _ => "?"
            };
            var cents = $"{r.Cents}c";
            SetLabels(c, m, cents);
        }

        private void SetLabels(string a, string b, string c)
        {
            if (_labels[0] != null) _labels[0].text = a;
            if (_labels[1] != null) _labels[1].text = b;
            if (_labels[2] != null) _labels[2].text = c;
        }

        private static Material CreateIvoryMaterial()
        {
            Shader shader = null;
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            var m = new Material(shader);
            var ivory = new Color(0.93f, 0.91f, 0.86f);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", ivory);
            else
                m.color = ivory;
            return m;
        }
    }
}
