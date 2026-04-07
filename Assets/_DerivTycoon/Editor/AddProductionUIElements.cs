using DerivTycoon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.Editor
{
    public static class AddProductionUIElements
    {
        [MenuItem("DerivTycoon/UI/Add Production UI Elements")]
        public static void AddElements()
        {
            var panel = GameObject.Find("BuildingInfoPanelRoot");
            if (panel == null)
            {
                Debug.LogError("[AddProductionUI] BuildingInfoPanelRoot not found in scene");
                return;
            }

            var buildingInfoUI = panel.GetComponent<BuildingInfoUI>();
            if (buildingInfoUI == null)
            {
                Debug.LogError("[AddProductionUI] BuildingInfoUI component not found");
                return;
            }

            // Move SellButton down to make room
            var sellBtn = panel.transform.Find("SellButton");
            if (sellBtn != null)
            {
                var rt = sellBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -110f);
            }

            // CountdownText
            var countdownText = CreateText(panel.transform, "CountdownText",
                new Vector2(0, -60f), new Vector2(220f, 22f), "Production: OFF", 13, Color.gray);

            // VaultText
            var vaultText = CreateText(panel.transform, "VaultText",
                new Vector2(0, -82f), new Vector2(220f, 20f), "Vault: $0.00", 13, new Color(1f, 0.85f, 0.2f));

            // WinStreakText
            var winStreakText = CreateText(panel.transform, "WinStreakText",
                new Vector2(0, -102f), new Vector2(220f, 18f), "Cycles: 0", 12, new Color(0.7f, 0.7f, 0.7f));

            // ToggleProductionButton
            var toggleBtn = CreateButton(panel.transform, "ToggleProductionButton",
                new Vector2(0, -82f), new Vector2(190f, 30f), "Start Production",
                new Color(0.1f, 0.5f, 0.25f));

            // Reorder: shift the above elements to fit between PnLText and SellButton
            // PnLText is around y=-32 in anchored space, SellButton now at y=-110
            // Let's place: countdown at -38, vault at -56, winstreak at -72, toggle at -91
            SetAnchored(countdownText.GetComponent<RectTransform>(), new Vector2(0, -38f));
            SetAnchored(vaultText.GetComponent<RectTransform>(), new Vector2(0, -57f));
            SetAnchored(winStreakText.GetComponent<RectTransform>(), new Vector2(0, -74f));
            SetAnchored(toggleBtn.GetComponent<RectTransform>(), new Vector2(0, -93f));

            // Wire references
            var so = new SerializedObject(buildingInfoUI);
            so.FindProperty("CountdownText").objectReferenceValue = countdownText.GetComponent<Text>();
            so.FindProperty("VaultText").objectReferenceValue = vaultText.GetComponent<Text>();
            so.FindProperty("WinStreakText").objectReferenceValue = winStreakText.GetComponent<Text>();
            so.FindProperty("ToggleProductionButton").objectReferenceValue = toggleBtn.GetComponent<Button>();
            so.FindProperty("ToggleProductionButtonText").objectReferenceValue =
                toggleBtn.transform.Find("Text")?.GetComponent<Text>();
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[AddProductionUI] Production UI elements added and wired successfully");
        }

        private static void SetAnchored(RectTransform rt, Vector2 pos)
        {
            rt.anchoredPosition = pos;
        }

        private static GameObject CreateText(Transform parent, string name, Vector2 anchoredPos,
            Vector2 size, string text, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            go.AddComponent<CanvasRenderer>();
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, Vector2 anchoredPos,
            Vector2 size, string label, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor      = bgColor;
            cb.highlightedColor = bgColor * 1.3f;
            cb.pressedColor     = bgColor * 0.7f;
            btn.colors = cb;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            // Label
            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.AddComponent<CanvasRenderer>();
            var txt = labelGo.AddComponent<Text>();
            txt.text      = label;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 13;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            return go;
        }
    }
}
