
using UnityEngine;
using UnityEditor;

namespace DerivTycoon.Editor
{
    public static class BuildPalladiumRefineryPrefab
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

        [MenuItem("DerivTycoon/Build Prefabs/Palladium Refinery")]
        public static void BuildPalladiumRefinery()
        {
            var cConcrete  = new Color(0.52f, 0.53f, 0.55f);
            var cTank      = new Color(0.62f, 0.68f, 0.76f);
            var cTankDark  = new Color(0.38f, 0.42f, 0.50f);
            var cMetal     = new Color(0.55f, 0.58f, 0.62f);
            var cMetalDark = new Color(0.30f, 0.32f, 0.35f);
            var cPipe      = new Color(0.50f, 0.52f, 0.55f);
            var cPipeDark  = new Color(0.28f, 0.30f, 0.33f);
            var cWarning   = new Color(0.88f, 0.62f, 0.05f);
            var cRed       = new Color(0.80f, 0.12f, 0.08f);
            var cPalladium = new Color(0.72f, 0.78f, 0.88f);
            var cGlass     = new Color(0.55f, 0.75f, 0.92f);
            var cIndicator = new Color(0.20f, 0.90f, 0.60f);

            var root = new GameObject("PalladiumRefineryPrefab");
            root.transform.position = new Vector3(19, 0, 0);
            var t = root.transform;

            AddPrim(PrimitiveType.Cube, "Pad", t,
                new Vector3(0, 0.07f, 0), new Vector3(1.90f, 0.14f, 1.90f), cConcrete, 0, 0.3f);

            AddPrim(PrimitiveType.Cylinder, "Tank_Main_Body", t,
                new Vector3(0.08f, 1.00f, 0.20f), new Vector3(1.00f, 0.88f, 1.00f), cTank, 0.4f, 0.6f);
            AddPrim(PrimitiveType.Sphere, "Tank_Main_Dome", t,
                new Vector3(0.08f, 1.92f, 0.20f), new Vector3(1.00f, 0.30f, 1.00f), cTankDark, 0.4f, 0.7f);
            AddPrim(PrimitiveType.Cylinder, "Tank_Main_Base", t,
                new Vector3(0.08f, 0.20f, 0.20f), new Vector3(1.10f, 0.08f, 1.10f), cMetalDark, 0.5f, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Tank_Band1", t,
                new Vector3(0.08f, 0.62f, 0.20f), new Vector3(1.06f, 0.06f, 1.06f), cMetalDark, 0.5f, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Tank_Band2", t,
                new Vector3(0.08f, 1.10f, 0.20f), new Vector3(1.06f, 0.06f, 1.06f), cMetalDark, 0.5f, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Tank_Band3", t,
                new Vector3(0.08f, 1.58f, 0.20f), new Vector3(1.06f, 0.06f, 1.06f), cMetalDark, 0.5f, 0.5f);
            AddPrim(PrimitiveType.Cube, "Tank_Ladder", t,
                new Vector3(0.58f, 1.00f, -0.32f), new Vector3(0.06f, 1.80f, 0.04f), cMetalDark, 0.4f, 0.4f);

            AddPrim(PrimitiveType.Cylinder, "Tank_Small_Body", t,
                new Vector3(-0.58f, 0.65f, 0.30f), new Vector3(0.52f, 0.56f, 0.52f), cTank, 0.3f, 0.55f);
            AddPrim(PrimitiveType.Sphere, "Tank_Small_Dome", t,
                new Vector3(-0.58f, 1.20f, 0.30f), new Vector3(0.52f, 0.18f, 0.52f), cTankDark, 0.3f, 0.6f);
            AddPrim(PrimitiveType.Cylinder, "Tank_Small_Base", t,
                new Vector3(-0.58f, 0.15f, 0.30f), new Vector3(0.58f, 0.06f, 0.58f), cMetalDark, 0.5f, 0.4f);

            AddPrim(PrimitiveType.Cylinder, "Pipe_H1", t,
                new Vector3(-0.25f, 1.30f, 0.15f), new Vector3(0.10f, 0.35f, 0.10f), cPipe, 0.3f, 0.5f,
                new Vector3(0, 0, 90f));
            AddPrim(PrimitiveType.Cube, "Pipe_Elbow1", t,
                new Vector3(-0.58f, 1.30f, 0.15f), new Vector3(0.14f, 0.14f, 0.14f), cPipeDark, 0.4f, 0.4f);
            AddPrim(PrimitiveType.Cylinder, "Pipe_V1", t,
                new Vector3(-0.58f, 1.10f, 0.15f), new Vector3(0.10f, 0.22f, 0.10f), cPipe, 0.3f, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Pipe_Front", t,
                new Vector3(0.08f, 0.75f, -0.52f), new Vector3(0.10f, 0.40f, 0.10f), cPipe, 0.3f, 0.5f,
                new Vector3(90, 0, 0));
            AddPrim(PrimitiveType.Cube, "Pipe_FrontElbow", t,
                new Vector3(0.08f, 0.75f, -0.78f), new Vector3(0.13f, 0.13f, 0.13f), cPipeDark, 0.4f, 0.4f);
            AddPrim(PrimitiveType.Cylinder, "Pipe_FrontDown", t,
                new Vector3(0.08f, 0.50f, -0.78f), new Vector3(0.10f, 0.28f, 0.10f), cPipe, 0.3f, 0.5f);

            AddPrim(PrimitiveType.Cylinder, "Valve_Big", t,
                new Vector3(-0.05f, 1.30f, 0.15f), new Vector3(0.24f, 0.04f, 0.24f), cRed, 0.2f, 0.6f);
            AddPrim(PrimitiveType.Cube, "Valve_BigSpoke", t,
                new Vector3(-0.05f, 1.32f, 0.15f), new Vector3(0.22f, 0.04f, 0.04f), cRed, 0.2f, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Valve_Small", t,
                new Vector3(0.08f, 0.75f, -0.65f), new Vector3(0.16f, 0.04f, 0.16f), cRed, 0.2f, 0.6f,
                new Vector3(90, 0, 0));

            AddPrim(PrimitiveType.Cube, "ControlRoom_Body", t,
                new Vector3(0.55f, 0.52f, -0.55f), new Vector3(0.65f, 0.70f, 0.60f), cConcrete, 0, 0.25f);
            AddPrim(PrimitiveType.Cube, "ControlRoom_Roof", t,
                new Vector3(0.55f, 0.92f, -0.55f), new Vector3(0.72f, 0.12f, 0.66f), cMetalDark, 0.4f, 0.3f);
            AddPrim(PrimitiveType.Cube, "CtrlRoom_WinF", t,
                new Vector3(0.55f, 0.55f, -0.85f), new Vector3(0.42f, 0.32f, 0.04f), cGlass, 0, 0.95f);
            AddPrim(PrimitiveType.Cube, "CtrlRoom_WinL", t,
                new Vector3(0.22f, 0.55f, -0.55f), new Vector3(0.04f, 0.32f, 0.38f), cGlass, 0, 0.95f);
            AddPrim(PrimitiveType.Sphere, "Status_Green", t,
                new Vector3(0.44f, 0.90f, -0.85f), new Vector3(0.07f, 0.07f, 0.04f), cIndicator, 0, 0.9f);
            AddPrim(PrimitiveType.Sphere, "Status_Yellow", t,
                new Vector3(0.55f, 0.90f, -0.85f), new Vector3(0.07f, 0.07f, 0.04f), cWarning, 0, 0.9f);

            AddPrim(PrimitiveType.Cylinder, "VentStack", t,
                new Vector3(-0.10f, 2.28f, -0.10f), new Vector3(0.12f, 0.52f, 0.12f),
                new Color(0.35f, 0.35f, 0.36f), 0.3f, 0.4f);
            AddPrim(PrimitiveType.Cylinder, "VentStack_Cap", t,
                new Vector3(-0.10f, 2.58f, -0.10f), new Vector3(0.20f, 0.05f, 0.20f), cMetalDark, 0.4f, 0.5f);
            AddPrim(PrimitiveType.Cylinder, "Palladium_Sheen", t,
                new Vector3(0.08f, 1.28f, 0.20f), new Vector3(0.55f, 0.04f, 0.55f), cPalladium, 0.7f, 0.95f);
            AddPrim(PrimitiveType.Cube, "Warning_Strip", t,
                new Vector3(0, 0.15f, -0.76f), new Vector3(1.56f, 0.06f, 0.04f), cWarning, 0, 0.5f);

            var gPos = new Vector3[] {
                new Vector3(-0.78f, 0.17f, -0.70f),
                new Vector3( 0.75f, 0.17f,  0.72f),
                new Vector3(-0.76f, 0.17f,  0.68f),
            };
            for (int i = 0; i < gPos.Length; i++)
                AddPrim(PrimitiveType.Sphere, $"Gravel_{i}", t,
                    gPos[i], new Vector3(0.12f, 0.08f, 0.15f), cConcrete, 0, 0.15f);

            string dir = "Assets/_DerivTycoon/Resources/Buildings";
            if (!AssetDatabase.IsValidFolder("Assets/_DerivTycoon/Resources"))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon", "Resources");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon/Resources", "Buildings");

            string path = dir + "/PalladiumRefineryPrefab.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildPrefab] PalladiumRefineryPrefab saved to {path}");
        }
    }
}
