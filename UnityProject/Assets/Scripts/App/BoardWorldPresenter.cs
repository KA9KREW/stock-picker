using StockPicker.Game.Core;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StockPicker.App
{
    public enum VintageBoardVisualKind
    {
        /// <summary>Procedural <see cref="ClassicStockBoard"/> only.</summary>
        ProceduralClassic = 0,

        /// <summary>Blender / .obj model (keeps per-face materials). Falls back to procedural if none assigned.</summary>
        ImportedModel = 1,

        /// <summary>Imported mesh behind the procedural board.</summary>
        ProceduralWithImportedUnderlay = 2
    }

    /// <summary>
    /// 3D board: imported board mesh (default when available), procedural classic board, or legacy table.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public sealed class BoardWorldPresenter : MonoBehaviour
    {
        private const string DefaultBoardModelAssetPath = "Assets/Art/Board/StockTickerBoard.obj";

        [Header("References")]
        [SerializeField] private GameFlowController _controller;
        [SerializeField] private Transform _boardRoot;

        [Header("Presentation")]
        [SerializeField] private bool _useVintageBoard = true;

        [Tooltip("Imported .obj is used when assigned (or auto-found in Editor). Procedural = quad-built board only.")]
        [SerializeField]
        private VintageBoardVisualKind _vintageBoardVisual = VintageBoardVisualKind.ImportedModel;

        [Header("Classic quotation board (horizontal tracks)")]
        [SerializeField] private float _classicLabelColW = 0.54f;
        [SerializeField] private float _classicBarLength = 2.95f;
        [SerializeField] private float _classicTrackH = 0.15f;
        [SerializeField] private float _classicRowGap = 0.026f;
        [SerializeField] private float _classicTitleH = 0.44f;
        [SerializeField] private float _classicScaleH = 0.24f;
        [SerializeField] private float _classicTopPad = 0.075f;
        [SerializeField] private float _classicBorderOuter = 0.052f;
        [SerializeField] private float _classicBorderInner = 0.024f;
        [SerializeField] private Vector3 _vintageFaceLocalPosition = new Vector3(0f, 0.052f, 0.05f);
        [SerializeField] private float _vintageBoardYawDegrees = 0f;
        [SerializeField] private Vector3 _vintageFaceLocalScale = new Vector3(1.12f, 1.12f, 1.12f);
        [SerializeField] private float _tokenForwardZ = 0.05f;

        [Header("Imported board (optional)")]
        [Tooltip("Drag StockTickerBoard.obj from Project — keeps all materials. Preferred over raw Mesh.")]
        [SerializeField]
        private GameObject _importedBoardModel;

        [Tooltip("Legacy: single Mesh sub-asset; all submeshes share one material unless you use Imported Board Model.")]
        [SerializeField]
        private Mesh _boardMesh;

        [SerializeField] private Material _boardMeshMaterial;
        [SerializeField] private Vector3 _boardMeshScale = new Vector3(0.05f, 0.05f, 0.05f);

        [Tooltip("Extra offset after optional auto-centering")]
        [SerializeField] private Vector3 _boardMeshLocalPosition;

        [SerializeField] private Vector3 _boardMeshEuler;

        [Tooltip("Move mesh so its bounds center sits at parent origin (helps Blender exports).")]
        [SerializeField]
        private bool _centerImportedMeshPivot = true;

        [Tooltip("Z offset (local) for imported board behind procedural when using underlay.")]
        [SerializeField]
        private float _importedBoardZBehindProcedural = -0.018f;

        [Tooltip("If true, skip the placeholder cube when a mesh is assigned (legacy table mode)")]
        [SerializeField] private bool _hideProceduralTableWhenMeshAssigned = true;

        [Tooltip("Procedural rails only (legacy table mode)")]
        [SerializeField] private bool _showProceduralRails = true;

        [Header("Legacy token layout (when vintage is off)")]
        [SerializeField] private Vector3 _tokenOrigin = new Vector3(-2.2f, 0.12f, 0f);

        [SerializeField] private float _tokenSpacingX = 0.88f;
        [SerializeField] private float _tokenZMin = -1.75f;
        [SerializeField] private float _tokenZMax = 1.75f;

        private Transform[] _tokens = new Transform[6];
        private Transform[] _priceHighlights = new Transform[6];
        private Vector3[] _targets = new Vector3[6];
        private static readonly Color PawnRed = new(0.9f, 0.08f, 0.06f);
        private static readonly Color PawnRing = new(0.15f, 0.02f, 0.02f);

        private Material _tableMat;
        private Material _railMat;
        private Transform _vintageFace;
        private Transform _vintageBoardContent;
        private ClassicStockBoard.Layout _classicLayout;

        public Transform BoardRoot => _boardRoot;

        /// <summary>True when the classic / imported quotation face is active (tokens use <see cref="ClassicStockBoard"/> layout).</summary>
        public bool UsesVintageBoard => _useVintageBoard;

        /// <summary>
        /// Parent for dice: same transform as pawns/highlights when vintage, so rolls sit on the board face.
        /// Otherwise <see cref="BoardRoot"/> (table space).
        /// </summary>
        public Transform DiceTrayAttachPoint =>
            _useVintageBoard && _vintageBoardContent != null ? _vintageBoardContent : _boardRoot;

        private void Reset()
        {
            _controller = GetComponent<GameFlowController>();
#if UNITY_EDITOR
            if (_importedBoardModel == null)
                _importedBoardModel = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBoardModelAssetPath);
#endif
        }

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponent<GameFlowController>();
            if (_boardRoot == null)
            {
                var br = new GameObject("BoardRoot");
                br.transform.SetParent(transform, false);
                _boardRoot = br.transform;
            }

            TryResolveImportedBoardModel();
            BuildGeometry();
        }

        private void OnEnable()
        {
            if (_controller != null)
                _controller.UiUpdated += SyncFromSession;
        }

        private void OnDisable()
        {
            if (_controller != null)
                _controller.UiUpdated -= SyncFromSession;
        }

        private void Start()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (_useVintageBoard)
            {
                cam.transform.SetPositionAndRotation(new Vector3(0f, 2.72f, -2.25f), Quaternion.Euler(54f, 0f, 0f));
                cam.fieldOfView = 56f;
            }
            else
            {
                cam.transform.SetPositionAndRotation(new Vector3(0.15f, 3.4f, -5.1f), Quaternion.Euler(28f, -12f, 0f));
                cam.fieldOfView = 42f;
            }
        }

        private void Update()
        {
            for (var i = 0; i < 6; i++)
            {
                if (_tokens[i] == null) continue;
                _tokens[i].localPosition = Vector3.Lerp(_tokens[i].localPosition, _targets[i], Time.deltaTime * 10f);
            }
        }

        private void BuildGeometry()
        {
            _tableMat = CreateMat(new Color(0.38f, 0.28f, 0.2f));
            _railMat = CreateMat(new Color(0.35f, 0.28f, 0.22f));

            if (_useVintageBoard)
                BuildVintagePresentation();
            else
                BuildLegacyPresentation();

            for (var i = 0; i < 6; i++)
            {
                var tok = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tok.name = "Token_" + (CommodityId)i;
                var parent = _useVintageBoard && _vintageBoardContent != null ? _vintageBoardContent : _boardRoot;
                tok.transform.SetParent(parent, false);
                tok.transform.localRotation = Quaternion.identity;
                tok.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                DestroyCollider(tok);
                ApplyMaterial(tok, CreateMat(PawnRed));

                var sh = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sh.name = "PawnShadow";
                sh.transform.SetParent(tok.transform, false);
                sh.transform.localPosition = new Vector3(0f, 0f, -0.022f);
                sh.transform.localRotation = Quaternion.identity;
                sh.transform.localScale = new Vector3(0.24f, 0.09f, 1f);
                DestroyCollider(sh);
                ApplyMaterial(sh, CreateMat(new Color(0.04f, 0.04f, 0.06f)));

                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "TokenRing_" + (CommodityId)i;
                ring.transform.SetParent(tok.transform, false);
                ring.transform.localPosition = new Vector3(0f, -0.065f, 0f);
                ring.transform.localScale = new Vector3(0.28f, 0.018f, 0.28f);
                DestroyCollider(ring);
                ApplyMaterial(ring, CreateMat(PawnRing));

                _tokens[i] = tok.transform;
                _targets[i] = TokenPosition(i, 100, GameRules.CreateDefaultRuntime());
            }
        }

        private void BuildVintagePresentation()
        {
            var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = "Table";
            stand.transform.SetParent(_boardRoot, false);
            stand.transform.localScale = new Vector3(5f, 0.06f, 2.8f);
            stand.transform.localPosition = new Vector3(0f, -0.03f, 0.2f);
            DestroyCollider(stand);
            ApplyMaterial(stand, _tableMat);

            _classicLayout = ClassicStockBoard.ComputeLayout(_classicLabelColW, _classicBarLength, _classicTrackH,
                _classicRowGap, _classicTitleH, _classicScaleH, _classicTopPad, _classicBorderOuter, _classicBorderInner);

            var faceGo = new GameObject("VintageBoardFace");
            faceGo.transform.SetParent(_boardRoot, false);
            faceGo.transform.localPosition = _vintageFaceLocalPosition;
            faceGo.transform.localRotation = Quaternion.Euler(0f, _vintageBoardYawDegrees, 0f);
            faceGo.transform.localScale = _vintageFaceLocalScale;
            _vintageFace = faceGo.transform;

            var flatGo = new GameObject("BoardFlat");
            flatGo.transform.SetParent(_vintageFace, false);
            flatGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            flatGo.transform.localPosition = Vector3.zero;

            var contentGo = new GameObject("BoardContent");
            _vintageBoardContent = contentGo.transform;
            _vintageBoardContent.SetParent(flatGo.transform, false);
            _vintageBoardContent.localRotation = Quaternion.identity;
            _vintageBoardContent.localPosition = Vector3.zero;

            var hasModel = _importedBoardModel != null;
            var wantImport = hasModel && _vintageBoardVisual != VintageBoardVisualKind.ProceduralClassic;
            var wantProcedural = !hasModel || _vintageBoardVisual != VintageBoardVisualKind.ImportedModel;

            if (wantImport && wantProcedural)
                InstantiateImportedBoard(_vintageBoardContent, _importedBoardZBehindProcedural);

            if (wantProcedural)
            {
                var font = LegacyUiFont.Get();
                ClassicStockBoard.Build(_vintageBoardContent, _classicLayout, font);
            }
            else if (wantImport)
            {
                InstantiateImportedBoard(_vintageBoardContent, 0f);
            }
            else if (_vintageBoardVisual == VintageBoardVisualKind.ImportedModel && !hasModel)
            {
                Debug.LogWarning(
                    "Vintage board is set to Imported Model but no model was assigned and none found at " +
                    DefaultBoardModelAssetPath + ". Using procedural board. Drag StockTickerBoard.obj into Imported Board Model.");
                var font = LegacyUiFont.Get();
                ClassicStockBoard.Build(_vintageBoardContent, _classicLayout, font);
            }

            BuildPriceHighlights();
        }

        private void BuildPriceHighlights()
        {
            var sz = ClassicStockBoard.HighlightSize(_classicLayout);
            var mat = CreateMat(new Color(1f, 0.94f, 0.18f));
            for (var i = 0; i < 6; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "PriceHighlight_" + (CommodityId)i;
                go.transform.SetParent(_vintageBoardContent, false);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = new Vector3(sz.x, sz.y, 1f);
                DestroyCollider(go);
                ApplyMaterial(go, mat);
                _priceHighlights[i] = go.transform;
                var p = TokenPosition(i, 100, GameRules.CreateDefaultRuntime());
                _priceHighlights[i].localPosition = new Vector3(p.x, p.y, 0.028f);
            }
        }

        private void BuildLegacyPresentation()
        {
            var hasImported = _importedBoardModel != null;
            var useMesh = _boardMesh != null;
            var skipTable = _hideProceduralTableWhenMeshAssigned && (useMesh || hasImported);

            if (!skipTable)
            {
                var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                table.name = "Table";
                table.transform.SetParent(_boardRoot, false);
                table.transform.localScale = new Vector3(6f, 0.08f, 4.2f);
                table.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                DestroyCollider(table);
                ApplyMaterial(table, _tableMat);
            }

            if (hasImported)
                InstantiateImportedBoard(_boardRoot, 0f);
            else if (useMesh)
                AddImportedBoard();

            for (var i = 0; i < 6; i++)
            {
                if (!_showProceduralRails) continue;
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "Rail_" + (CommodityId)i;
                rail.transform.SetParent(_boardRoot, false);
                rail.transform.localScale = new Vector3(0.06f, 0.02f, 3.8f);
                rail.transform.localPosition = RailOrigin(i);
                DestroyCollider(rail);
                ApplyMaterial(rail, _railMat);
            }
        }

        private void TryResolveImportedBoardModel()
        {
#if UNITY_EDITOR
            if (_importedBoardModel == null)
                _importedBoardModel = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBoardModelAssetPath);
