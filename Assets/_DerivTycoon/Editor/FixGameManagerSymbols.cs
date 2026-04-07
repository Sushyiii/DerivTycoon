using DerivTycoon.Core;
using UnityEditor;
using UnityEngine;

namespace DerivTycoon.Editor
{
    public static class FixGameManagerSymbols
    {
        [MenuItem("DerivTycoon/Fix GameManager Symbols (3 commodities)")]
        public static void Fix()
        {
            var gm = Object.FindObjectOfType<GameManager>();
            if (gm == null)
            {
                Debug.LogError("[FixSymbols] GameManager not found in scene");
                return;
            }

            var so = new SerializedObject(gm);
            var prop = so.FindProperty("commoditySymbols");

            prop.arraySize = 3;
            prop.GetArrayElementAtIndex(0).stringValue = "frxXAUUSD";
            prop.GetArrayElementAtIndex(1).stringValue = "frxXAGUSD";
            prop.GetArrayElementAtIndex(2).stringValue = "1HZ100V";

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[FixSymbols] GameManager commoditySymbols updated to 3 symbols");
        }
    }
}
