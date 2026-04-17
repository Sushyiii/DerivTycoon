using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using DerivTycoon.UI;

public static class BuildLoginPanel
{
    [MenuItem("DerivTycoon/Build Login Panel")]
    public static void Build()
    {
        var canvas = GameObject.Find("UICanvas");
        if (canvas == null) { Debug.LogError("UICanvas not found"); return; }

        // Remove existing if present
        var existing = canvas.transform.Find("LoginPanelRoot");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ?????? Root overlay ??????
        var root = MakePanel("LoginPanelRoot", canvas.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.05f, 0.08f, 0.15f, 0.95f));
        var loginUI = root.AddComponent<LoginPanelUI>();

        // ?????? Card ??????
        var card = MakePanel("LoginPanel", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(320, 420),
            new Color(0.1f, 0.15f, 0.25f, 1f));

        // Title
        var title = MakeText("TitleText", card.transform, font,
            "Commodity Mine Tycoon", 22, FontStyle.Bold,
            new Color(1f, 0.85f, 0.2f),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-20, 50), new Vector2(0, -30));

        // Subtitle
        MakeText("SubtitleText", card.transform, font,
            "Trade real commodities with live market data", 14, FontStyle.Normal,
            new Color(0.7f, 0.8f, 0.9f),
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-20, 40), new Vector2(0, -90));

        // Login button
        var loginBtn = MakeButton("LoginButton", card.transform, font,
            "Login with Deriv", 16, new Color(0.85f, 0.15f, 0.15f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(240, 50), new Vector2(0, 40));

        // Demo button
        var demoBtn = MakeButton("DemoButton", card.transform, font,
            "Play Demo (No Login)", 14, new Color(0.2f, 0.35f, 0.55f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(240, 50), new Vector2(0, -30));

        // Status text
        var statusTxt = MakeText("StatusText", card.transform, font,
            "", 12, FontStyle.Normal, new Color(1f, 0.5f, 0.3f),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-20, 40), new Vector2(0, 20));

        // ?????? Loading panel ??????
        var loading = MakePanel("LoadingPanel", root.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.05f, 0.08f, 0.15f, 0.98f));
        MakeText("LoadingText", loading.transform, font,
            "Authenticating...", 20, FontStyle.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(300, 60), Vector2.zero);
        loading.SetActive(false);

        // ?????? Wire references via SerializedObject ??????
        var so = new SerializedObject(loginUI);
        so.FindProperty("loginPanel").objectReferenceValue  = card;
        so.FindProperty("loginButton").objectReferenceValue = loginBtn;
        so.FindProperty("demoButton").objectReferenceValue  = demoBtn;
        so.FindProperty("loadingPanel").objectReferenceValue = loading;
        so.FindProperty("statusText").objectReferenceValue  = statusTxt;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(canvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[BuildLoginPanel] Done");
    }

    static GameObject MakePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        if (anchorMin == anchorMax) // point anchor ??? use sizeDelta/anchoredPos via offsetMin/Max trick
        {
            rt.sizeDelta = offsetMax;
            rt.anchoredPosition = offsetMin;
        }
        else
        {
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        }
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static Text MakeText(string name, Transform parent, Font font,
        string content, int size, FontStyle style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.sizeDelta = sizeDelta; rt.anchoredPosition = anchoredPos;
        var txt = go.AddComponent<Text>();
        txt.text = content; txt.font = font; txt.fontSize = size;
        txt.fontStyle = style; txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
    }

    static Button MakeButton(string name, Transform parent, Font font,
        string label, int fontSize, Color bgColor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.sizeDelta = sizeDelta; rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var txt = labelGO.AddComponent<Text>();
        txt.text = label; txt.font = font; txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;

        return btn;
    }
}