#endif
            if (_importedBoardModel == null)
                _importedBoardModel = Resources.Load<GameObject>("Board/StockTickerBoard");
        }

        private void InstantiateImportedBoard(Transform parent, float extraLocalZ)
        {
            if (_importedBoardModel == null) return;

            var inst = Instantiate(_importedBoardModel, parent);
            inst.name = "ImportedBoard";
            foreach (var col in inst.GetComponentsInChildren<Collider>())
                Destroy(col);

            inst.transform.localRotation = Quaternion.Euler(_boardMeshEuler);
            inst.transform.localScale = _boardMeshScale;
            inst.transform.localPosition = new Vector3(0f, 0f, extraLocalZ);

            if (!_centerImportedMeshPivot)
            {
                inst.transform.localPosition += _boardMeshLocalPosition;
                return;
            }

            var renderers = inst.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                inst.transform.localPosition += _boardMeshLocalPosition;
                return;
            }

            var w = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                w.Encapsulate(renderers[i].bounds);

            var centerInParent = parent.InverseTransformPoint(w.center);
            inst.transform.localPosition = -centerInParent + _boardMeshLocalPosition + new Vector3(0f, 0f, extraLocalZ);
        }

        private void AddImportedBoard()
        {
            var go = new GameObject("ImportedBoard");
            go.transform.SetParent(_boardRoot, false);
            go.transform.localScale = _boardMeshScale;
            go.transform.localEulerAngles = _boardMeshEuler;

            if (_centerImportedMeshPivot)
            {
                var c = _boardMesh.bounds.center;
                go.transform.localPosition = new Vector3(
                    -c.x * _boardMeshScale.x,
                    -c.y * _boardMeshScale.y,
                    -c.z * _boardMeshScale.z) + _boardMeshLocalPosition;
            }
            else
            {
                go.transform.localPosition = _boardMeshLocalPosition;
            }

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = _boardMesh;
            var mr = go.AddComponent<MeshRenderer>();
            var fallback = _boardMeshMaterial != null ? _boardMeshMaterial : _tableMat;
            var n = Mathf.Max(1, _boardMesh.subMeshCount);
            var mats = new Material[n];
            for (var i = 0; i < n; i++)
                mats[i] = fallback;
            mr.sharedMaterials = mats;
        }

        private Vector3 RailOrigin(int commodityIndex)
        {
            var x = _tokenOrigin.x + commodityIndex * _tokenSpacingX;
            var zMid = (_tokenZMin + _tokenZMax) * 0.5f;
            return new Vector3(x, 0.02f, zMid);
        }

        private Vector3 TokenPosition(int commodityIndex, int priceCents, GameRules rules)
        {
            if (_useVintageBoard)
            {
                var x = ClassicStockBoard.HighlightCenterX(_classicLayout, priceCents);
                var y = ClassicStockBoard.DisplayRowCenterY(_classicLayout, commodityIndex);
                return new Vector3(x, y, _tokenForwardZ);
            }

            var min = Mathf.Max(5, rules.wipeoutAtCents + 5);
            var max = Mathf.Max(min + 1, rules.splitThresholdCents);
            var p = Mathf.Clamp(priceCents, min, max);
            var t = Mathf.InverseLerp(min, max, p);
            var xl = _tokenOrigin.x + commodityIndex * _tokenSpacingX;
            var z = Mathf.Lerp(_tokenZMin, _tokenZMax, t);
            return new Vector3(xl, _tokenOrigin.y, z);
        }

        private void SyncFromSession()
        {
            if (_controller?.Session == null || _controller.Rules == null) return;
            var prices = _controller.Session.State.Market.PricesCents;
            var rules = _controller.Rules;
            for (var i = 0; i < 6; i++)
            {
                _targets[i] = TokenPosition(i, prices[i], rules);
                if (_priceHighlights[i] != null)
                {
                    var hp = _targets[i];
                    _priceHighlights[i].localPosition = new Vector3(hp.x, hp.y, 0.028f);
                }
            }
        }

        private static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null)
                Destroy(c);
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = mat;
        }

        private static Material CreateMat(Color c)
        {
            Shader shader = null;
            if (GraphicsSettings.defaultRenderPipeline != null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var m = new Material(shader);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            else
                m.color = c;
            return m;
        }
    }
}
