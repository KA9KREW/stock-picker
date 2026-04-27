using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StockPicker.Editor
{
    /// <summary>
    /// One-click release AAB output for Google Play (development off, App Bundle on).
    /// </summary>
    public static class PlayStoreAndroidBuildMenu
    {
        private const string OutputDir = "Build/Android";
        private const string DefaultAabFileName = "StockPicker-release.aab";

        [MenuItem("StockPicker/Release/Build release AAB (Android)")]
        public static void BuildReleaseAab()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Debug.LogError("[Build] Could not switch to Android build target.");
                    return;
                }
            }

            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outDir = Path.Combine(projectRoot, OutputDir);
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            var aabPath = Path.Combine(outDir, DefaultAabFileName);
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] Add at least one enabled scene in File → Build Settings.");
                return;
            }

            Debug.Log($"[Build] Building release AAB → {aabPath}");
            var report = BuildPipeline.BuildPlayer(scenes, aabPath, BuildTarget.Android, BuildOptions.None);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[Build] Succeeded: {Path.GetFullPath(aabPath)}");
            else
                Debug.LogError($"[Build] Failed with {report.summary.result}. See Console / {report.summary}.");
        }
    }
}
