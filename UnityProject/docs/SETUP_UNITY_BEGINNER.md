# Unity setup from scratch (Windows)

This project expects **Unity 2022.3 LTS**. Your repo pins **`2022.3.62f3`** in `ProjectSettings/ProjectVersion.txt` (newer LTS patches address security fixes; avoid older 2022.3 builds Hub flags). Any **2022.3.x** patch usually opens the project fine; staying on the **2022.3 LTS** line is what matters.

## 1. Create a Unity ID (free)

1. Open [id.unity.com](https://id.unity.com/) and sign up (email + password).
2. You will use this to sign in inside **Unity Hub**.

You do **not** need a paid license for local development with the **Personal** tier (subject to Unity’s eligibility rules on their site).

## 2. Install Unity Hub

**Option A — Winget (fast on Windows 11/10):** open **PowerShell** or **Terminal** and run:

```powershell
winget install Unity.UnityHub --accept-package-agreements --accept-source-agreements
```

**Option B — Manual:** download from [unity.com/download](https://unity.com/download) and run the installer.

After install, launch **Unity Hub** and **sign in** with your Unity ID.

## 3. Install the Unity Editor (2022.3 LTS)

1. In Unity Hub, open **Installs** (left sidebar).
2. Click **Install Editor**.
3. Choose **2022.3.x LTS** — install **2022.3.62f3** or the **latest 2022.3 LTS** Hub shows (includes security fixes).
4. On the **Add modules** step, enable at least:
   - **Microsoft Visual Studio Community** (recommended) *or* **Visual Studio Code** + C# extension — for scripts and debugging.
   - **WebGL Build Support** — if you want browser builds.
   - **Android Build Support** — only when you are ready to build for Google Play (includes SDK/NDK/OpenJDK via Hub).

5. Finish the install and wait until it completes (several GB).

## 4. Open this game project

1. In Unity Hub, click **Projects** → **Add** → **Add project from disk**.
2. Select the **`UnityProject`** folder (the one that contains `Assets`, `Packages`, `ProjectSettings`).

   Example path:

   `C:\Users\user\stockticker\UnityProject`

3. Click the project to open it. **First open** can take several minutes (importing packages, generating cache).

## 5. Play in the Editor

1. In the Unity Editor, in the **Project** window, go to **`Assets/Scenes`**.
2. Double-click **`Main.unity`**.
3. Press the **Play** button at the top center.

You should see the in-game UI (buttons for Roll, trading, Save/Load).

### If `Main.unity` looks broken or empty

Use the menu: **`StockTicker` → `Setup Main Scene`**. That recreates the scene and adds it to **File → Build Settings**.

## 6. Optional: WebGL in the browser

1. **File → Build Settings…**
2. Select **WebGL**, click **Switch Platform** (first time only, can take a while).
3. Click **Build And Run** and pick an output folder (e.g. `Build/WebGL`).

Or build then from PowerShell in `UnityProject`:

```powershell
.\scripts\serve-webgl.ps1 -BuildDir .\Build\WebGL -Port 8080
```

Open `http://127.0.0.1:8080/`.

## 7. Common problems

| Problem | What to try |
|--------|-------------|
| Hub says “No editor installed” | Complete **Installs → Install Editor** (step 3). |
| Project won’t open / wrong version | Install **2022.3 LTS**; avoid 2023/6000 for this repo unless you know you’re upgrading the project. |
| Scripts don’t compile | Install **Visual Studio** with **Game development with Unity** workload, or open the project once so Hub installs **Visual Studio Community** module. |
| Pink/missing materials | Usually a one-time import issue; close Unity, delete the project’s `Library` folder, reopen (reimport takes time). |
| Firewall / corporate PC | Allow Unity Hub and Unity Editor through the firewall; some proxies block package download. |

## 8. Where saves go (Windows)

The game writes `stockticker_campaign.json` under Unity’s **persistent data path**, typically:

`%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\`

Exact names come from **Edit → Project Settings → Player → Company Name / Product Name**.
