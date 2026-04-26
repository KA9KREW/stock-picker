using UnityEngine;

namespace StockPicker.App
{
    /// <summary>
    /// Resolves a <see cref="Font"/> that works without TMP Essentials (built-in TTF, then OS fonts on supported platforms).
    /// </summary>
    public static class LegacyUiFont
    {
        private static Font s_cached;

        public static Font Get()
        {
            if (s_cached != null)
                return s_cached;

            s_cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (s_cached == null)
                s_cached = Resources.GetBuiltinResource<Font>("Arial.ttf");

#if !UNITY_WEBGL
            if (s_cached == null)
            {
                try
                {
                    s_cached = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Liberation Sans", "Helvetica" }, 32);
                }
                catch
                {
                    // ignored
                }
            }

            if (s_cached == null)
            {
                try
                {
                    s_cached = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
                catch
                {
                    // ignored
                }
            }
#endif

            if (s_cached == null)
                Debug.LogError(
                    "No UI font found. Install OS fonts or add a .ttf under Resources. HUD uses Unity UI Text (not TMP).");

            return s_cached;
        }
    }
}
