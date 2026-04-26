# Third-party notices

Keep this file updated whenever you import fonts, SFX, music, icons, or packages with attribution requirements.

## Currently shipped in-repo (MVP)

| Component | Source | License |
|-----------|--------|---------|
| `com.unity.nuget.newtonsoft-json` | Unity NuGet | MIT (see package) |
| `com.unity.ugui` | Unity | Unity Companion License |
| `com.unity.test-framework` | Unity | Unity Companion License |
| Built-in `Arial.ttf` referenced via `Resources.GetBuiltinResource<Font>("Arial.ttf")` | Unity engine resources | Unity EULA (replace for shipping if your lawyer advises) |

## Replace-before-ship recommendations

- Swap built-in Arial references for a licensed font (e.g. SIL OFL on Google Fonts) and record it here.
- Add Kenney / CC0 / commissioned asset packs with per-asset license lines.
