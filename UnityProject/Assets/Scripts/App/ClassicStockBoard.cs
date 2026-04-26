using UnityEngine;
using UnityEngine.Rendering;

namespace StockPicker.App
{
    /// <summary>
    /// Physical Stock Picker–style quotation board (horizontal adaptation): dense 5¢ grid 0–200¢,
    /// six stacked horizontal tracks, felt + gold frame, vintage colors.
    /// Built in local XY (+Z toward camera); parent applies pitch to lay on the table.
    /// </summary>
    public static class ClassicStockBoard
    {
        public const int TrackCount = 6;

        /// <summary>41 columns: price levels 0, 5, 10, …, 200 (cents).</summary>
        public const int PriceMin = 0;

        public const int PriceMax = 200;

        public const int PriceStep = 5;

        public const int ColumnCount = (PriceMax - PriceMin) / PriceStep + 1;

        /// <summary>Scale labels every 10¢ to stay readable; grid remains 5¢.</summary>
        public const int ScaleLabelStep = 10;

        /// <summary>Top → bottom, matching the classic board layout (Oil, then Bonds, then Industrials, then Grain).</summary>
        private static readonly string[] RowLabels = { "GOLD", "SILVER", "OIL", "BONDS", "INDUSTRIALS", "GRAIN" };

        /// <summary>Printed label for each game commodity index (same spelling as row plates).</summary>
        public static readonly string[] CommodityNameByGameIndex =
            { "GOLD", "SILVER", "BONDS", "OIL", "INDUSTRIALS", "GRAIN" };

        /// <summary>
        /// Maps game commodity index → row index in <see cref="RowLabels"/>.
        /// </summary>
        public static readonly int[] GameIndexToDisplayRow = { 0, 1, 3, 2, 4, 5 };

        private static readonly Color Felt = HexColor(0x1a2f1a);

        private static readonly Color Gold = HexColor(0xd4af37);

        private static readonly Color GoldDeep = new Color(0.58f, 0.45f, 0.14f);

        private static readonly Color GoldLine = new Color(0.82f, 0.68f, 0.28f);

        private static readonly Color LabelStrip = new Color(0.94f, 0.93f, 0.88f);

        /// <summary>
        /// Lane fills; indices follow <see cref="RowLabels"/> (GOLD…GRAIN) — matches classic board:
        /// orange, silver, peach/salmon, sage, rose, yellow.
        /// </summary>
        private static readonly Color[] TrackColors =
        {
            HexColor(0xff7700), // GOLD — bright orange
            HexColor(0xededea), // SILVER — light grey / off-white
            HexColor(0xeec4a8), // OIL — peachy tan / light salmon
            HexColor(0x95a882), // BONDS — muted sage green
            HexColor(0xebb0b8), // INDUSTRIALS — light pink / rose
            HexColor(0xffe22a) // GRAIN — bright yellow
        };

        public struct Layout
        {
            public float LabelColW;
            public float BarLength;
            public float TrackH;
            public float RowGap;
            public float TitleH;
            public float ScaleH;
            public float TopPad;
            public float BorderOuter;
            public float BorderInner;
            public float BoardHalfW;
            public float BoardHalfH;
            public float FrameMidY;
            public float BarLeftX;
            public float BarRightX;
            public float CenterOffsetY;
            public float[] TrackCenterY;
            public float TitleCenterY;
            public float ScaleCenterY;
        }

