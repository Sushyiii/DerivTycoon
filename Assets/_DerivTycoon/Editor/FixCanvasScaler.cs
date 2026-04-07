using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.Editor
{
    public static class FixCanvasScaler
    {
        [MenuItem("DerivTycoon/UI/Fix Canvas For Mobile")]
        public static void Fix()
        {
            var canvas = GameObject.Find("UICanvas");
            if (canvas == null) { Debug.LogError("UICanvas not found"); return; }

            // Switch to Scale With Screen Size
            var scaler = canvas.GetComponent<CanvasScaler>();
            var so = new SerializedObject(scaler);
            so.FindProperty("m_UiScaleMode").enumValueIndex = 1; // ScaleWithScreenSize
            so.FindProperty("m_ReferenceResolution").vector2Value = new Vector2(720, 1280);
            so.FindProperty("m_MatchWidthOrHeight").floatValue = 0.5f; // blend width+height
            so.FindProperty("m_ScreenMatchMode").enumValueIndex = 0; // MatchWidthOrHeight
            so.ApplyModifiedProperties();

            // Make TradePanelRoot stretch full width with 20px margin each side
            var tradePanel = GameObject.Find("TradePanelRoot");
            if (tradePanel != null)
            {
                var rt = tradePanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.offsetMin = new Vector2(20f, rt.offsetMin.y);   // left margin
                rt.offsetMax = new Vector2(-20f, rt.offsetMax.y);  // right margin
                rt.sizeDelta = new Vector2(0f, 380f);              // width=stretch, height=380

                // Reposition the 3 buttons to use relative positions within 0..1 anchors
                // Panel is now full-width-40px, redistribute buttons
                LayoutButtonAnchored(tradePanel, "GoldButton",      0f,    0.5f, 0.3f, 50f, "Gold");
                LayoutButtonAnchored(tradePanel, "SilverButton",    0.5f,  0.5f, 0.3f, 50f, "Silver");
                LayoutButtonAnchored(tradePanel, "VolatilityButton",1f,    0.5f, 0.3f, 50f, "Vol 100");

                // SelectedCommodityText and LivePriceText ??? anchor to left/right halves
                SetAnchoredStretch(tradePanel, "SelectedCommodityText", 0f,   0.48f, 0f, -40f);
                SetAnchoredStretch(tradePanel, "LivePriceText",          0.52f, 1f,  0f, -40f);

                // ConfirmButton and CancelButton stretch to halves
                SetAnchoredStretch(tradePanel, "ConfirmButton", 0f,   0.48f, -130f, -80f);
                SetAnchoredStretch(tradePanel, "CancelButton",  0.52f, 1f,  -130f, -80f);
            }

            // Make BuildingInfoPanelRoot also stretch full width
            var infoPanel = GameObject.Find("BuildingInfoPanelRoot");
            if (infoPanel != null)
            {
                var rt = infoPanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.offsetMin = new Vector2(20f, rt.offsetMin.y);
                rt.offsetMax = new Vector2(-20f, rt.offsetMax.y);
                rt.sizeDelta = new Vector2(0f, 420f);
            }

            EditorUtility.SetDirty(canvas);
            if (tradePanel) EditorUtility.SetDirty(tradePanel);
            if (infoPanel)  EditorUtility.SetDirty(infoPanel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[FixCanvasScaler] Canvas configured for mobile (720x1280, ScaleWithScreenSize)");
        }

        // Button that uses anchor x-position (0=left, 0.5=center, 1=right) within parent
        private static void LayoutButtonAnchored(GameObject parent, string name,
            float anchorX, float anchorY, float widthFraction, float height, string label)
        {
            var t = parent.transform.Find(name);
            if (t == null) return;

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, anchorY);
            rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot     = new Vector2(anchorX, 0.5f);
            rt.sizeDelta = new Vector2(0f, height); // width will be set via offset
            // Use stretch: each button takes ~30% with margins
            rt.anchorMin = new Vector2(anchorX == 0f ? 0f : anchorX == 0.5f ? 0.35f : 0.68f, 0.5f);
            rt.anchorMax = new Vector2(anchorX == 0f ? 0.3f : anchorX == 0.5f ? 0.65f : 1f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(4f, -height / 2f);
            rt.offsetMax = new Vector2(-4f, height / 2f);
            rt.anchoredPosition = new Vector2(0f, 90f);

            var txt = t.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text      = label;
                txt.fontSize  = 16;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        private static void SetAnchoredStretch(GameObject parent, string name,
            float anchorXMin, float anchorXMax, float yMin, float yMax)
        {
            var t = parent.transform.Find(name);
            if (t == null) return;

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorXMin, 0.5f);
            rt.anchorMax = new Vector2(anchorXMax, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(8f, yMin);
            rt.offsetMax = new Vector2(-8f, yMax);
        }
    }
}
