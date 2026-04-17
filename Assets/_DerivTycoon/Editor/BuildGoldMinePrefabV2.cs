using UnityEditor;
using UnityEngine;

namespace DerivTycoon.Editor
{
    public static class BuildGoldMinePrefabV2
    {
        const string CLIFF   = "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Dirt_Cliff_01.prefab";
        const string CRYSTAL = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Stone_Crystal_02_Gold.prefab";
        const string GEM     = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Gemstone_07_Gold.prefab";
        const string CART    = "Assets/Skyden_Games/Low Poly Environment/Prefabs/Cart01.prefab";
        const string PICKAXE = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Pickaxe_Used_04_Iron.prefab";
        const string LANTERN = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Lantern_02_Yellow_Light_Iron.prefab";
        const string BARREL  = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Barrel_03.prefab";
        const string CRATE   = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Crate_Wooden_03.prefab";
        const string ROCKS_A = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_01.prefab";
        const string ROCKS_B = "Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Small_Rocks_02.prefab";
        const string CACTUS  = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Cactus_04.prefab";
        const string BASE_MAT = "Assets/Materials/WallMat.mat";

        [MenuItem("DerivTycoon/Build Prefabs/Gold Mine V2")]
        public static void Build()
        {
            var root = new GameObject("GoldMinePrefab");
            root.transform.position = new Vector3(20, 0, 0); // off to side, cleaned up after
            var t = root.transform;

            // 1. Dirt cliff ??? main rocky body
            var cliff = Child(CLIFF, t, Vector3.zero, Vector3.zero, Vector3.zero);
            if (cliff != null)
            {
                // Scale to ~1.8 units wide
                var b = Bounds(cliff);
                float s = b.size.x > 0.01f ? 1.8f / b.size.x : 0.012f;
                cliff.transform.localScale = Vector3.one * s;
                // Center it so base sits at y=0
                b = Bounds(cliff);
                cliff.transform.localPosition = new Vector3(0, -b.min.y, 0);
                cliff.transform.localEulerAngles = new Vector3(0, 180, 0);
                cliff.name = "Cliff";
            }
            else
            {
                // Fallback rock hill
                AddPrim("Cliff", t, new Vector3(0, 0.75f, 0), new Vector3(1.8f, 1.5f, 1.5f),
                    new Color(0.50f, 0.40f, 0.28f));
            }

            // 2. Gold crystals ??? the unmistakable hero element
            var c1 = Child(CRYSTAL, t, new Vector3(-0.28f, 1.35f, 0.05f), Vector3.one * 0.50f, new Vector3(0, 30, 18));
            if (c1 != null) c1.name = "GoldCrystal_A";

            var c2 = Child(CRYSTAL, t, new Vector3(0.22f, 1.45f, -0.05f), Vector3.one * 0.40f, new Vector3(8, -20, -12));
            if (c2 != null) c2.name = "GoldCrystal_B";

            var c3 = Child(CRYSTAL, t, new Vector3(0.0f, 1.60f, -0.15f), Vector3.one * 0.32f, new Vector3(-5, 60, 22));
            if (c3 != null) c3.name = "GoldCrystal_C";

            // 3. Gold gem accents near entrance
            var g1 = Child(GEM, t, new Vector3(-0.60f, 0.30f, -0.52f), Vector3.one * 0.32f, new Vector3(0, 45, 0));
            if (g1 != null) g1.name = "GoldGem_A";

            var g2 = Child(GEM, t, new Vector3(0.52f, 0.28f, -0.50f), Vector3.one * 0.24f, new Vector3(0, -30, 0));
            if (g2 != null) g2.name = "GoldGem_B";

            // 4. Mine entrance tunnel
            AddPrim("Tunnel_Void", t, new Vector3(0, 0.38f, -0.70f), new Vector3(0.44f, 0.62f, 0.22f),
                new Color(0.04f, 0.03f, 0.02f));
            AddPrim("Portal_Post_L", t, new Vector3(-0.26f, 0.44f, -0.68f), new Vector3(0.08f, 0.74f, 0.13f),
                new Color(0.40f, 0.26f, 0.12f));
            AddPrim("Portal_Post_R", t, new Vector3(0.26f, 0.44f, -0.68f), new Vector3(0.08f, 0.74f, 0.13f),
                new Color(0.40f, 0.26f, 0.12f));
            AddPrim("Portal_Lintel", t, new Vector3(0, 0.82f, -0.68f), new Vector3(0.62f, 0.10f, 0.13f),
                new Color(0.35f, 0.22f, 0.10f));

            // 5. Cart rails leading from entrance
            AddPrim("Rail_L", t, new Vector3(-0.10f, 0.18f, -0.44f), new Vector3(0.05f, 0.05f, 0.72f),
                new Color(0.38f, 0.35f, 0.32f));
            AddPrim("Rail_R", t, new Vector3(0.10f, 0.18f, -0.44f), new Vector3(0.05f, 0.05f, 0.72f),
                new Color(0.38f, 0.35f, 0.32f));
            for (int i = 0; i < 3; i++)
                AddPrim($"Tie_{i}", t, new Vector3(0, 0.16f, -0.18f - i * 0.24f),
                    new Vector3(0.32f, 0.04f, 0.07f), new Color(0.33f, 0.21f, 0.10f));

            // 6. Mine cart on tracks
            var cart = Child(CART, t, new Vector3(0, 0.22f, -0.46f), Vector3.one * 0.26f, new Vector3(0, 0, 0));
            if (cart != null) cart.name = "MineCart";

            // 7. Pickaxe leaning at entrance
            var pick = Child(PICKAXE, t, new Vector3(-0.52f, 0.48f, -0.56f), Vector3.one * 0.32f, new Vector3(-15, 30, -68));
            if (pick != null) pick.name = "Pickaxe";

            // 8. Lantern on portal post
            var lantern = Child(LANTERN, t, new Vector3(0.40f, 0.75f, -0.62f), Vector3.one * 0.30f, Vector3.zero);
            if (lantern != null) lantern.name = "Lantern";

            // 9. Supply props ??? barrel and crate grouped to one side
            var barrel = Child(BARREL, t, new Vector3(0.65f, 0.20f, 0.28f), Vector3.one * 0.32f, new Vector3(0, 20, 0));
            if (barrel != null) barrel.name = "Barrel";

            var crate = Child(CRATE, t, new Vector3(0.55f, 0.18f, 0.52f), Vector3.one * 0.30f, new Vector3(0, -15, 0));
            if (crate != null) crate.name = "Crate";

            // 10. Ground rocks ??? scatter
            var ra = Child(ROCKS_A, t, new Vector3(-0.72f, 0.05f, 0.52f), Vector3.one * 0.65f, new Vector3(0, 45, 0));
            if (ra != null) ra.name = "Rocks_A";

            var rb = Child(ROCKS_B, t, new Vector3(0.68f, 0.05f, -0.22f), Vector3.one * 0.55f, new Vector3(0, -20, 0));
            if (rb != null) rb.name = "Rocks_B";

            // 11. Cactus ??? desert mine atmosphere
            var cactus = Child(CACTUS, t, new Vector3(-0.68f, 0.05f, -0.25f), Vector3.one * 0.32f, new Vector3(0, 15, 0));
            if (cactus != null) cactus.name = "Cactus";

            // Remove all colliders (BuildingController adds its own)
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            // Save prefab
            string dir = "Assets/_DerivTycoon/Resources/Buildings";
            if (!AssetDatabase.IsValidFolder("Assets/_DerivTycoon/Resources"))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon", "Resources");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/_DerivTycoon/Resources", "Buildings");

            string path = dir + "/GoldMinePrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(root);

            Debug.Log($"[GoldMineV2] Saved ??? {path}");
        }

        // Instantiate a prefab asset as child with given local transform
        static GameObject Child(string assetPath, Transform parent,
            Vector3 localPos, Vector3 localScale, Vector3 localEuler)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[GoldMineV2] Not found: {assetPath}");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            if (localScale != Vector3.zero) go.transform.localScale = localScale;
            go.transform.localEulerAngles = localEuler;
            return go;
        }

        // Get world-space bounds of all renderers in a GO
        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new UnityEngine.Bounds(go.transform.position, Vector3.one);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // Add a primitive with a URP material (using WallMat as template)
        static void AddPrim(string name, Transform parent, Vector3 pos, Vector3 scale, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            var baseMat = AssetDatabase.LoadAssetAtPath<Material>(BASE_MAT);
            if (baseMat != null)
            {
                var mat = new Material(baseMat) { color = col };
                go.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
    }
}
