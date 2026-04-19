using UnityEditor;
using UnityEngine;

public static class BuildCityEnvironment
{
    private const string PolyPath   = "Assets/polyperfect/Low Poly Ultimate Pack/M/- Prefabs_M/";
    private const string SimplePath = "Assets/SimpleTown/Prefabs/";
    private const string PolyCity   = "Assets/PolygonCity/Prefabs/";

    [MenuItem("DerivTycoon/Setup CityGrid Props")]
    public static void SetupCityGridProps()
    {
        var cityGridGO = GameObject.Find("CityGrid");
        if (cityGridGO == null) { Debug.LogError("CityGrid not found in scene"); return; }

        var cg = cityGridGO.GetComponent<DerivTycoon.City.CityGrid>();
        if (cg == null) { Debug.LogError("CityGrid component not found"); return; }

        var so = new SerializedObject(cg);

        var smallProps = so.FindProperty("SmallPropPrefabs");
        string[] smallPaths =
        {
            "Assets/PolygonConstruction/Prefabs/Props/SM_Prop_Cone_01.prefab",
            "Assets/PolygonConstruction/Prefabs/Props/SM_Prop_Barrier_01.prefab",
            "Assets/PolygonConstruction/Prefabs/Props/SM_Prop_Cone_Barrier_01.prefab",
            PolyPath + "Construction_M/planks-stack.prefab",
            PolyPath + "Construction_M/brick-concrete-stack.prefab",
        };
        smallProps.arraySize = smallPaths.Length;
        for (int i = 0; i < smallPaths.Length; i++)
            smallProps.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(smallPaths[i]);

        var vehicleProps = so.FindProperty("VehiclePrefabs");
        string[] vehiclePaths =
        {
            "Assets/PolygonConstruction/Prefabs/Vehicles/SM_Veh_Excavator_01.prefab",
            "Assets/PolygonConstruction/Prefabs/Vehicles/SM_Veh_Bulldozer_01.prefab",
        };
        vehicleProps.arraySize = vehiclePaths.Length;
        for (int i = 0; i < vehiclePaths.Length; i++)
            vehicleProps.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(vehiclePaths[i]);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cg);
        Debug.Log("[BuildCityEnvironment] CityGrid props set up");
    }

    [MenuItem("DerivTycoon/Build City Environment")]
    public static void Build()
    {
        SetupCityGridProps();

        var existing = GameObject.Find("CityEnvironment");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("CityEnvironment");
        var rng  = new System.Random(42);

        // ── Ground base ─────────────────────────────────────────────────────
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "CityGround";
        ground.transform.SetParent(root.transform);
        ground.transform.position   = new Vector3(0, -0.02f, 0);
        ground.transform.localScale = new Vector3(7f, 1f, 7f);
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            { color = new Color(0.35f, 0.52f, 0.25f) };
        ground.GetComponent<Renderer>().material = groundMat;

        // ── Road ring ───────────────────────────────────────────────────────
        PlaceRoadRing(root.transform);

        // ── Houses (spaced out) ─────────────────────────────────────────────
        PlaceSuburbanHouses(root.transform, rng);

        // ── Street props along roads ────────────────────────────────────────
        PlaceStreetProps(root.transform, rng);

        // ── Parked cars ─────────────────────────────────────────────────────
        PlaceParkedCars(root.transform, rng);

        // ── Trees ───────────────────────────────────────────────────────────
        PlaceTrees(root.transform, rng);

        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[BuildCityEnvironment] Done");
    }

    // ── Road ring ─────────────────────────────────────────────────────────────
    static void PlaceRoadRing(Transform parent)
    {
        var roadStraight = Load(PolyPath + "Tiles_M/Roads_M/tile-mainroad-straight.prefab");
        if (roadStraight == null) { Debug.LogWarning("Road tile not found"); return; }

        float roadOffset = 9.5f;
        float s = 0.2f;
        int   n = 9;
        float start = -8f, step = 2f;

        for (int i = 0; i < n; i++)
        {
            float x = start + i * step;
            Place(roadStraight, new Vector3(x,  0,  roadOffset), Quaternion.Euler(0, 90, 0), s, parent);
            Place(roadStraight, new Vector3(x,  0, -roadOffset), Quaternion.Euler(0, 90, 0), s, parent);
        }
        for (int i = 0; i < n; i++)
        {
            float z = start + i * step;
            Place(roadStraight, new Vector3( roadOffset, 0, z), Quaternion.identity, s, parent);
            Place(roadStraight, new Vector3(-roadOffset, 0, z), Quaternion.identity, s, parent);
        }
    }

    // ── Suburban houses ───────────────────────────────────────────────────────
    static void PlaceSuburbanHouses(Transform parent, System.Random rng)
    {
        string[] paths =
        {
            PolyPath + "Buildings_M/building-house-family-small.prefab",
            PolyPath + "Buildings_M/building-house-family-large.prefab",
            PolyPath + "Buildings_M/building-house-modern.prefab",
            PolyPath + "Buildings_M/building-house-middle.prefab",
            PolyPath + "Buildings_M/building-house-big.prefab",
            PolyPath + "Buildings_M/building-house-block.prefab",
        };

        float offset  = 15f;   // further from grid
        float spacing = 5.5f;  // more space between houses
        int   count   = 5;
        float start   = -count * spacing / 2f + spacing * 0.5f;
        float baseScale = 0.42f;

        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < count; i++)
            {
                var prefab = Load(paths[rng.Next(paths.Length)]);
                if (prefab == null) continue;

                float p    = start + i * spacing + Jitter(rng, 0.5f);
                float perp = offset + Jitter(rng, 1.5f);
                float yRot = side * 90f + Jitter(rng, 10f);
                float sc   = baseScale * (float)(0.85 + rng.NextDouble() * 0.3);

                Vector3 pos = SidePos(side, p, perp);
                Place(prefab, pos, Quaternion.Euler(0, yRot, 0), sc, parent);
            }
        }
    }

    // ── Street props: lamp posts, bus stops, benches, hydrants, trash ─────────
    static void PlaceStreetProps(Transform parent, System.Random rng)
    {
        var lampPrefab      = Load(PolyPath + "Props_M/Props City_M/lamp-road.prefab");
        var busStopPrefab   = Load(PolyPath + "Props_M/Props City_M/bus-stop.prefab");
        var benchPrefab     = Load(PolyPath + "Props_M/Props City_M/bench-old.prefab");
        var hydrantPrefab   = Load(PolyPath + "Props_M/Props City_M/fire-hydrant.prefab");
        var dumpsterPrefab  = Load(PolyPath + "Props_M/Props City_M/dumpster.prefab");
        var trafficLight    = Load(SimplePath + "Props/traffic_light_mesh.prefab");
        var mailbox         = Load(PolyPath + "Props_M/Props City_M/mail-box.prefab");

        float roadEdge = 11f;
        float propScale = 0.25f;
        float tlScale   = 0.3f;

        // Lamp posts every ~4 units along the 4 road sides
        if (lampPrefab != null)
        {
            for (int side = 0; side < 4; side++)
            {
                for (float t = -8f; t <= 8f; t += 4f)
                {
                    float jit = Jitter(rng, 0.3f);
                    Vector3 pos = SidePos(side, t + jit, roadEdge);
                    float yRot = side * 90f;
                    Place(lampPrefab, pos, Quaternion.Euler(0, yRot, 0), propScale, parent);
                }
            }
        }

        // Traffic lights at the 4 corners
        if (trafficLight != null)
        {
            float c = roadEdge;
            Place(trafficLight, new Vector3( c, 0,  c), Quaternion.Euler(0, 225, 0), tlScale, parent);
            Place(trafficLight, new Vector3(-c, 0,  c), Quaternion.Euler(0, 315, 0), tlScale, parent);
            Place(trafficLight, new Vector3( c, 0, -c), Quaternion.Euler(0, 135, 0), tlScale, parent);
            Place(trafficLight, new Vector3(-c, 0, -c), Quaternion.Euler(0,  45, 0), tlScale, parent);
        }

        // Bus stop on 2 sides
        if (busStopPrefab != null)
        {
            Place(busStopPrefab, new Vector3(-3f, 0,  roadEdge), Quaternion.Euler(0, 180, 0), propScale * 1.4f, parent);
            Place(busStopPrefab, new Vector3( 3f, 0, -roadEdge), Quaternion.Euler(0,   0, 0), propScale * 1.4f, parent);
        }

        // Scatter benches, hydrants, mailboxes, dumpsters in the suburb area
        GameObject[] scatterProps = new[] { benchPrefab, hydrantPrefab, mailbox, dumpsterPrefab };
        for (int i = 0; i < 20; i++)
        {
            var prefab = scatterProps[rng.Next(scatterProps.Length)];
            if (prefab == null) continue;

            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float dist  = 11f + (float)(rng.NextDouble() * 6f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
            Place(prefab, pos, Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0), propScale, parent);
        }
    }

    // ── Parked cars along road edges ──────────────────────────────────────────
    static void PlaceParkedCars(Transform parent, System.Random rng)
    {
        string[] carPaths =
        {
            SimplePath + "Vehicles/car_blue.prefab",
            SimplePath + "Vehicles/car_green.prefab",
            SimplePath + "Vehicles/car_red.prefab",
            PolyPath + "Vehicles_M/Cars_M/car-sedan.prefab",
            PolyPath + "Vehicles_M/Cars_M/car-hatchback.prefab",
        };

        float carOffset = 12f;
        float carScale  = 0.25f;

        // Place a few parked cars on each side
        for (int side = 0; side < 4; side++)
        {
            int count = rng.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                var prefab = Load(carPaths[rng.Next(carPaths.Length)]);
                if (prefab == null) continue;

                float t    = Jitter(rng, 6f);
                float perp = carOffset + Jitter(rng, 0.5f);
                float yRot = side * 90f + Jitter(rng, 5f);

                Place(prefab, SidePos(side, t, perp), Quaternion.Euler(0, yRot, 0), carScale, parent);
            }
        }
    }

    // ── Trees ─────────────────────────────────────────────────────────────────
    static void PlaceTrees(Transform parent, System.Random rng)
    {
        string[] paths =
        {
            PolyPath + "Nature_M/Trees_M/tree-beech.prefab",
            PolyPath + "Nature_M/Trees_M/tree-birch.prefab",
            PolyPath + "Nature_M/Trees_M/tree-birch-tall.prefab",
            PolyPath + "Nature_M/Trees_M/bush-big.prefab",
            PolyPath + "Nature_M/Trees_M/bush-medium.prefab",
        };

        for (int i = 0; i < 40; i++)
        {
            var prefab = Load(paths[rng.Next(paths.Length)]);
            if (prefab == null) continue;

            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float dist  = 12f + (float)(rng.NextDouble() * 14f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
            float scale = (float)(0.25 + rng.NextDouble() * 0.2);

            Place(prefab, pos, Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0), scale, parent);
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

    static float Jitter(System.Random rng, float range) =>
        (float)(rng.NextDouble() * range * 2 - range);

    static GameObject Load(string path) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static void Place(GameObject prefab, Vector3 pos, Quaternion rot, float scale, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = Vector3.one * scale;
    }
}
