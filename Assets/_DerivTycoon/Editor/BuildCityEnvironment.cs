using UnityEditor;
using UnityEngine;

public static class BuildCityEnvironment
{
    // Asset pack base paths
    private const string Poly   = "Assets/polyperfect/Low Poly Ultimate Pack/M/- Prefabs_M/";
    private const string PCityE = "Assets/PolygonCity/Prefabs/Environments/";
    private const string PCityP = "Assets/PolygonCity/Prefabs/Props/";
    private const string PCityV = "Assets/PolygonCity/Prefabs/Vehicles/";
    private const string PCityB = "Assets/PolygonCity/Prefabs/Buildings/";
    private const string Simple = "Assets/SimpleTown/Prefabs/";

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("DerivTycoon/Setup CityGrid Props")]
    public static void SetupCityGridProps()
    {
        var go = GameObject.Find("CityGrid");
        if (go == null) { Debug.LogError("CityGrid not found"); return; }
        var cg = go.GetComponent<DerivTycoon.City.CityGrid>();
        if (cg == null) { Debug.LogError("CityGrid component missing"); return; }

        var so = new SerializedObject(cg);

        // Small props on empty tiles — visible, not green-tinted
        SetArray(so, "SmallPropPrefabs", new[]
        {
            "Assets/PolygonConstruction/Prefabs/Props/SM_Prop_Cone_01.prefab",
            "Assets/PolygonConstruction/Prefabs/Props/SM_Prop_Cone_Barrier_01.prefab",
            Poly + "Construction_M/planks-stack.prefab",
            Poly + "Construction_M/steel-prop-stack.prefab",
            PCityP + "SM_Prop_Pallet_01.prefab",
            PCityP + "SM_Prop_Skip_01.prefab",
        });

        // Vehicles on tiles
        SetArray(so, "VehiclePrefabs", new[]
        {
            "Assets/PolygonConstruction/Prefabs/Vehicles/SM_Veh_Excavator_01.prefab",
            "Assets/PolygonConstruction/Prefabs/Vehicles/SM_Veh_Bulldozer_01.prefab",
            "Assets/PolygonConstruction/Prefabs/Vehicles/SM_Veh_Mini_Loader_01.prefab",
        });

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cg);
        Debug.Log("[CityEnv] CityGrid props configured");
    }

    static void SetArray(SerializedObject so, string prop, string[] paths)
    {
        var p = so.FindProperty(prop);
        p.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("DerivTycoon/Build City Environment")]
    public static void Build()
    {
        SetupCityGridProps();

        var existing = GameObject.Find("CityEnvironment");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("CityEnvironment");
        var rng  = new System.Random(42);

        // 1. Large green ground base
        BuildGround(root.transform);

        // 2. PolygonCity sidewalk tiles around the grid perimeter
        BuildSidewalks(root.transform);

        // 3. Spaced-out suburban houses
        BuildHouses(root.transform, rng);

        // 4. Background city buildings (depth)
        BuildBackgroundBuildings(root.transform, rng);

        // 5. Street furniture (light poles, benches, trash cans, bus stops)
        BuildStreetFurniture(root.transform, rng);

        // 6. Parked cars (PolygonCity vehicles)
        BuildParkedCars(root.transform, rng);

        // 7. Trees and greenery
        BuildTrees(root.transform, rng);

        MarkDirty(root);
        Debug.Log("[CityEnv] City environment built successfully");
    }

    // ── 1. Ground ─────────────────────────────────────────────────────────────
    static void BuildGround(Transform parent)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "CityGround";
        g.transform.SetParent(parent);
        g.transform.position   = new Vector3(0, -0.02f, 0);
        g.transform.localScale = new Vector3(7.5f, 1f, 7.5f); // 75x75 units
        Object.DestroyImmediate(g.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            { color = new Color(0.32f, 0.50f, 0.22f) };
        g.GetComponent<Renderer>().material = mat;
    }

    // ── 2. Sidewalk ring using PolygonCity tiles ───────────────────────────────
    static void BuildSidewalks(Transform parent)
    {
        var swStraight = Load(PCityE + "SM_Env_Sidewalk_01.prefab");
        var swCorner   = Load(PCityE + "SM_Env_Sidewalk_Corner_01.prefab");
        var swConst    = Load(PCityE + "SM_Env_Sidewalk_Construction_01.prefab");

        if (swStraight == null) return;

        float offset = 9.8f;
        float sw = 0.1f;   // sidewalk tile scale
        float step = 2f;
        int   n = 8;

        for (int i = 0; i < n; i++)
        {
            float t = -7f + i * step;
            // Alternate normal/construction sidewalk for variety
            var tile = (i % 3 == 0 && swConst != null) ? swConst : swStraight;
            Place(tile, new Vector3(t, 0,  offset), Quaternion.Euler(0, 0,   0), sw, parent);
            Place(tile, new Vector3(t, 0, -offset), Quaternion.Euler(0, 180, 0), sw, parent);
            Place(tile, new Vector3( offset, 0, t), Quaternion.Euler(0, 90,  0), sw, parent);
            Place(tile, new Vector3(-offset, 0, t), Quaternion.Euler(0, 270, 0), sw, parent);
        }

        // Corners
        if (swCorner != null)
        {
            Place(swCorner, new Vector3( offset, 0,  offset), Quaternion.Euler(0, 0,   0), sw, parent);
            Place(swCorner, new Vector3(-offset, 0,  offset), Quaternion.Euler(0, 270, 0), sw, parent);
            Place(swCorner, new Vector3( offset, 0, -offset), Quaternion.Euler(0, 90,  0), sw, parent);
            Place(swCorner, new Vector3(-offset, 0, -offset), Quaternion.Euler(0, 180, 0), sw, parent);
        }
    }

    // ── 3. Suburban houses ────────────────────────────────────────────────────
    static void BuildHouses(Transform parent, System.Random rng)
    {
        string[] paths =
        {
            Poly + "Buildings_M/building-house-family-small.prefab",
            Poly + "Buildings_M/building-house-family-large.prefab",
            Poly + "Buildings_M/building-house-modern.prefab",
            Poly + "Buildings_M/building-house-middle.prefab",
            Poly + "Buildings_M/building-house-big.prefab",
            Poly + "Buildings_M/building-house-block.prefab",
        };

        float offset  = 16f;
        float spacing = 6f;
        int   count   = 5;
        float baseScale = 0.42f;

        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Load(paths[rng.Next(paths.Length)]);
                if (p == null) continue;
                float t    = -(count - 1) * spacing / 2f + i * spacing + Jitter(rng, 0.8f);
                float perp = offset + Jitter(rng, 2f);
                float sc   = baseScale * (float)(0.85 + rng.NextDouble() * 0.3);
                Place(p, SidePos(side, t, perp), Quaternion.Euler(0, side * 90f + Jitter(rng, 8f), 0), sc, parent);
            }
        }
    }

    // ── 4. Background city buildings ──────────────────────────────────────────
    static void BuildBackgroundBuildings(Transform parent, System.Random rng)
    {
        string[] paths =
        {
            PCityB + "SM_Bld_Apartment_01.prefab",
            PCityB + "SM_Bld_Apartment_02.prefab",
            PCityB + "SM_Bld_Apartment_03.prefab",
            Poly   + "Buildings_M/building-block-4floor-front.prefab",
            Poly   + "Buildings_M/building-block-5floor-front.prefab",
        };

        float offset = 25f;
        int count = 4;

        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Load(paths[rng.Next(paths.Length)]);
                if (p == null) continue;
                float t    = -(count - 1) * 7f / 2f + i * 7f + Jitter(rng, 1f);
                float perp = offset + Jitter(rng, 3f);
                float sc   = (float)(0.3 + rng.NextDouble() * 0.15);
                Place(p, SidePos(side, t, perp), Quaternion.Euler(0, side * 90f, 0), sc, parent);
            }
        }
    }

    // ── 5. Street furniture ───────────────────────────────────────────────────
    static void BuildStreetFurniture(Transform parent, System.Random rng)
    {
        // Load prefabs
        var lampBase   = Load(PCityP + "SM_Prop_LightPole_Base_01.prefab");
        var lampLight  = Load(PCityP + "SM_Prop_LightPole_Lights_01.prefab");
        var busStop    = Load(PCityP + "SM_Prop_BusStop_01.prefab");
        var bench      = Load(PCityP + "SM_Prop_ParkBench_01.prefab");
        var trashCan   = Load(PCityP + "SM_Prop_TrashCan_01.prefab");
        var trashBin   = Load(PCityP + "SM_Prop_Trashbin_01.prefab");
        var trafficLt  = Load(PCityP + "SM_Prop_TrafficLight_01.prefab");
        var mailbox    = Load(PCityP + "SM_Prop_Mailbox_01.prefab");
        var parking    = Load(PCityP + "SM_Prop_ParkingMeter_01.prefab");
        var billboard  = Load(Simple + "Props/billboard_mesh.prefab");
        var simpleLamp = Load(Simple + "Props/lamp_mesh.prefab");

        float sw = 10.5f;  // street furniture sits just outside sidewalk
        float propS = 0.25f;

        // Lamp posts every 4 units along all 4 sides — PolygonCity style
        for (int side = 0; side < 4; side++)
        {
            for (float t = -7f; t <= 7f; t += 4f)
            {
                float j = Jitter(rng, 0.2f);
                Vector3 pos = SidePos(side, t + j, sw);
                float rot = side * 90f;

                if (lampBase != null) Place(lampBase,  pos, Quaternion.Euler(0, rot, 0), propS, parent);
                if (lampLight != null)
                {
                    var lt = Place(lampLight, pos + Vector3.up * 0.025f * (1f / propS),
                                   Quaternion.Euler(0, rot, 0), propS, parent);
                }
            }
        }

        // Traffic lights at corners
        if (trafficLt != null)
        {
            float c = sw;
            Place(trafficLt, new Vector3( c, 0,  c), Quaternion.Euler(0, 225, 0), propS * 1.1f, parent);
            Place(trafficLt, new Vector3(-c, 0,  c), Quaternion.Euler(0, 315, 0), propS * 1.1f, parent);
            Place(trafficLt, new Vector3( c, 0, -c), Quaternion.Euler(0, 135, 0), propS * 1.1f, parent);
            Place(trafficLt, new Vector3(-c, 0, -c), Quaternion.Euler(0,  45, 0), propS * 1.1f, parent);
        }

        // Bus stops on 2 sides
        if (busStop != null)
        {
            Place(busStop, new Vector3(-2f, 0,  sw), Quaternion.Euler(0, 180, 0), propS * 1.3f, parent);
            Place(busStop, new Vector3( 2f, 0, -sw), Quaternion.Euler(0,   0, 0), propS * 1.3f, parent);
        }

        // Scatter benches, trash cans, mailboxes, parking meters in suburb
        GameObject[] scatter = { bench, trashCan, trashBin, mailbox, parking };
        for (int i = 0; i < 28; i++)
        {
            var pf = scatter[rng.Next(scatter.Length)];
            if (pf == null) continue;
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float dist  = 11f + (float)(rng.NextDouble() * 8f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
            Place(pf, pos, Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0), propS, parent);
        }

        // Billboards scattered further out
        if (billboard != null)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                float dist  = 18f + (float)(rng.NextDouble() * 6f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
                Place(billboard, pos, Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0), 0.4f, parent);
            }
        }
    }

    // ── 6. Parked cars ────────────────────────────────────────────────────────
    static void BuildParkedCars(Transform parent, System.Random rng)
    {
        string[] paths =
        {
            PCityV + "SM_Veh_Car_Sedan_01.prefab",
            PCityV + "SM_Veh_Car_Medium_01.prefab",
            PCityV + "SM_Veh_Car_Small_01.prefab",
            PCityV + "SM_Veh_Car_Taxi_01.prefab",
            PCityV + "SM_Veh_Car_Van_01.prefab",
        };

        float curb = 12.5f;
        float carSc = 0.22f;

        for (int side = 0; side < 4; side++)
        {
            int count = rng.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                var pf = Load(paths[rng.Next(paths.Length)]);
                if (pf == null) continue;
                float t    = Jitter(rng, 6f);
                float perp = curb + Jitter(rng, 0.4f);
                float yRot = side * 90f + Jitter(rng, 5f);
                Place(pf, SidePos(side, t, perp), Quaternion.Euler(0, yRot, 0), carSc, parent);
            }
        }
    }

    // ── 7. Trees & greenery ───────────────────────────────────────────────────
    static void BuildTrees(Transform parent, System.Random rng)
    {
        string[] paths =
        {
            PCityE + "SM_Env_Tree_01.prefab",
            PCityE + "SM_Env_Tree_02.prefab",
            PCityE + "SM_Env_Tree_03.prefab",
            Poly   + "Nature_M/Trees_M/tree-beech.prefab",
            Poly   + "Nature_M/Trees_M/tree-birch.prefab",
            Poly   + "Nature_M/Trees_M/tree-birch-tall.prefab",
            Simple + "Props/tree_large_mesh.prefab",
            Simple + "Props/tree_medium_mesh.prefab",
        };

        for (int i = 0; i < 50; i++)
        {
            var pf = Load(paths[rng.Next(paths.Length)]);
            if (pf == null) continue;

            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float dist  = 11.5f + (float)(rng.NextDouble() * 16f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
            float sc = (float)(0.2 + rng.NextDouble() * 0.2);
            Place(pf, pos, Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0), sc, parent);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static Vector3 SidePos(int side, float along, float perp) => side switch
    {
        0 => new Vector3( along, 0,  perp),
        1 => new Vector3( perp,  0, -along),
        2 => new Vector3(-along, 0, -perp),
        _ => new Vector3(-perp,  0,  along),
    };

    static float Jitter(System.Random rng, float r) =>
        (float)(rng.NextDouble() * r * 2 - r);

    static GameObject Load(string path) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static GameObject Place(GameObject pf, Vector3 pos, Quaternion rot, float scale, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static void MarkDirty(GameObject root)
    {
        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
