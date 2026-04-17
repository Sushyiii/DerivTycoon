using UnityEditor;
using UnityEngine;

namespace DerivTycoon.Editor
{
    /// <summary>
    /// Builds the GoldMinePrefab using:
    ///   - Skyden Mountain 1 (sandy low-poly cone) as the rocky hill
    ///   - PurePoly gold crystals as the hero gold element
    ///   - Mine entrance at the base of the mountain
    ///   - Cart, pickaxe, lantern, barrel, crate as props
    /// Materials for primitives are saved as asset files to avoid serialization issues.
    /// </summary>
    public static class BuildGoldMinePrefabV3
    {
        const string MOUNTAIN = "Assets/Skyden_Games/Low Poly Environment/Prefabs/Mountain 1.prefab";
        const string CRYSTAL  = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Stone_Crystal_02_Gold.prefab";
        const string GEM      = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Gemstone_07_Gold.prefab";
        const string CART     = "Assets/Skyden_Games/Low Poly Environment/Prefabs/Cart01.prefab";
        const string PICKAXE  = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Pickaxe_Used_04_Iron.prefab";
        const string LANTERN  = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Lantern_02_Yellow_Light_Iron.prefab";
        const string BARREL   = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Barrel_03.prefab";
        const string CRATE    = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Crate_Wooden_03.prefab";
        const string CACTUS   = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Cactus_04.prefab";
        const string MAT_DIR  = "Assets/_DerivTycoon/Resources/Buildings/Materials";
        const string OUT_DIR  = "Assets/_DerivTycoon/Resources/Buildings";

        static Shader _urpLit;