        public static Layout ComputeLayout(float labelColW, float barLength, float trackH, float rowGap, float titleH,
            float scaleH, float topPad, float borderOuter, float borderInner)
        {
            var trackCenters = new float[TrackCount];
            var gapTitle = trackH * 0.32f;
            var gapScale = trackH * 0.26f;
            float y = 0f;
            y -= topPad;
            y -= titleH * 0.5f;
            var titleCenter = y;
            y -= titleH * 0.5f + gapTitle;
            y -= scaleH * 0.5f;
            var scaleCenter = y;
            y -= scaleH * 0.5f + gapScale;

            for (var i = 0; i < TrackCount; i++)
            {
                y -= trackH * 0.5f;
                trackCenters[i] = y;
                y -= trackH * 0.5f;
                if (i < TrackCount - 1)
                    y -= rowGap;
            }

            var bottomTracks = y;
            var topEdge = titleCenter + titleH * 0.5f + topPad;
            var centerOffset = (topEdge + bottomTracks) * 0.5f;

            for (var i = 0; i < TrackCount; i++)
                trackCenters[i] -= centerOffset;

            var botE = trackCenters[TrackCount - 1] - trackH * 0.5f;
            var bottomY = botE - scaleH * 0.82f;

            titleCenter -= centerOffset;
            scaleCenter -= centerOffset;

            var ymax = titleCenter + titleH * 0.5f;
            var ymin = bottomY;
            var frameMidY = (ymax + ymin) * 0.5f;
            var innerHalfW = barLength * 0.5f + labelColW + 0.1f;
            var innerHalfH = (ymax - ymin) * 0.5f + 0.12f;

            return new Layout
            {
                LabelColW = labelColW,
                BarLength = barLength,
                TrackH = trackH,
                RowGap = rowGap,
                TitleH = titleH,
                ScaleH = scaleH,
                TopPad = topPad,
                BorderOuter = borderOuter,
                BorderInner = borderInner,
                BoardHalfW = innerHalfW + borderOuter + borderInner + 0.05f,
                BoardHalfH = innerHalfH + borderOuter + borderInner + 0.1f,
                FrameMidY = frameMidY,
                BarLeftX = -barLength * 0.5f,
                BarRightX = barLength * 0.5f,
                CenterOffsetY = centerOffset,
                TrackCenterY = trackCenters,
                TitleCenterY = titleCenter,
                ScaleCenterY = scaleCenter
            };
        }

        public static float ColumnWidth(in Layout L) => L.BarLength / ColumnCount;

        public static int PriceToColumnIndex(int priceCents)
        {
            var p = SnapPriceToStep(priceCents);
            return Mathf.Clamp(p / PriceStep, 0, ColumnCount - 1);
        }

        public static float PriceCentsToBarT(int priceCents)
        {
            var p = Mathf.Clamp(priceCents, PriceMin, PriceMax);
            return p / (float)PriceMax;
        }

        public static float PriceCentsToLocalX(in Layout L, int priceCents)
        {
            var t = PriceCentsToBarT(priceCents);
            return L.BarLeftX + t * L.BarLength;
        }

        public static float HighlightCenterX(in Layout L, int priceCents)
        {
            var idx = PriceToColumnIndex(priceCents);
            var cw = ColumnWidth(L);
            return L.BarLeftX + (idx + 0.5f) * cw;
        }

        public static int SnapPriceToStep(int priceCents)
        {
            var p = Mathf.Clamp(priceCents, PriceMin, PriceMax);
            var steps = Mathf.RoundToInt(p / (float)PriceStep);
            return Mathf.Clamp(steps * PriceStep, PriceMin, PriceMax);
        }

        public static float DisplayRowCenterY(in Layout L, int gameCommodityIndex)
        {
            var row = GameIndexToDisplayRow[Mathf.Clamp(gameCommodityIndex, 0, TrackCount - 1)];
            return L.TrackCenterY[row];
        }

        public static Vector2 HighlightSize(in Layout L)
        {
            var cw = ColumnWidth(L);
            return new Vector2(cw * 0.93f, L.TrackH * 0.86f);
        }

