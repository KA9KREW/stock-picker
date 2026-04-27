# Google Play — release checklist (Stock Picker)

Automated checks: Unity menu **StockPicker → Release → Validate Google Play settings**.

## Already set in this repo (verify after opening Unity)

- **Application ID:** `com.stockpicker.game` (change if you need a unique namespace).
- **Company name:** `StockPicker` (update to your legal / DBA name if different).
- **Target API:** 35, **Min API:** 24, **IL2CPP** for Android, **ARMv7 + ARM64** targets, **INTERNET** permission forced.
- **Backend:** EDM4U (`com.google.external-dependency-manager`) in `Packages/manifest.json`.

## You must do in Unity before every store upload

1. **Icons:** Player Settings → Android → assign **adaptive** and **legacy** icons (all required sizes). Validator fails if none are set.
2. **Keystore:** Player Settings → Publishing Settings → create or link your **upload keystore**; never lose the keystore/password.
3. **Versioning:** Increment **Bundle Version Code** for each Play upload; keep **Version** (e.g. `1.0.1`) in sync with store listing.
4. **Build:** **AAB** (Google Play App Bundle), **Release** (not Development), **ARM64** included (already enabled with current architecture flags).
5. **Device test:** Install the release AAB (internal testing track or `bundletool`). Confirm: cold start, Google sign-in, PlayFab session, one full game loop, **Close app** / backgrounding.

## Play Console (mandatory policy)

- **Privacy policy URL** (required: you collect accounts / IDs via Google + PlayFab).
- **Data safety** form: disclose sign-in, PlayFab player ID, statistics/leaderboards, data sent to Microsoft (PlayFab) and Google.
- **Content rating** (IARC), **target audience**, **news app** / **COVID** / **government** declarations as applicable.
- **Store listing:** screenshots, feature graphic, description — no misleading claims.

## Backend configuration (production)

On the scene object with **StockPickerGameRoot**:

- `_useLocalMockBackend` = **unchecked**
- `_backendConfig.PlayFabTitleId` = your live title
- `_backendConfig.GoogleWebClientId` = **Web client** OAuth ID from Google Cloud
- In **Google Cloud** and **Play App Signing**, register **SHA-1/256** for your **upload** and **app signing** certificates as Google’s docs require.
- In **PlayFab Game Manager:** create statistics matching `BackendConfig` (`BeatMarketLifetimeCents`, `BeatMarketBestSeasonCents`), enable leaderboards, enable **Google** login per PlayFab docs.

## What “no issues going live” cannot promise

Play / Google can still reject for policy, crashes on specific devices, or misconfigured OAuth. This checklist removes common **technical** blockers; **policy and QA** remain your responsibility.