        [MenuItem("DerivTycoon/Build Prefabs/Gold Mine V3")]
        public static void Build()
        {
            _urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (_urpLit == null)
            {
                Debug.LogError("[GoldMineV3] URP Lit shader not found");
                return;
            }

            EnsureFolder(OUT_DIR);
            EnsureFolder(MAT_DIR);
            // Create & fully import materials BEFORE building the prefab
            AssetDatabase.DeleteAsset(MAT_DIR + "/GoldMine_Tunnel.mat");
            AssetDatabase.DeleteAsset(MAT_DIR + "/GoldMine_Wood.mat");
            AssetDatabase.DeleteAsset(MAT_DIR + "/GoldMine_Metal.mat");
            AssetDatabase.DeleteAsset(MAT_DIR + "/GoldMine_Stone.mat");

            SaveMatToFile("GoldMine_Tunnel", new Color(0.04f, 0.03f, 0.02f), 0f, 0.05f);
            SaveMatToFile("GoldMine_Wood",   new Color(0.42f, 0.28f, 0.13f), 0f, 0.2f);
            SaveMatToFile("GoldMine_Metal",  new Color(0.38f, 0.35f, 0.32f), 0.4f, 0.4f);
            SaveMatToFile("GoldMine_Stone",  new Color(0.62f, 0.50f, 0.35f), 0f, 0.15f);

            // Force AssetDatabase to import the new materials
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Now load them back as proper assets
            var matTunnel = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/GoldMine_Tunnel.mat");
            var matWood   = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/GoldMine_Wood.mat");
            var matMetal  = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/GoldMine_Metal.mat");
            var matStone  = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/GoldMine_Stone.mat");

            var root = new GameObject("GoldMinePrefab");
            root.transform.position = new Vector3(20, 0, 0);
            var t = root.transform;

            // 1. Tall mountain — tinted GOLD by recoloring its renderers
            var mountain = Child(MOUNTAIN, t);
            if (mountain != null)
            {
                var b = GetBounds(mountain);
                float s = b.size.x > 0.01f ? 2.0f / b.size.x : 1f;
                mountain.transform.localScale = new Vector3(s, s * 2.2f, s); // very tall
                b = GetBounds(mountain);
                mountain.transform.localPosition = new Vector3(0, -b.min.y + root.transform.position.y, 0);
                mountain.transform.localEulerAngles = new Vector3(0, 225, 0);
                mountain.name = "Mountain";
                // Tint the mountain GOLDEN — saves into prefab via per-instance materials
                var matGold = SaveMatToFileAndReturn("GoldMine_MtnGold",
                    new Color(0.85f, 0.65f, 0.10f), 0.1f, 0.3f);
                foreach (var r in mountain.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = matGold;
            }

            // 2. Large gold gems bursting from the mountain face — PP_Gemstone_07_Gold is bright yellow
            // PP_Gemstone pivot is at base, so y positions are straightforward
            var g1 = Child(GEM, t); SetChild(g1, "Gold_A",
                new Vector3(0.0f, 2.5f, 0.55f), Vector3.one * 1.40f, new Vector3(-15, 10, 0));
            var g2 = Child(GEM, t); SetChild(g2, "Gold_B",
                new Vector3(-0.4f, 2.0f, 0.60f), Vector3.one * 1.10f, new Vector3(-10, -30, 10));
            var g3 = Child(GEM, t); SetChild(g3, "Gold_C",
                new Vector3(0.45f, 1.8f, 0.50f), Vector3.one * 1.00f, new Vector3(-20, 40, -8));
            var g4 = Child(GEM, t); SetChild(g4, "Gold_D",
                new Vector3(-0.1f, 1.3f, 0.65f), Vector3.one * 0.85f, new Vector3(5, -15, 5));
            var g5 = Child(GEM, t); SetChild(g5, "Gold_E",
                new Vector3(0.3f, 3.0f, 0.30f), Vector3.one * 0.90f, new Vector3(-25, 20, 15));

            // 3. Dark mine entrance at base
            AddPrim("Tunnel_Void", t, new Vector3(0, 0.50f, 0.88f),
                new Vector3(0.55f, 0.75f, 0.25f), matTunnel);
            AddPrim("Portal_L", t, new Vector3(-0.32f, 0.55f, 0.86f),
                new Vector3(0.10f, 0.85f, 0.18f), matWood);
            AddPrim("Portal_R", t, new Vector3(0.32f, 0.55f, 0.86f),
                new Vector3(0.10f, 0.85f, 0.18f), matWood);
            AddPrim("Portal_Top", t, new Vector3(0, 0.98f, 0.86f),
                new Vector3(0.76f, 0.12f, 0.18f), matWood);

            // 4. Pickaxe prop
            var pick = Child(PICKAXE, t); SetChild(pick, "Pickaxe",
                new Vector3(-0.60f, 0.60f, 0.72f), Vector3.one * 0.40f, new Vector3(-15, 25, -65));

            // 5. Cactus
            var cactus = Child(CACTUS, t); SetChild(cactus, "Cactus",
                new Vector3(-0.75f, 0.05f, 0.25f), Vector3.one * 0.40f, new Vector3(0, 15, 0));

            // Remove all child colliders
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            // Save prefab
            string path = OUT_DIR + "/GoldMinePrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(root);

            Debug.Log($"[GoldMineV3] Saved ??? {path}");
        }

        // ====== Helpers ======

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name   = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static Material SaveMatToFileAndReturn(string name, Color col, float metallic, float smoothness)
        {
            AssetDatabase.DeleteAsset($"{MAT_DIR}/{name}.mat");
            SaveMatToFile(name, col, metallic, smoothness);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/{name}.mat");
        }

        static void SaveMatToFile(string name, Color col, float metallic, float smoothness)
        {
            string path = $"{MAT_DIR}/{name}.mat";
            var mat = new Material(_urpLit);
            mat.color = col;
            mat.SetFloat("_Metallic",   metallic);
            mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
        }

        // Primitive mine cart: body + rim + 4 wheels
        static void BuildMineCart(Transform parent, Vector3 origin, Material matMetal, Material matWood)
        {
            var cart = new GameObject("MineCart");
            cart.transform.SetParent(parent);
            cart.transform.localPosition = origin;

            AddPrimTo("Cart_Body", cart.transform, new Vector3(0, 0.28f, 0), new Vector3(0.38f, 0.26f, 0.44f), matWood);
            AddPrimTo("Cart_Rim",  cart.transform, new Vector3(0, 0.42f, 0), new Vector3(0.44f, 0.06f, 0.50f), matMetal);
            // Wheels
            var wheelPos = new Vector3[] {
                new Vector3(-0.20f, 0.16f, 0.18f), new Vector3(0.20f, 0.16f, 0.18f),
                new Vector3(-0.20f, 0.16f, -0.18f), new Vector3(0.20f, 0.16f, -0.18f),
            };
            for (int i = 0; i < 4; i++)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                w.name = $"Wheel_{i}";
                w.transform.SetParent(cart.transform);
                w.transform.localPosition = wheelPos[i];
                w.transform.localScale = new Vector3(0.18f, 0.06f, 0.18f);
                w.transform.localEulerAngles = new Vector3(0, 0, 90);
                w.GetComponent<Renderer>().sharedMaterial = matMetal;
                var c = w.GetComponent<Collider>(); if (c) Object.DestroyImmediate(c);
            }

            // Gold ore nuggets in cart
            AddPrimTo("Ore_1", cart.transform, new Vector3(-0.06f, 0.50f, 0.05f),  new Vector3(0.15f, 0.12f, 0.15f), matMetal); // tinted gold at runtime
            AddPrimTo("Ore_2", cart.transform, new Vector3( 0.07f, 0.50f, -0.06f), new Vector3(0.12f, 0.10f, 0.12f), matMetal);
        }

        static void AddPrimTo(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var c = go.GetComponent<Collider>(); if (c) Object.DestroyImmediate(c);
        }

        static GameObject Child(string assetPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) { Debug.LogWarning($"[GoldMineV3] Not found: {assetPath}"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetChild(GameObject go, string name, Vector3 pos, Vector3 scale, Vector3 euler)
        {
            if (go == null) return;
            go.name = name;
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            go.transform.localEulerAngles = euler;
        }

        static Bounds GetBounds(GameObject go)
        {
            var rr = go.GetComponentsInChildren<Renderer>();
            if (rr.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rr[0].bounds;
            foreach (var r in rr) b.Encapsulate(r.bounds);
            return b;
        }

        static void AddPrim(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
    }
}