        public static void Build(Transform parent, in Layout L, Font font)
        {
            if (font == null)
                font = LegacyUiFont.Get();
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var shared = CreateSharedUnlitMaterial(Color.white);
            var block = new MaterialPropertyBlock();

            const float zFelt = -0.032f;
            const float zFrame = -0.014f;
            const float zCell = -0.004f;
            const float zGrid = -0.007f;
            const float zGoldSep = -0.005f;
            const float zPar = -0.003f;
            const float zText = 0.058f;

            CreateQuad(parent, "Felt", new Vector3(0f, L.FrameMidY, zFelt),
                new Vector2(L.BoardHalfW * 2f - L.BorderOuter * 0.12f, L.BoardHalfH * 2f - L.BorderOuter * 0.12f), Felt, shared,
                block);

            BuildGoldFrame(parent, L, shared, block, zFrame);

            AddTitle(parent, font, L.TitleCenterY, L, zText);

            var labelX = L.BarLeftX - L.LabelColW * 0.66f;
            var labelPlateW = L.LabelColW * 0.98f;
            var scaleChars = Mathf.Clamp(L.ScaleH * 0.22f, 0.012f, 0.02f);
            var scaleColor = new Color(0.06f, 0.06f, 0.08f);
            var cw = ColumnWidth(L);

            for (var v = PriceMin; v <= PriceMax; v += ScaleLabelStep)
            {
                var tx = PriceCentsToLocalX(L, v);
                AddLabel(parent, $"Tick_{v}", font, v.ToString(), new Vector3(tx, L.ScaleCenterY, zText), scaleChars, scaleColor,
                    FontStyle.Bold, 0.38f);
            }

            var divH = 0.003f;
            CreateQuad(parent, "ScaleLine", new Vector3((L.BarLeftX + L.BarRightX) * 0.5f, L.ScaleCenterY - L.ScaleH * 0.38f, zGrid),
                new Vector2(L.BarLength * 1.04f, divH), new Color(0.08f, 0.08f, 0.1f), shared, block);

            var xCol0 = L.BarLeftX + cw * 0.5f;
            var xCol200 = L.BarLeftX + (ColumnCount - 0.5f) * cw;
            var yTracksMid = (L.TrackCenterY[0] + L.TrackCenterY[TrackCount - 1]) * 0.5f;
            var vertChar = scaleChars * 0.52f;
            AddLabel(parent, "OffMkt", font, "OFF MARKET", new Vector3(xCol0, yTracksMid, zText + 0.006f), vertChar,
                new Color(0.15f, 0.15f, 0.18f), FontStyle.Bold, 0.3f, 90f);
            AddLabel(parent, "SplitHint", font, "SPLIT", new Vector3(xCol200, yTracksMid, zText + 0.006f), vertChar * 0.95f,
                new Color(0.12f, 0.22f, 0.42f), FontStyle.Bold, 0.3f, 90f);

            for (var row = 0; row < TrackCount; row++)
            {
                var y = L.TrackCenterY[row];
                var baseC = TrackColors[row];

                var plateC = Color.Lerp(TrackColors[row], LabelStrip, 0.35f);
                CreateQuad(parent, $"LblPlate_{row}", new Vector3(labelX, y, zCell + 0.002f),
                    new Vector2(labelPlateW, L.TrackH * 0.92f), plateC, shared, block);

                AddLabel(parent, $"RowLbl_{row}", font, RowLabels[row], new Vector3(labelX, y, zText + 0.004f),
                    Mathf.Clamp(L.TrackH * 0.22f, 0.014f, 0.024f), new Color(0.04f, 0.04f, 0.06f), FontStyle.Italic, 0.36f);

                for (var c = 0; c < ColumnCount; c++)
                {
                    var cx = L.BarLeftX + (c + 0.5f) * cw;
                    var price = PriceMin + c * PriceStep;
                    var stripe = (c & 1) == 0 ? 1f : 0.9f;
                    var cell = Color.Lerp(baseC, Color.black, 0.05f * stripe);
                    if (price <= 5)
                        cell = Color.Lerp(cell, new Color(0.55f, 0.55f, 0.52f), 0.35f);
                    if (price == 100)
                        cell = Color.Lerp(cell, Color.white, 0.22f);
                    if (price >= 195)
                        cell = Color.Lerp(cell, new Color(0.35f, 0.45f, 0.65f), 0.12f);

                    CreateQuad(parent, $"Cell_{row}_{c}", new Vector3(cx, y, zCell),
                        new Vector2(cw * 0.98f, L.TrackH * 0.94f), cell, shared, block);
                }

                for (var g = 1; g < ColumnCount; g++)
                {
                    var gx = L.BarLeftX + g * cw;
                    CreateQuad(parent, $"Vdiv_{row}_{g}", new Vector3(gx, y, zGrid),
                        new Vector2(0.0018f, L.TrackH * 0.96f), new Color(0.06f, 0.06f, 0.07f), shared, block);
                }

                CreateQuad(parent, $"BarTop_{row}", new Vector3((L.BarLeftX + L.BarRightX) * 0.5f, y + L.TrackH * 0.48f, zGrid),
                    new Vector2(L.BarLength * 1.06f, 0.002f), new Color(0.05f, 0.05f, 0.06f), shared, block);
                CreateQuad(parent, $"BarBot_{row}", new Vector3((L.BarLeftX + L.BarRightX) * 0.5f, y - L.TrackH * 0.48f, zGrid),
                    new Vector2(L.BarLength * 1.06f, 0.002f), new Color(0.05f, 0.05f, 0.06f), shared, block);

                if (row < TrackCount - 1)
                {
                    var yNext = L.TrackCenterY[row + 1];
                    var ySep = (y + yNext) * 0.5f;
                    CreateQuad(parent, $"GoldSep_{row}", new Vector3((L.BarLeftX + L.BarRightX) * 0.5f, ySep, zGoldSep),
                        new Vector2(L.BarLength * 1.08f, 0.0045f), GoldLine, shared, block);
                }
            }

            var xPar = PriceCentsToLocalX(L, 100);
            var topE = L.TrackCenterY[0] + L.TrackH * 0.5f;
            var botE = L.TrackCenterY[TrackCount - 1] - L.TrackH * 0.5f;
            var parH = topE - botE;
            var parY = (topE + botE) * 0.5f;
            CreateQuad(parent, "Par100Line", new Vector3(xPar, parY, zPar), new Vector2(0.004f, parH), Gold, shared, block);
            AddLabel(parent, "Par100", font, "PAR 100", new Vector3(xPar, L.ScaleCenterY - L.ScaleH * 0.55f, zText), scaleChars * 0.85f,
                new Color(0.1f, 0.1f, 0.12f), FontStyle.Bold, 0.36f);

            var botTickY = botE - L.RowGap * 0.35f - L.ScaleH * 0.42f;
            for (var v = PriceMin; v <= PriceMax; v += ScaleLabelStep)
            {
                var bx = PriceCentsToLocalX(L, v);
                AddLabel(parent, $"TickBot_{v}", font, v.ToString(), new Vector3(bx, botTickY, zText), scaleChars * 0.92f, scaleColor,
                    FontStyle.Bold, 0.35f);
            }
        }

