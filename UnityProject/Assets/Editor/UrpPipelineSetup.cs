#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StockPicker.Editor
{
    /// <summary>
    /// One-click URP pipeline asset for 2022.3 + assignment to Graphics settings.
    /// </summary>
    public static class UrpPipelineSetup
    {
        private const string RendererPath = "Assets/Settings/URP/ForwardRenderer.asset";
        private const string PipelinePath = "Assets/Settings/URP/UniversalRenderPipelineAsset.asset";

        [MenuItem("StockPicker/Rendering/Create and Assign URP (2022.3)")]
        public static void CreateAndAssign()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder("Assets/Settings/URP"))
                AssetDatabase.CreateFolder("Assets/Settings", "URP");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                list.ClearArray();
                list.InsertArrayElementAtIndex(0);
                list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log($"URP assigned: {PipelinePath}");
        }
    }
}
#endif
