#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class BuildPixelBuilding3
{
    public static void Execute()
    {
        // ── Materials ──────────────────────────────────────────────
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");

        // Teal / green wall
        Material wallMat = new Material(lit);
        wallMat.color = new Color(0.35f, 0.55f, 0.45f); // muted teal-green
        AssetDatabase.CreateAsset(wallMat, "Assets/Materials/PixelBrick3.mat");

        // Dark green roof
        Material roofMat = new Material(lit);
        roofMat.color = new Color(0.15f, 0.30f, 0.20f);
        AssetDatabase.CreateAsset(roofMat, "Assets/Materials/PixelRoof3.mat");

        // Cream / off-white trim
        Material trimMat = new Material(lit);
        trimMat.color = new Color(0.90f, 0.88f, 0.80f);
        AssetDatabase.CreateAsset(trimMat, "Assets/Materials/PixelTrim3.mat");

        // Yellow door
        Material doorMat = new Material(lit);
        doorMat.color = new Color(0.85f, 0.70f, 0.25f);
        AssetDatabase.CreateAsset(doorMat, "Assets/Materials/PixelDoor3.mat");

        // Light blue window (reuse concept)
        Material windowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PixelWindow.mat");
        if (windowMat == null)
        {
            windowMat = new Material(lit);
            windowMat.color = new Color(0.6f, 0.85f, 0.9f);
            windowMat.SetFloat("_Smoothness", 0.8f);
            AssetDatabase.CreateAsset(windowMat, "Assets/Materials/PixelWindow3.mat");
        }

        // Chimney (reuse)
        Material chimneyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PixelChimney.mat");
        if (chimneyMat == null)
        {
            chimneyMat = new Material(lit);
            chimneyMat.color = new Color(0.35f, 0.22f, 0.18f);
            AssetDatabase.CreateAsset(chimneyMat, "Assets/Materials/PixelChimney3.mat");
        }

        // Porch (reuse)
        Material porchMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PixelPorch.mat");
        if (porchMat == null)
        {
            porchMat = new Material(lit);
            porchMat.color = new Color(0.55f, 0.50f, 0.45f);
            AssetDatabase.CreateAsset(porchMat, "Assets/Materials/PixelPorch3.mat");
        }

        // Awning – warm orange-red
        Material awningMat = new Material(lit);
        awningMat.color = new Color(0.80f, 0.35f, 0.20f);
        AssetDatabase.CreateAsset(awningMat, "Assets/Materials/PixelAwning3.mat");

        AssetDatabase.SaveAssets();

        // ── Root ───────────────────────────────────────────────────
        // Place to the right of PixelBuilding2 (which is at x≈7)
        GameObject root = new GameObject("PixelBuilding3");
        root.transform.position = new Vector3(14f, 0f, 0f);

        // Helper
        System.Func<string, Vector3, Vector3, Material, GameObject> MakeCube =
            (string name, Vector3 localPos, Vector3 scale, Material mat) =>
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(root.transform);
                go.transform.localPosition = localPos;
                go.transform.localScale = scale;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                return go;
            };

        // ── Body – wider, shorter shop-style ──────────────────────
        // 4 wide x 3.2 tall x 3 deep
        MakeCube("B3_Body",
            new Vector3(0f, 1.6f, 0f),
            new Vector3(4.0f, 3.2f, 3.0f),
            wallMat);

        // ── Roof slab ─────────────────────────────────────────────
        MakeCube("B3_Roof",
            new Vector3(0f, 3.45f, 0f),
            new Vector3(4.4f, 0.5f, 3.4f),
            roofMat);

        // Roof top (flat cap)
        MakeCube("B3_RoofTop",
            new Vector3(0f, 3.80f, 0f),
            new Vector3(4.0f, 0.15f, 3.0f),
            roofMat);

        // Roof edge / overhang trim
        MakeCube("B3_RoofEdge",
            new Vector3(0f, 3.25f, 1.55f),
            new Vector3(4.2f, 0.10f, 0.15f),
            trimMat);

        // ── Foundation trim ───────────────────────────────────────
        MakeCube("B3_FoundationTrim",
            new Vector3(0f, -0.05f, 0f),
            new Vector3(4.2f, 0.10f, 3.2f),
            trimMat);

        // ── Storefront window (large) ─────────────────────────────
        MakeCube("B3_StorefrontWindow",
            new Vector3(-0.8f, 1.1f, 1.51f),
            new Vector3(1.6f, 1.4f, 0.05f),
            windowMat);

        // Storefront window frame
        MakeCube("B3_StorefrontFrame",
            new Vector3(-0.8f, 1.1f, 1.52f),
            new Vector3(1.75f, 1.55f, 0.03f),
            trimMat);

        // ── Upper windows (row of 3) ─────────────────────────────
        float windowY = 2.5f;
        float[] windowXPositions = { -1.2f, 0f, 1.2f };
        string[] windowNames = { "B3_WindowLeft", "B3_WindowCenter", "B3_WindowRight" };
        string[] frameNames = { "B3_WindowFrameLeft", "B3_WindowFrameCenter", "B3_WindowFrameRight" };
        string[] sillNames = { "B3_WindowSillLeft", "B3_WindowSillCenter", "B3_WindowSillRight" };

        for (int i = 0; i < 3; i++)
        {
            MakeCube(windowNames[i],
                new Vector3(windowXPositions[i], windowY, 1.51f),
                new Vector3(0.55f, 0.55f, 0.05f),
                windowMat);

            MakeCube(frameNames[i],
                new Vector3(windowXPositions[i], windowY, 1.52f),
                new Vector3(0.70f, 0.70f, 0.03f),
                trimMat);

            MakeCube(sillNames[i],
                new Vector3(windowXPositions[i], windowY - 0.35f, 1.55f),
                new Vector3(0.70f, 0.08f, 0.10f),
                trimMat);
        }

        // ── Door ──────────────────────────────────────────────────
        MakeCube("B3_Door",
            new Vector3(1.2f, 0.65f, 1.51f),
            new Vector3(0.8f, 1.3f, 0.05f),
            doorMat);

        MakeCube("B3_DoorFrame",
            new Vector3(1.2f, 0.70f, 1.52f),
            new Vector3(0.95f, 1.45f, 0.03f),
            trimMat);

        // ── Awning over storefront ────────────────────────────────
        MakeCube("B3_Awning",
            new Vector3(-0.8f, 1.90f, 1.90f),
            new Vector3(1.9f, 0.08f, 0.80f),
            awningMat);

        // Awning front edge (thicker strip)
        MakeCube("B3_AwningEdge",
            new Vector3(-0.8f, 1.87f, 2.28f),
            new Vector3(1.9f, 0.14f, 0.08f),
            awningMat);

        // ── Chimney ───────────────────────────────────────────────
        MakeCube("B3_Chimney",
            new Vector3(1.4f, 4.5f, 0f),
            new Vector3(0.5f, 1.0f, 0.5f),
            chimneyMat);

        MakeCube("B3_ChimneyTop",
            new Vector3(1.4f, 5.05f, 0f),
            new Vector3(0.65f, 0.12f, 0.65f),
            chimneyMat);

        // ── Porch ─────────────────────────────────────────────────
        MakeCube("B3_PorchBase",
            new Vector3(0f, 0.05f, 2.0f),
            new Vector3(2.8f, 0.10f, 1.0f),
            porchMat);

        MakeCube("B3_PorchStep",
            new Vector3(0f, -0.05f, 2.55f),
            new Vector3(2.8f, 0.10f, 0.30f),
            porchMat);

        // ── Side window (left wall) ──────────────────────────────
        MakeCube("B3_SideWindowLeft",
            new Vector3(-2.01f, 1.8f, 0f),
            new Vector3(0.05f, 0.6f, 0.6f),
            windowMat);

        MakeCube("B3_SideWindowFrameLeft",
            new Vector3(-2.02f, 1.8f, 0f),
            new Vector3(0.03f, 0.75f, 0.75f),
            trimMat);

        // ── Finalize ──────────────────────────────────────────────
        Undo.RegisterCreatedObjectUndo(root, "Create PixelBuilding3");
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("PixelBuilding3 created successfully!");
    }
}
#endif
