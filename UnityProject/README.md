# Market Dice (working title) — Unity MVP

Original-branded, offline-first stock-commodity dice game inspired by classic **Stock Ticker**-style board rules (six commodities, three-dice outcomes, dividends, splits, wipeouts, periodic trading). See [Wikipedia: Stock Ticker (board game)](https://en.wikipedia.org/wiki/Stock_Ticker) for rules inspiration — do **not** reuse trademarked names/art from any publisher.

**New to Unity?** Follow the step-by-step guide: [`docs/SETUP_UNITY_BEGINNER.md`](docs/SETUP_UNITY_BEGINNER.md) (install Hub, 2022.3 LTS, open this folder, press Play).

**Cursor / AI:** Open the **`UnityProject`** folder in Cursor. Primary rules live in [`.cursor/rules/stock-ticker.mdc`](.cursor/rules/stock-ticker.mdc) (`alwaysApply: true`). Supplementary [Common-ka/ai-agent-unity-rules](https://github.com/Common-ka/ai-agent-unity-rules) patterns are vendored as `common-ka-*.mdc` with `alwaysApply: false` — they target Unity 6.x; **this repo stays on 2022.3.62f3** unless you upgrade. Reload the Cursor window after pulling changes.

**URP (after pulling packages):** In Unity, run **`StockTicker > Rendering > Create and Assign URP (2022.3)`** once so the 3D board uses the Universal RP (fixes pink materials for URP/Unlit). Then **Window > TextMeshPro > Import TMP Essential Resources** if prompted.

**Custom board mesh:** `Assets/Art/Board/StockTickerBoard.obj` is included (your upload). After import, expand the asset in the Project window, **drag the Mesh** onto **Board World Presenter → Board Mesh** on the `StockTicker` object. Tune **Board Mesh Scale** and **Token layout** fields so the six tokens line up with the printed tracks. Add the companion `.mtl`/textures beside the `.obj` if you want original materials.

## Quick start (local playtest)

1. Install **Unity 2022.3.62f3** (or any newer **2022.3 LTS** patch Hub offers) with **WebGL Build Support** (optional but recommended for browser tests).
2. In **Unity Hub**, **Add** / open this folder: `UnityProject`.
3. Open **`Assets/Scenes/Main.unity`** (already in repo; also listed in **File > Build Settings**).
4. Press **Play** ▶.

Optional: menu **`StockTicker > Setup Main Scene`** regenerates the same scene and re-adds it to Build Settings (useful if the scene YAML was upgraded oddly by your Unity version).

## Requirements

- Unity **2022.3 LTS** (matches `ProjectSettings/ProjectVersion.txt`)
- Android Build Support module (for `.aab`)
- WebGL module (for browser testing)

## First-time setup

1. Open the `UnityProject` folder in Unity Hub.
2. Wait for packages to resolve (includes `Newtonsoft.Json` + Test Framework).
3. If `Main.unity` is missing or broken, run **`StockTicker > Setup Main Scene`**.

## Play in Editor

Open `Assets/Scenes/Main.unity` and press **Play**.

- **Roll / Continue**: resolves dice while in `Rolling`, advances season summary while in `SeasonComplete`.
- **Trading window**: queue human orders (commodity / lot / buy vs sell), then **Resolve trading** (AI orders are generated automatically) or **Skip trading**.
- **Save / Load** uses `Application.persistentDataPath/stockticker_campaign.json` (Newtonsoft JSON).  
  On Windows this is typically under `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\` (see **Edit > Project Settings > Player**).

## Tests

Open **Window > General > Test Runner**, select **EditMode**, run `StockTicker.Game.Tests`.

## WebGL (local browser test)

**Easiest:** **File > Build Settings…** → platform **WebGL** → **Build And Run**. Unity starts a local server and opens the browser.

**Manual build + serve:**

1. **File > Build Settings…** → **WebGL** → pick output folder (e.g. `Build/WebGL`) → **Build**.
2. Serve that folder over **http** (`file://` is unreliable for WebGL).

From `UnityProject` in PowerShell:

```powershell
.\scripts\serve-webgl.ps1 -BuildDir .\Build\WebGL -Port 8080
```

The script tries **Python** `http.server` first; if Python is not installed, it falls back to a small **PowerShell** static server.

Then open `http://127.0.0.1:8080/`.

## Android (Google Play, paid app checklist)

1. **Player Settings**: unique package name, version code/name, icons, keystore.
2. **Publishing format**: **AAB** (Play requirement).
3. **Play Console**: create app, **closed testing** track first, complete **Data safety** form (this MVP is offline-only; declare no collection if accurate).
4. **Pricing**: set paid SKU (e.g. **$4.99 USD**).
5. **Store listing**: screenshots, feature graphic, privacy policy URL (even if minimal for offline-only).

## Project layout

- `Assets/Scripts/Game/Core` — rules engine, session, RNG, trading resolution.
- `Assets/Scripts/Game/AI` — six bot personalities + factory.
- `Assets/Scripts/Game/Progression` — season wins + cosmetic unlock IDs (data-only for MVP UI).
- `Assets/Scripts/Infrastructure/Save` — JSON persistence.
- `Assets/Scripts/App` — `StockTickerGameRoot` runtime UI bootstrap.
- `Assets/Editor` — one-click scene setup.

## Legal / IP

Read `docs/BRANDING_AND_IP.md` and keep `Assets/ThirdParty/ATTRIBUTIONS.md` updated for any imported art/audio/fonts.
