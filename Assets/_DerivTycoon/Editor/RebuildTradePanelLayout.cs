using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.Editor
{
    /// <summary>
    /// Rebuilds TradePanelRoot and HUDPanel layout for ScaleWithScreenSize 720x1280 reference.
    /// </summary>
    public static class RebuildTradePanelLayout
    {
        [MenuItem("DerivTycoon/UI/Rebuild UI For 720x1280")]
        public static void Rebuild()
        {
            var canvas = GameObject.Find("UICanvas");
            if (canvas == null) { Debug.LogError("UICanvas not found"); return; }

            // 1. Set CanvasScaler
            var scaler = canvas.GetComponent<CanvasScaler>();
            var scalerSo = new SerializedObject(scaler);
            scalerSo.FindProperty("m_UiScaleMode").enumValueIndex = 1; // ScaleWithScreenSize
            scalerSo.FindProperty("m_ReferenceResolution").vector2Value = new Vector2(720, 1280);
            scalerSo.FindProperty("m_MatchWidthOrHeight").floatValue = 0.5f;
            scalerSo.ApplyModifiedProperties();

            // 2. Rebuild HUDPanel ??? anchored to top, full width, 60px tall
            RebuildHUD(canvas);

            // 3. Rebuild TradePanelRoot
            RebuildTradePanel(canvas);

            // 4. Rebuild BuildingInfoPanelRoot
            RebuildBuildingInfoPanel(canvas);

            EditorUtility.SetDirty(canvas);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[RebuildUI] Done ??? reference 720x1280");
        }

        private static void RebuildHUD(GameObject canvas)
        {
            var hud = canvas.transform.Find("HUDPanel");
            if (hud == null) return;

            var rt = hud.GetComponent<RectTransform>();
            // Stretch full width, anchor to top
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -70f);
            rt.offsetMax = new Vector2(0f, 0f);

            // Balance text ??? left side
            var balance = hud.Find("BalanceText");
            if (balance != null) SetStretchChild(balance, 0f, 0.5f, 0f, 0f, 18, FontStyle.Bold, Color.white);

            // New Trade button ??? right side
            var newTrade = hud.Find("NewTradeButton");
            if (newTrade != null)
            {
                var btnRt = newTrade.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.5f, 0f);
                btnRt.anchorMax = new Vector2(1f, 1f);
                btnRt.offsetMin = new Vector2(4f, 8f);
                btnRt.offsetMax = new Vector2(-8f, -8f);
                var txt = newTrade.GetComponentInChildren<Text>();
                if (txt != null) { txt.fontSize = 18; txt.alignment = TextAnchor.MiddleCenter; }
            }
        }

        private static void RebuildTradePanel(GameObject canvas)
        {
            var panel = canvas.transform.Find("TradePanelRoot");
            if (panel == null) return;

            // Panel: stretch full width, centered vertically, 420px tall
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0f, -210f); // -half height
            rt.offsetMax = new Vector2(0f,  210f); // +half height

            // Title ??? top area
            PlaceChild(panel, "TitleText",  0.5f, 1f, 0f, -50f, 360f, 40f, 20, FontStyle.Bold, Color.white);

            // 3 commodity buttons in a row ??? y=-110 from top (anchoredPos from top anchor)
            PlaceButtonRow(panel, "GoldButton",       0f,    0.33f, -105f, -155f, "Gold");
            PlaceButtonRow(panel, "SilverButton",     0.33f, 0.67f, -105f, -155f, "Silver");
            PlaceButtonRow(panel, "VolatilityButton", 0.67f, 1f,    -105f, -155f, "Vol 100");

            // Selected commodity name ??? left half
            PlaceChild(panel, "SelectedCommodityText", 0.1f, 0.5f, -175f, -215f, 0f, 0f, 16, FontStyle.Bold, Color.white);

            // Live price ??? right half
            PlaceChild(panel, "LivePriceText", 0.5f, 0.9f, -175f, -215f, 0f, 0f, 16, FontStyle.Normal, new Color(0.2f, 0.9f, 0.3f));

            // Place Building + Cancel buttons at bottom
            PlaceButtonRow(panel, "ConfirmButton", 0f,   0.48f, -340f, -400f, "Place Building");
            PlaceButtonRow(panel, "CancelButton",  0.52f, 1f,   -340f, -400f, "Cancel");
        }

        private static void RebuildBuildingInfoPanel(GameObject canvas)
        {
            var panel = canvas.transform.Find("BuildingInfoPanelRoot");
            if (panel == null) return;

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(20f, -230f);
            rt.offsetMax = new Vector2(-20f, 230f);

            // All elements use top-center anchors with negative y offsets from top
            PlaceInfoText(panel, "BuildingNameText",   0.5f, 1f,  -35f,  -68f, 20, FontStyle.Bold,   new Color(1f, 0.85f, 0.25f));
            PlaceInfoText(panel, "SymbolText",         0.5f, 1f,  -72f,  -96f, 14, FontStyle.Normal, new Color(0.55f, 0.55f, 0.55f));
            PlaceInfoText(panel, "StakeText",          0.5f, 1f,  -98f, -122f, 14, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            PlaceInfoText(panel, "EntryPriceText",     0.5f, 1f, -126f, -150f, 14, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            PlaceInfoText(panel, "CurrentPriceText",   0.5f, 1f, -152f, -176f, 14, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            PlaceInfoText(panel, "PnLText",            0.5f, 1f, -180f, -210f, 17, FontStyle.Bold,   Color.white);

            // Separator
            PlaceSeparator(panel, -216f);

            PlaceInfoText(panel, "CountdownText",      0.5f, 1f, -222f, -244f, 14, FontStyle.Normal, new Color(0.65f, 0.65f, 0.65f));
            PlaceInfoText(panel, "VaultText",          0.5f, 1f, -248f, -270f, 15, FontStyle.Bold,   new Color(1f, 0.85f, 0.2f));
            PlaceInfoText(panel, "WinStreakText",       0.5f, 1f, -273f, -293f, 13, FontStyle.Normal, new Color(0.6f, 0.6f, 0.6f));

            PlaceButtonFull(panel, "ToggleProductionButton", -300f, -340f, new Color(0.1f, 0.45f, 0.2f), 16);
            PlaceButtonFull(panel, "SellButton",             -350f, -395f, new Color(0.7f, 0.12f, 0.08f), 18);

            // Close button ??? top right
            var closeT = panel.Find("CloseButton");
            if (closeT != null)
            {
                var crt = closeT.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(1f, 1f);
                crt.anchorMax = new Vector2(1f, 1f);
                crt.pivot     = new Vector2(1f, 1f);
                crt.sizeDelta = new Vector2(44f, 44f);
                crt.anchoredPosition = new Vector2(-4f, -4f);
                var txt = closeT.GetComponentInChildren<Text>();
                if (txt != null) { txt.text = "X"; txt.fontSize = 16; txt.fontStyle = FontStyle.Bold; txt.color = Color.white; }
                var img = closeT.GetComponent<Image>();
                if (img != null) img.color = new Color(0.45f, 0.08f, 0.08f);
            }
        }

        // ====== Helpers ======

        private static void SetStretchChild(Transform t, float xMin, float xMax,
            float yOffMin, float yOffMax, int fontSize, FontStyle style, Color color)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(8f, yOffMin);
            rt.offsetMax = new Vector2(-8f, yOffMax);
            var txt = t.GetComponent<Text>() ?? t.GetComponentInChildren<Text>();
            if (txt != null) { txt.fontSize = fontSize; txt.fontStyle = style; txt.color = color; txt.alignment = TextAnchor.MiddleCenter; }
        }

        // Button spanning anchor range, yTop/yBottom from top of parent
        private static void PlaceButtonRow(Transform panel, string name,
            float xMin, float xMax, float yTop, float yBot, string label)
        {
            var t = panel.Find(name);
            if (t == null) return;
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 1f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(4f,  yBot);
            rt.offsetMax = new Vector2(-4f, yTop);
            var txt = t.GetComponentInChildren<Text>();
            if (txt != null) { if (!string.IsNullOrEmpty(label)) txt.text = label; txt.fontSize = 16; txt.alignment = TextAnchor.MiddleCenter; }
        }

        private static void PlaceChild(Transform panel, string name,
            float xMin, float xMax, float yTop, float yBot,
            float extraW, float extraH, int fontSize, FontStyle style, Color color)
        {
            var t = panel.Find(name);
            if (t == null) return;
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 1f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(4f,  yBot);
            rt.offsetMax = new Vector2(-4f, yTop);
            var txt = t.GetComponent<Text>() ?? t.GetComponentInChildren<Text>();
            if (txt != null) { txt.fontSize = fontSize; txt.fontStyle = style; txt.color = color; txt.alignment = TextAnchor.MiddleCenter; }
        }

        private static void PlaceInfoText(Transform panel, string name,
            float xAnchor, float yAnchor, float yTop, float yBot,
            int fontSize, FontStyle style, Color color)
        {
            var t = panel.Find(name);
            if (t == null) return;
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(10f, yBot);
            rt.offsetMax = new Vector2(-10f, yTop);
            var txt = t.GetComponent<Text>() ?? t.GetComponentInChildren<Text>();
            if (txt != null) { txt.fontSize = fontSize; txt.fontStyle = style; txt.color = color; txt.alignment = TextAnchor.MiddleCenter; }
        }

        private static void PlaceButtonFull(Transform panel, string name,
            float yTop, float yBot, Color bgColor, int fontSize)
        {
            var t = panel.Find(name);
            if (t == null) return;
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(20f, yBot);
            rt.offsetMax = new Vector2(-20f, yTop);
            var img = t.GetComponent<Image>();
            if (img != null) img.color = bgColor;
            var txt = t.GetComponentInChildren<Text>();
            if (txt != null) { txt.fontSize = fontSize; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; }
        }

        private static void PlaceSeparator(Transform panel, float yTop)
        {
            var existing = panel.Find("ProductionSeparator");
            if (existing == null)
            {
                var go = new GameObject("ProductionSeparator");
                go.transform.SetParent(panel, false);
                go.AddComponent<CanvasRenderer>();
                go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                existing = go.transform;
            }
            var rt = existing.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(20f, yTop - 1f);
            rt.offsetMax = new Vector2(-20f, yTop);
        }
    }
}
