using DerivTycoon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.Editor
{
    /// <summary>
    /// Rebuilds the BuildingInfoPanel layout from scratch with correct vertical ordering.
    /// Run: DerivTycoon/UI/Fix Building Info Layout
    /// </summary>
    public static class FixBuildingInfoLayout
    {
        [MenuItem("DerivTycoon/UI/Fix Building Info Layout")]
        public static void Fix()
        {
            var panel = GameObject.Find("BuildingInfoPanelRoot");
            if (panel == null) { Debug.LogError("BuildingInfoPanelRoot not found"); return; }

            var ui = panel.GetComponent<BuildingInfoUI>();
            if (ui == null) { Debug.LogError("BuildingInfoUI not found"); return; }

            // Resize panel to 260x420 to fit all elements
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(260f, 420f);

            // Fix Close button ??? top-right corner, "X" label
            FixCloseButton(panel);

            // Reposition all elements top-to-bottom (anchored to top-center)
            // y values are negative = down from top edge
            SetAnchored(panel, "BuildingNameText",    0f,  -30f, 220f, 28f);
            SetAnchored(panel, "SymbolText",          0f,  -58f, 220f, 20f);
            SetAnchored(panel, "StakeText",           0f,  -78f, 220f, 20f);
            SetAnchored(panel, "EntryPriceText",      0f, -104f, 220f, 20f);
            SetAnchored(panel, "CurrentPriceText",    0f, -124f, 220f, 20f);
            SetAnchored(panel, "PnLText",             0f, -150f, 220f, 24f);

            // Separator line (production section)
            EnsureSeparator(panel, -180f);

            SetAnchored(panel, "CountdownText",       0f, -196f, 220f, 20f);
            SetAnchored(panel, "VaultText",           0f, -216f, 220f, 20f);
            SetAnchored(panel, "WinStreakText",       0f, -236f, 220f, 18f);
            SetAnchored(panel, "ToggleProductionButton", 0f, -263f, 200f, 32f);
            SetAnchored(panel, "SellButton",          0f, -312f, 200f, 36f);

            // Fix font sizes for clarity
            SetFontSize(panel, "BuildingNameText",  17, FontStyle.Bold,  new Color(1f, 0.85f, 0.25f));
            SetFontSize(panel, "SymbolText",        12, FontStyle.Normal, new Color(0.6f, 0.6f, 0.6f));
            SetFontSize(panel, "StakeText",         12, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            SetFontSize(panel, "EntryPriceText",    12, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            SetFontSize(panel, "CurrentPriceText",  12, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            SetFontSize(panel, "PnLText",           14, FontStyle.Bold,   Color.white);
            SetFontSize(panel, "CountdownText",     12, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f));
            SetFontSize(panel, "VaultText",         13, FontStyle.Bold,   new Color(1f, 0.85f, 0.2f));
            SetFontSize(panel, "WinStreakText",     11, FontStyle.Normal, new Color(0.6f, 0.6f, 0.6f));

            EditorUtility.SetDirty(panel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[FixBuildingInfoLayout] Layout fixed successfully");
        }

        private static void FixCloseButton(GameObject panel)
        {
            var closeGo = panel.transform.Find("CloseButton");
            if (closeGo == null) return;

            var rt = closeGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(30f, 30f);
            rt.anchoredPosition = new Vector2(-4f, -4f);

            // Fix label to "X"
            var txt = closeGo.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text      = "X";
                txt.fontSize  = 14;
                txt.fontStyle = FontStyle.Bold;
                txt.color     = Color.white;
            }

            var img = closeGo.GetComponent<Image>();
            if (img != null) img.color = new Color(0.5f, 0.1f, 0.1f);
        }

        private static void SetAnchored(GameObject panel, string childName,
            float x, float y, float w, float h)
        {
            var t = panel.transform.Find(childName);
            if (t == null) { Debug.LogWarning($"[FixLayout] Child not found: {childName}"); return; }

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);

            // Fix button colors for Sell and Toggle
            if (childName == "SellButton")
            {
                var img = t.GetComponent<Image>();
                if (img != null) img.color = new Color(0.75f, 0.15f, 0.1f);
            }
            if (childName == "ToggleProductionButton")
            {
                var img = t.GetComponent<Image>();
                if (img != null) img.color = new Color(0.1f, 0.45f, 0.2f);
            }
        }

        private static void SetFontSize(GameObject panel, string childName,
            int size, FontStyle style, Color color)
        {
            var t = panel.transform.Find(childName);
            if (t == null) return;
            var txt = t.GetComponent<Text>();
            if (txt == null) txt = t.GetComponentInChildren<Text>();
            if (txt == null) return;
            txt.fontSize  = size;
            txt.fontStyle = style;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private static void EnsureSeparator(GameObject panel, float y)
        {
            var existing = panel.transform.Find("ProductionSeparator");
            if (existing != null) return;

            var go = new GameObject("ProductionSeparator");
            go.transform.SetParent(panel.transform, false);
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(220f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
        }
    }
}
