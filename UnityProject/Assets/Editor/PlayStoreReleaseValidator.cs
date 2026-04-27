using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StockPicker.Editor
{
    /// <summary>
    /// Pre-upload checks for Google Play. Does not replace Play Console review or device testing.
    /// </summary>
    public static class PlayStoreReleaseValidator
    {
        [MenuItem("StockPicker/Release/Validate Google Play settings")]
        public static void Validate()
        {
            var blocking = 0;

            var packageId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (string.IsNullOrWhiteSpace(packageId))
            {
                Debug.LogError("[PlayStore] Android application ID (package name) is empty.");
                blocking++;
            }
            else if (packageId.Contains("DefaultCompany") || packageId.Contains("UnityProject") ||
                     packageId.Contains("com.unity."))
            {
                Debug.LogError($"[PlayStore] Application ID looks like a placeholder: {packageId}. Set a unique reverse-DNS id.");
                blocking++;
            }

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            {
                Debug.LogError("[PlayStore] Set Android scripting backend to IL2CPP (Player Settings → Other Settings).");
                blocking++;
            }

            var arch = PlayerSettings.Android.targetArchitectures;
            if ((arch & AndroidArchitecture.ARM64) == 0)
            {
                Debug.LogError("[PlayStore] Enable ARM64 device target (Google Play 64-bit requirement).");
                blocking++;
            }

            var targetApiLevel = (int)PlayerSettings.Android.targetSdkVersion;
            if (targetApiLevel == 0)
                Debug.LogWarning("[PlayStore] Target API is 0 (Automatic). For reproducible Play uploads, pin Target API to current Play requirement (e.g. 35).");
            else if (targetApiLevel < 34)
                Debug.LogWarning($"[PlayStore] Target API {targetApiLevel} may be below current Google Play requirements. Confirm in Play Console.");

            if (!PlayerSettings.Android.forceInternetPermission)
                Debug.LogWarning("[PlayStore] INTERNET permission not forced — enable if networking (PlayFab / sign-in) fails on device.");

            if (!HasAnyLauncherIcon())
            {
                Debug.LogError("[PlayStore] No Android launcher icons assigned. Fill all required slots in Player Settings → Mobile Icons.");
                blocking++;
            }

            if (PlayerSettings.companyName == "DefaultCompany")
                Debug.LogWarning("[PlayStore] Company Name is still DefaultCompany — set a real publisher name.");

            if (PlayerSettings.productName.Contains("template") || PlayerSettings.productName.Length < 2)
                Debug.LogWarning("[PlayStore] Review Product Name for store listing consistency.");

            Debug.Log(blocking == 0
                ? "[PlayStore] Basic automated checks passed. Complete UnityProject/GOOGLE_PLAY_RELEASE.md and test a release AAB on device."
                : $"[PlayStore] {blocking} blocking issue(s) — fix before uploading to Play Console.");
        }

        private static bool HasAnyLauncherIcon()
        {
            try
            {
                foreach (var kind in PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android))
                {
                    var icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
                    if (icons != null && icons.Any(t => t != null && t.width > 0))
                        return true;
                }
            }
            catch
            {
                // Older Unity API surface; fall through
            }

            return false;
        }
    }
}
