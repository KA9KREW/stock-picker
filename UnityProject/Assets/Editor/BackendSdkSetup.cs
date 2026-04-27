using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StockPicker.Editor
{
    [InitializeOnLoad]
    public static class BackendSdkSetup
    {
        private const string PlayFabDefine = "PLAYFAB_SDK";
        private const string GoogleDefine = "GOOGLE_SIGNIN_SDK";
        private static readonly BuildTargetGroup[] TargetGroups = { BuildTargetGroup.Android, BuildTargetGroup.iOS };

        static BackendSdkSetup()
        {
            EditorApplication.delayCall += ApplyDefinesIfSdkPresent;
        }

        [MenuItem("StockPicker/Backend/Configure SDK Defines")]
        public static void ApplyDefinesIfSdkPresent()
        {
            var hasPlayFab = ResolveType(
                "PlayFab.PlayFabClientAPI, PlayFab",
                "PlayFab.PlayFabClientAPI, PlayFabSDK")
                != null;
            var hasGoogle = ResolveType(
                "Google.GoogleSignIn, GoogleSignIn",
                "Google.GoogleSignIn, GoogleSignIn-1.0.4")
                != null;

            var updated = false;
            for (var i = 0; i < TargetGroups.Length; i++)
            {
                var group = TargetGroups[i];
                if (group == BuildTargetGroup.Unknown)
                    continue;

                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                    .Split(';')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToList();

                updated |= SetDefine(symbols, PlayFabDefine, hasPlayFab);
                updated |= SetDefine(symbols, GoogleDefine, hasGoogle);

                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols));
            }

            if (updated)
                Debug.Log($"[BackendSdkSetup] Updated defines. PlayFab: {hasPlayFab}, Google: {hasGoogle}");
        }

        [MenuItem("StockPicker/Backend/Validate SDK Wiring")]
        public static void ValidateWiring()
        {
            var hasPlayFab = ResolveType("PlayFab.PlayFabClientAPI, PlayFab", "PlayFab.PlayFabClientAPI, PlayFabSDK") != null;
            var hasGoogle = ResolveType("Google.GoogleSignIn, GoogleSignIn", "Google.GoogleSignIn, GoogleSignIn-1.0.4") != null;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            Debug.Log(
                $"[BackendSdkSetup] SDK presence => PlayFab: {hasPlayFab}, Google: {hasGoogle}. Current defines ({EditorUserBuildSettings.selectedBuildTargetGroup}): {defines}");
        }

        private static Type ResolveType(params string[] names)
        {
            for (var i = 0; i < names.Length; i++)
            {
                var t = Type.GetType(names[i]);
                if (t != null) return t;
            }

            return null;
        }

        private static bool SetDefine(System.Collections.Generic.List<string> symbols, string define, bool enabled)
        {
            var has = symbols.Contains(define);
            if (enabled && !has)
            {
                symbols.Add(define);
                return true;
            }

            if (!enabled && has)
            {
                symbols.RemoveAll(s => s == define);
                return true;
            }

            return false;
        }
    }
}
