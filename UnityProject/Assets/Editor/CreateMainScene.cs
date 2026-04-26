#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StockPicker.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StockPicker.Editor
{
    public static class CreateMainScene
    {
        [MenuItem("StockPicker/Setup Main Scene")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("StockPicker");
            go.AddComponent<StockPickerGameRoot>();

            var scenesDir = Path.Combine(Application.dataPath, "Scenes");
            if (!Directory.Exists(scenesDir))
                Directory.CreateDirectory(scenesDir);

            const string path = "Assets/Scenes/Main.unity";
            EditorSceneManager.SaveScene(scene, path);

            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Any(s => s.path == path))
                list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();

            AssetDatabase.Refresh();
            Debug.Log($"Saved {path} and enabled it in Build Settings.");
        }
    }
}
#endif
