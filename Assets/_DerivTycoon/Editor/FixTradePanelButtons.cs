using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.Editor
{
    public static class FixTradePanelButtons
    {
        [MenuItem("DerivTycoon/UI/Fix Trade Panel Buttons")]
        public static void Fix()
        {
            var panel = GameObject.Find("TradePanelRoot");
            if (panel == null) { Debug.LogError("TradePanelRoot not found"); return; }

            // Panel is 420x380. Distribute 3 buttons evenly at y=90 (same row)
            // Each button 120x50, centered at x = -140, 0, +140
            LayoutButton(panel, "GoldButton",       -140f, 90f, 120f, 50f, "Gold");
            LayoutButton(panel, "SilverButton",        0f, 90f, 120f, 50f, "Silver");
            LayoutButton(panel, "VolatilityButton",  140f, 90f, 120f, 50f, "Vol 100");

            EditorUtility.SetDirty(panel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[FixTradePanelButtons] Buttons repositioned");
        }

        private static void LayoutButton(GameObject panel, string name,
            float x, float y, float w, float h, string label)
        {
            var t = panel.transform.Find(name);
            if (t == null) { Debug.LogWarning($"Not found: {name}"); return; }

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);

            // Fix label text and font size
            var txt = t.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text      = label;
                txt.fontSize  = 14;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.resizeTextForBestFit = false;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }
    }
}