        private static void BuildGoldFrame(Transform parent, in Layout L, Material shared, MaterialPropertyBlock block, float z)
        {
            var hw = L.BoardHalfW;
            var hh = L.BoardHalfH;
            var tO = L.BorderOuter;
            var tI = L.BorderInner;
            var midY = L.FrameMidY;

            void Bar(string n, Vector3 p, Vector2 sz)
            {
                CreateQuad(parent, n, p, sz, Gold, shared, block);
            }

            Bar("Frame_OuterT", new Vector3(0f, midY + hh - tO * 0.5f, z), new Vector2(hw * 2f, tO));
            Bar("Frame_OuterB", new Vector3(0f, midY - hh + tO * 0.5f, z), new Vector2(hw * 2f, tO));
            Bar("Frame_OuterL", new Vector3(-hw + tO * 0.5f, midY, z), new Vector2(tO, hh * 2f - tO * 2f));
            Bar("Frame_OuterR", new Vector3(hw - tO * 0.5f, midY, z), new Vector2(tO, hh * 2f - tO * 2f));

            var iw = hw - tO - tI * 0.5f;
            var ih = hh - tO - tI * 0.5f;
            var z2 = z + 0.003f;
            Bar("Frame_InnerT", new Vector3(0f, midY + ih - tI * 0.5f, z2), new Vector2(iw * 2f, tI));
            Bar("Frame_InnerB", new Vector3(0f, midY - ih + tI * 0.5f, z2), new Vector2(iw * 2f, tI));
            Bar("Frame_InnerL", new Vector3(-iw + tI * 0.5f, midY, z2), new Vector2(tI, ih * 2f - tI * 2f));
            Bar("Frame_InnerR", new Vector3(iw - tI * 0.5f, midY, z2), new Vector2(tI, ih * 2f - tI * 2f));

            var corner = tO * 0.55f;
            var goldDim = GoldDeep;
            CreateQuad(parent, "Corner_TR", new Vector3(hw - corner * 0.55f, midY + hh - corner * 0.55f, z + 0.002f),
                new Vector2(corner, corner), goldDim, shared, block);
            CreateQuad(parent, "Corner_TL", new Vector3(-hw + corner * 0.55f, midY + hh - corner * 0.55f, z + 0.002f),
                new Vector2(corner, corner), goldDim, shared, block);
            CreateQuad(parent, "Corner_BR", new Vector3(hw - corner * 0.55f, midY - hh + corner * 0.55f, z + 0.002f),
                new Vector2(corner, corner), goldDim, shared, block);
            CreateQuad(parent, "Corner_BL", new Vector3(-hw + corner * 0.55f, midY - hh + corner * 0.55f, z + 0.002f),
                new Vector2(corner, corner), goldDim, shared, block);
        }

