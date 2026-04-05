
using UnityEditor;
using UnityEngine;

namespace DerivTycoon.Editor
{
    public static class MovePrefabsToResources
    {
        [MenuItem("DerivTycoon/Build Prefabs/Move Prefabs to Resources")]
        public static void MovePrefabs()
        {
            string src = "Assets/_DerivTycoon/Prefabs/Buildings";
            string dst = "Assets/_DerivTycoon/Resources/Buildings";

            // Ensure destination exists
            if (!AssetDatabase.IsValidFolder("Assets/_DerivTycoon/Resources"))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon", "Resources");
            if (!AssetDatabase.IsValidFolder(dst))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon/Resources", "Buildings");

            string[] prefabNames = {
                "GoldMinePrefab",
                "SilverMintPrefab",
                "PlatinumForgePrefab",
                "PalladiumRefineryPrefab",
                "TradingTowerPrefab"
            };

            foreach (var name in prefabNames)
            {
                string srcPath = $"{src}/{name}.prefab";
                string dstPath = $"{dst}/{name}.prefab";

                if (!AssetDatabase.LoadAssetAtPath<GameObject>(srcPath))
                {
                    Debug.LogWarning($"[MovePrefabs] Not found: {srcPath}");
                    continue;
                }

                var err = AssetDatabase.MoveAsset(srcPath, dstPath);
                if (string.IsNullOrEmpty(err))
                    Debug.Log($"[MovePrefabs] Moved {name} ??? Resources/Buildings/");
                else
                    Debug.LogError($"[MovePrefabs] Failed to move {name}: {err}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MovePrefabs] Done.");
        }
    }
}
