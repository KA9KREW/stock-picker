using TMPro;
using UnityEngine;

namespace StockPicker.App
{
    /// <summary>
    /// Shared TMP font resolution for UI and world-space labels when TMP Essentials are not in Assets.
    /// </summary>
    public static class TmpFontResolver
    {
        private static TMP_FontAsset s_runtimeFallbackFont;

        public static TMP_FontAsset GetFont()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            var fromResources = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fromResources != null)
                return fromResources;

            if (s_runtimeFallbackFont != null)
                return s_runtimeFallbackFont;

            Font legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacy == null)
                legacy = Resources.GetBuiltinResource<Font>("Arial.ttf");
#if !UNITY_WEBGL
            if (legacy == null)
            {
                try
                {
                    legacy = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Liberation Sans" }, 32);
                }
                catch
                {
                    // ignored
                }
            }
#endif

            if (legacy != null)
            {
                s_runtimeFallbackFont = TMP_FontAsset.CreateFontAsset(legacy);
                if (s_runtimeFallbackFont != null)
                    return s_runtimeFallbackFont;
            }

            Debug.LogError(
                "No TextMesh Pro font available. In Unity: Window > TextMeshPro > Import TMP Essential Resources.");
            return null;
        }
    }
}
