using UnityEditor;
using UnityEngine;

namespace DerivTycoon.Editor
{
    public static class UpgradeAssetsToURP
    {
        [MenuItem("DerivTycoon/Upgrade Third-Party Materials to URP")]
        public static void Upgrade()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[UpgradeURP] URP Lit shader not found. Make sure URP is installed.");
                return;
            }

            string[] folders = {
                "Assets/Synty",
                "Assets/Skyden_Games",
                "Assets/SimpleTownLite",
                "Assets/Palmov Island"
            };

            int upgraded = 0;
            string[] matGuids = AssetDatabase.FindAssets("t:Material", folders);

            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Skip if already URP
                if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline")) continue;
                // Skip if already using URP-compatible shader
                if (mat.shader != null && mat.shader.name.Contains("Shader Graphs/")) continue;

                // Extract color before shader swap (Standard uses _Color, URP uses _BaseColor)
                Color col = Color.white;
                if (mat.HasProperty("_Color"))       col = mat.GetColor("_Color");
                else if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");

                float metallic   = mat.HasProperty("_Metallic")   ? mat.GetFloat("_Metallic")   : 0f;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") :
                                   mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness")  : 0.3f;

                // Grab main texture BEFORE switching shader (property names change)
                Texture mainTex = null;
                if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");

                mat.shader = urpLit;

                // Apply properties in URP Lit convention
                if (mat.HasProperty("_BaseColor"))   mat.SetColor("_BaseColor",   col);
                if (mat.HasProperty("_Color"))       mat.SetColor("_Color",       col);
                if (mat.HasProperty("_Metallic"))    mat.SetFloat("_Metallic",    metallic);
                if (mat.HasProperty("_Smoothness"))  mat.SetFloat("_Smoothness",  smoothness);

                // Copy main texture: Standard _MainTex → URP _BaseMap
                if (mainTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                }

                EditorUtility.SetDirty(mat);
                upgraded++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[UpgradeURP] Upgraded {upgraded} materials to URP Lit across {folders.Length} asset folders.");
        }
    }
}
