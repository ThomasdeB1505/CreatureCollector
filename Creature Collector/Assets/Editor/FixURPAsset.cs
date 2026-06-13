using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FixURPAsset
{
    [MenuItem("Tools/Force Upgrade Quibli URP Asset")]
    static void Upgrade()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
            "Assets/Quibli/Example URP Settings/Quibli URP Config.asset");

        if (asset == null) { Debug.LogError("Asset not found!"); return; }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        AssetDatabase.ForceReserializeAssets(
            new[] { "Assets/Quibli/Example URP Settings/Quibli URP Config.asset" },
            ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);

        Debug.Log("Done — try building again.");
    }
}