        private static void AddTitle(Transform parent, Font font, float titleY, in Layout L, float zText)
        {
            const string title = "QUOTATION BOARD";
            var outline = new Color(0.02f, 0.02f, 0.02f);
            var goldC = Gold;
            var d = 0.0075f;
            var titleChars = Mathf.Clamp(L.TitleH * 0.13f, 0.04f, 0.058f);
            var k = 0;
            foreach (var o in new[]
                     {
                         new Vector3(-d, -d, 0f), new Vector3(d, -d, 0f), new Vector3(-d, d, 0f), new Vector3(d, d, 0f),
                         new Vector3(-d, 0f, 0f), new Vector3(d, 0f, 0f), new Vector3(0f, -d, 0f), new Vector3(0f, d, 0f)
                     })
            {
                AddLabel(parent, $"TitleOutline_{k++}", font, title, new Vector3(o.x, titleY + o.y, zText - 0.003f), titleChars,
                    outline, FontStyle.Bold, 0.52f);
            }

            AddLabel(parent, "TitleMain", font, title, new Vector3(0f, titleY, zText + 0.008f), titleChars, goldC, FontStyle.Bold,
                0.52f);
        }

        private static void AddLabel(Transform parent, string name, Font font, string text, Vector3 localPos, float charSize,
            Color color, FontStyle style, float meshScale, float zRotationDegrees = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotationDegrees);
            go.transform.localScale = Vector3.one * meshScale;
            var tm = go.AddComponent<TextMesh>();
            if (font != null)
                tm.font = font;
            tm.text = text;
            tm.characterSize = charSize;
            tm.fontSize = 90;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = style;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && tm.font != null && tm.font.material != null)
            {
                var mat = new Material(tm.font.material);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else
                    mat.color = color;
                mat.renderQueue = 4002;
                if (mat.HasProperty("_Cull"))
                    mat.SetInt("_Cull", (int)CullMode.Off);
                mr.material = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.sortingOrder = 30;
            }
        }

        private static void CreateQuad(Transform parent, string name, Vector3 localPos, Vector2 size, Color color, Material shared,
            MaterialPropertyBlock block)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var col = go.GetComponent<Collider>();
            if (col != null)
                Object.Destroy(col);

            var r = go.GetComponent<MeshRenderer>();
            block.Clear();
            block.SetColor("_Color", color);
            if (shared.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", color);
            r.sharedMaterial = shared;
            r.SetPropertyBlock(block);
            r.sortingOrder = -5;
        }

        private static Material CreateSharedUnlitMaterial(Color baseColor)
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
                m.SetColor("_BaseColor", baseColor);
            else
                m.color = baseColor;
            return m;
        }

        private static Color HexColor(uint rgb)
        {
            var r = ((rgb >> 16) & 0xff) / 255f;
            var g = ((rgb >> 8) & 0xff) / 255f;
            var b = (rgb & 0xff) / 255f;
            return new Color(r, g, b);
        }
    }
}
