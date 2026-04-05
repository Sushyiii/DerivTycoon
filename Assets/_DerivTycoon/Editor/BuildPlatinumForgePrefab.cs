
using UnityEngine;
using UnityEditor;

namespace DerivTycoon.Editor
{
    public static class BuildPlatinumForgePrefab
    {
        static Material s_BaseMat;
        static Material MakeMat(Color col, float metallic = 0f, float smoothness = 0.3f)
        {
            if (s_BaseMat == null)
                s_BaseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WallMat.mat");
            var mat = new Material(s_BaseMat);
            mat.color = col;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        static GameObject AddPrim(PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 localScale, Color col,
            float metallic = 0f, float smoothness = 0.3f, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = MakeMat(col, metallic, smoothness);
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            return go;
        }

        [MenuItem("DerivTycoon/Build Prefabs/Platinum Forge")]
        public static void BuildPlatinumForge()
        {
            var cConcrete  = new Color(0.55f, 0.54f, 0.52f);
            var cDarkMetal = new Color(0.28f, 0.27f, 0.26f);
            var cMetal     = new Color(0.52f, 0.54f, 0.58f);
            var cPlatinum  = new Color(0.88f, 0.92f, 0.96f);
            var cBrick     = new Color(0.48f, 0.38f, 0.30f);
            var cEmber     = new Color(1.00f, 0.40f, 0.05f);
            var cHeat      = new Color(1.00f, 0.70f, 0.10f);
            var cSoot      = new Color(0.18f, 0.16f, 0.14f);
            var cPipe      = new Color(0.42f, 0.44f, 0.46f);
            var cWarning   = new Color(0.90f, 0.65f, 0.05f);

            var root = new GameObject("PlatinumForgePrefab");
            root.transform.position = new Vector3(16, 0, 0);
            var t = root.transform;

            AddPrim(PrimitiveType.Cube, "Foundation", t,
                new Vector3(0, 0.08f, 0), new Vector3(1.88f, 0.16f, 1.88f), cConcrete, 0, 0.2f);
            AddPrim(PrimitiveType.Cube, "ForgeBody_Main", t,
                new Vector3(0, 0.78f, 0.05f), new Vector3(1.55f, 1.12f, 1.50f), cConcrete, 0, 0.2f);

            AddPrim(PrimitiveType.Cube, "Strap_H1", t,
                new Vector3(0, 0.55f, -0.74f), new Vector3(1.56f, 0.08f, 0.04f), cDarkMetal, 0.5f, 0.4f);
            AddPrim(PrimitiveType.Cube, "Strap_H2", t,
                new Vector3(0, 1.00f, -0.74f), new Vector3(1.56f, 0.08f, 0.04f), cDarkMetal, 0.5f, 0.4f);
            AddPrim(PrimitiveType.Cube, "Strap_V1", t,
                new Vector3(-0.55f, 0.78f, -0.74f), new Vector3(0.08f, 1.14f, 0.04f), cDarkMetal, 0.5f, 0.4f);
            AddPrim(PrimitiveType.Cube, "Strap_V2", t,
                new Vector3( 0.55f, 0.78f, -0.74f), new Vector3(0.08f, 1.14f, 0.04f), cDarkMetal, 0.5f, 0.4f);

            AddPrim(PrimitiveType.Cube, "Furnace_L", t,
                new Vector3(-0.32f, 0.62f, -0.74f), new Vector3(0.18f, 1.00f, 0.08f), cBrick, 0, 0.2f);
            AddPrim(PrimitiveType.Cube, "Furnace_R", t,
                new Vector3( 0.32f, 0.62f, -0.74f), new Vector3(0.18f, 1.00f, 0.08f), cBrick, 0, 0.2f);
            AddPrim(PrimitiveType.Cube, "Furnace_Top", t,
                new Vector3(0, 1.18f, -0.74f), new Vector3(0.82f, 0.22f, 0.08f), cBrick, 0, 0.2f);
            AddPrim(PrimitiveType.Cube, "Furnace_Glow", t,
                new Vector3(0, 0.60f, -0.73f), new Vector3(0.40f, 0.80f, 0.04f), cEmber, 0, 0.9f);
            AddPrim(PrimitiveType.Cube, "Furnace_Core", t,
                new Vector3(0, 0.62f, -0.72f), new Vector3(0.20f, 0.45f, 0.04f), cHeat, 0, 0.95f);
            AddPrim(PrimitiveType.Cube, "Furnace_Door", t,
                new Vector3(-0.26f, 0.55f, -0.78f), new Vector3(0.25f, 0.55f, 0.06f),
                cDarkMetal, 0.6f, 0.5f, new Vector3(0, -25f, 0));

            AddPrim(PrimitiveType.Cube, "Roof_Main", t,
                new Vector3(0, 1.46f, 0.05f), new Vector3(1.58f, 0.22f, 1.52f), cDarkMetal, 0.4f, 0.3f);
            AddPrim(PrimitiveType.Cube, "Skylight_L", t,
                new Vector3(-0.38f, 1.58f, 0), new Vector3(0.30f, 0.06f, 0.50f),
                new Color(0.5f, 0.7f, 0.9f), 0, 0.9f);
            AddPrim(PrimitiveType.Cube, "Skylight_R", t,
                new Vector3( 0.38f, 1.58f, 0), new Vector3(0.30f, 0.06f, 0.50f),
                new Color(0.5f, 0.7f, 0.9f), 0, 0.9f);

            AddPrim(PrimitiveType.Cylinder, "Chimney_L_Stack", t,
                new Vector3(-0.45f, 2.10f, 0.40f), new Vector3(0.26f, 1.40f, 0.26f), cSoot, 0, 0.2f);
            AddPrim(PrimitiveType.Cylinder, "Chimney_L_Band1", t,
                new Vector3(-0.45f, 1.80f, 0.40f), new Vector3(0.32f, 0.06f, 0.32f), cWarning, 0, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Chimney_L_Band2", t,
                new Vector3(-0.45f, 2.50f, 0.40f), new Vector3(0.32f, 0.06f, 0.32f), cWarning, 0, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Chimney_L_Cap", t,
                new Vector3(-0.45f, 2.88f, 0.40f), new Vector3(0.34f, 0.08f, 0.34f), cDarkMetal, 0.5f, 0.4f);

            AddPrim(PrimitiveType.Cylinder, "Chimney_R_Stack", t,
                new Vector3( 0.45f, 1.92f, 0.40f), new Vector3(0.22f, 1.15f, 0.22f), cSoot, 0, 0.2f);
            AddPrim(PrimitiveType.Cylinder, "Chimney_R_Band", t,
                new Vector3( 0.45f, 1.65f, 0.40f), new Vector3(0.28f, 0.06f, 0.28f), cWarning, 0, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Chimney_R_Cap", t,
                new Vector3( 0.45f, 2.55f, 0.40f), new Vector3(0.30f, 0.08f, 0.30f), cDarkMetal, 0.5f, 0.4f);

            AddPrim(PrimitiveType.Cylinder, "Exhaust_L", t,
                new Vector3(-0.80f, 0.95f, 0.10f), new Vector3(0.12f, 0.40f, 0.12f), cPipe, 0.3f, 0.5f,
                new Vector3(0, 0, 90f));
            AddPrim(PrimitiveType.Cylinder, "Exhaust_R", t,
                new Vector3( 0.80f, 0.95f, 0.10f), new Vector3(0.12f, 0.40f, 0.12f), cPipe, 0.3f, 0.5f,
                new Vector3(0, 0, 90f));
            AddPrim(PrimitiveType.Cube, "Exhaust_ElbowL", t,
                new Vector3(-0.80f, 0.95f, 0.10f), new Vector3(0.16f, 0.16f, 0.16f), cPipe, 0.3f, 0.4f);
            AddPrim(PrimitiveType.Cube, "Exhaust_ElbowR", t,
                new Vector3( 0.80f, 0.95f, 0.10f), new Vector3(0.16f, 0.16f, 0.16f), cPipe, 0.3f, 0.4f);

            AddPrim(PrimitiveType.Cube, "Conveyor_Belt", t,
                new Vector3(0.50f, 0.26f, -0.55f), new Vector3(0.22f, 0.10f, 0.60f), cDarkMetal, 0.4f, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Conveyor_RollerF", t,
                new Vector3(0.50f, 0.26f, -0.82f), new Vector3(0.22f, 0.07f, 0.22f), cMetal, 0.5f, 0.6f,
                new Vector3(90, 0, 0));
            AddPrim(PrimitiveType.Cylinder, "Conveyor_RollerB", t,
                new Vector3(0.50f, 0.26f, -0.28f), new Vector3(0.22f, 0.07f, 0.22f), cMetal, 0.5f, 0.6f,
                new Vector3(90, 0, 0));
            AddPrim(PrimitiveType.Cube, "Ingot_1", t,
                new Vector3(0.50f, 0.36f, -0.68f), new Vector3(0.15f, 0.08f, 0.22f), cPlatinum, 0.8f, 0.95f);
            AddPrim(PrimitiveType.Cube, "Ingot_2", t,
                new Vector3(0.50f, 0.36f, -0.48f), new Vector3(0.15f, 0.08f, 0.22f), cPlatinum, 0.8f, 0.95f);

            AddPrim(PrimitiveType.Cylinder, "Gauge_Face", t,
                new Vector3(-0.30f, 1.05f, -0.78f), new Vector3(0.18f, 0.03f, 0.18f), cMetal, 0.5f, 0.7f);
            AddPrim(PrimitiveType.Sphere, "Gauge_Glass", t,
                new Vector3(-0.30f, 1.07f, -0.78f), new Vector3(0.14f, 0.06f, 0.14f),
                new Color(0.7f, 0.9f, 0.7f), 0, 0.95f);
            AddPrim(PrimitiveType.Cube, "Warning_Strip", t,
                new Vector3(0, 0.17f, -0.75f), new Vector3(1.56f, 0.06f, 0.04f), cWarning, 0, 0.5f);

            string dir = "Assets/_DerivTycoon/Resources/Buildings";
            if (!AssetDatabase.IsValidFolder("Assets/_DerivTycoon/Resources"))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon", "Resources");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon/Resources", "Buildings");

            string path = dir + "/PlatinumForgePrefab.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildPrefab] PlatinumForgePrefab saved to {path}");
        }
    }
}
