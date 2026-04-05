using UnityEngine;

namespace DerivTycoon.Buildings
{
    public static class BuildingFactory
    {
        private static readonly BuildingConfig[] Configs = new[]
        {
            new BuildingConfig("frxXAUUSD",  "Gold Mine",          new Color(1.0f, 0.8f, 0.1f), 3.0f),
            new BuildingConfig("frxXAGUSD",  "Silver Mint",        new Color(0.75f, 0.75f, 0.8f), 2.5f),
            new BuildingConfig("frxXPTUSD",  "Platinum Forge",     new Color(0.9f, 0.95f, 1.0f), 3.5f),
            new BuildingConfig("frxXPDUSD",  "Palladium Refinery", new Color(0.5f, 0.6f, 0.75f), 2.0f),
            new BuildingConfig("1HZ100V",    "Trading Tower",      new Color(0.1f, 0.9f, 0.5f),  4.0f),
        };

        public static GameObject Create(string symbol, Vector3 position)
        {
            var config = GetConfig(symbol);

            var root = new GameObject($"Building_{config.Name}");
            root.transform.position = position;

            // Base cube
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, config.BaseHeight / 2f, 0);
            body.transform.localScale = new Vector3(1.4f, config.BaseHeight, 1.4f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = config.Color;
            body.GetComponent<Renderer>().material = mat;

            // Rooftop accent
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(root.transform);
            roof.transform.localPosition = new Vector3(0, config.BaseHeight + 0.15f, 0);
            roof.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);

            var roofMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            roofMat.color = config.Color * 0.6f;
            roof.GetComponent<Renderer>().material = roofMat;

            // Add BuildingController for P&L-driven visuals
            var controller = root.AddComponent<BuildingController>();
            controller.Initialize(symbol, config);

            return root;
        }

        private static BuildingConfig GetConfig(string symbol)
        {
            foreach (var c in Configs)
                if (c.Symbol == symbol) return c;

            // Fallback
            return new BuildingConfig(symbol, symbol, Color.white, 2f);
        }
    }

    public class BuildingConfig
    {
        public string Symbol;
        public string Name;
        public Color Color;
        public float BaseHeight;

        public BuildingConfig(string symbol, string name, Color color, float baseHeight)
        {
            Symbol = symbol;
            Name = name;
            Color = color;
            BaseHeight = baseHeight;
        }
    }
}
