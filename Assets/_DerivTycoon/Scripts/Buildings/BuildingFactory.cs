using UnityEngine;

namespace DerivTycoon.Buildings
{
    public static class BuildingFactory
    {
        private static readonly BuildingConfig[] Configs = new[]
        {
            new BuildingConfig("frxXAUUSD", "Gold Mine",     new Color(1.0f, 0.8f, 0.1f),  3.0f, "GoldMinePrefab",     300f, 0f),
            new BuildingConfig("frxXAGUSD", "Silver Mint",   new Color(0.75f, 0.75f, 0.8f), 2.5f, "SilverMintPrefab",   300f, 0f),
            new BuildingConfig("1HZ100V",   "Trading Tower", new Color(0.1f, 0.9f, 0.5f),  4.0f, "TradingTowerPrefab",  60f, 0f),
        };

        public static GameObject Create(string symbol, Vector3 position)
        {
            var config = GetConfig(symbol);

            // Try to load prefab first
            var prefab = Resources.Load<GameObject>($"Buildings/{config.PrefabName}");
            if (prefab != null)
            {
                var instance = Object.Instantiate(prefab, position, Quaternion.identity);
                instance.name = $"Building_{config.Name}";

                var controller = instance.GetComponent<BuildingController>();
                if (controller == null)
                    controller = instance.AddComponent<BuildingController>();
                controller.Initialize(symbol, config);

                return instance;
            }

            // Fallback: procedural two-cube building
            return CreateProcedural(config, position);
        }

        private static GameObject CreateProcedural(BuildingConfig config, Vector3 position)
        {
            var root = new GameObject($"Building_{config.Name}");
            root.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, config.BaseHeight / 2f, 0);
            body.transform.localScale = new Vector3(1.4f, config.BaseHeight, 1.4f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = config.Color;
            body.GetComponent<Renderer>().material = mat;

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(root.transform);
            roof.transform.localPosition = new Vector3(0, config.BaseHeight + 0.15f, 0);
            roof.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);

            var roofMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            roofMat.color = config.Color * 0.6f;
            roof.GetComponent<Renderer>().material = roofMat;

            var controller = root.AddComponent<BuildingController>();
            controller.Initialize(symbol: config.Symbol, config);

            return root;
        }

        public static BuildingConfig GetConfig(string symbol)
        {
            foreach (var c in Configs)
                if (c.Symbol == symbol) return c;

            return new BuildingConfig(symbol, symbol, Color.white, 2f, null, 300f, 0f);
        }
    }

    public class BuildingConfig
    {
        public string Symbol;
        public string Name;
        public Color Color;
        public float BaseHeight;
        public string PrefabName;
        public float CycleDuration;   // seconds per production cycle
        public float BarrierOffset;   // 0 = ATM (metals), -1.2 = below spot (Vol100)

        public BuildingConfig(string symbol, string name, Color color, float baseHeight, string prefabName, float cycleDuration, float barrierOffset)
        {
            Symbol = symbol;
            Name = name;
            Color = color;
            BaseHeight = baseHeight;
            PrefabName = prefabName;
            CycleDuration = cycleDuration;
            BarrierOffset = barrierOffset;
        }
    }
}
