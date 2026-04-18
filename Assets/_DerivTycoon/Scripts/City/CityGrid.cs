using UnityEngine;

namespace DerivTycoon.City
{
    public class CityGrid : MonoBehaviour
    {
        public static CityGrid Instance { get; private set; }

        [Header("Grid Settings")]
        public int GridWidth = 8;
        public int GridHeight = 8;
        public float CellSize = 2f;

        [Header("Visuals")]
        public Material TileMaterial;
        public Material TileHighlightMaterial;

        private GridCell[,] _cells;
        private GameObject[,] _tileObjects;

        public float WorldWidth => GridWidth * CellSize;
        public float WorldHeight => GridHeight * CellSize;
        public Vector3 GridOrigin => transform.position - new Vector3(WorldWidth / 2f, 0, WorldHeight / 2f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            BuildGrid();
        }

        private void BuildGrid()
        {
            _cells = new GridCell[GridWidth, GridHeight];
            _tileObjects = new GameObject[GridWidth, GridHeight];

            for (int x = 0; x < GridWidth; x++)
            {
                for (int z = 0; z < GridHeight; z++)
                {
                    Vector3 worldPos = GridOrigin + new Vector3(x * CellSize + CellSize / 2f, 0, z * CellSize + CellSize / 2f);
                    _cells[x, z] = new GridCell(x, z, worldPos);
                    _tileObjects[x, z] = CreateTile(x, z, worldPos);
                }
            }

            Debug.Log($"[CityGrid] Built {GridWidth}x{GridHeight} grid");
        }

        private GameObject CreateTile(int x, int z, Vector3 worldPos)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
            tile.name = $"Tile_{x}_{z}";
            tile.transform.SetParent(transform);
            tile.transform.position = worldPos;
            tile.transform.localScale = new Vector3(CellSize / 10f, 1f, CellSize / 10f);

            // Replace MeshCollider (stripped in WebGL) with a BoxCollider
            var meshCol = tile.GetComponent<Collider>();
            if (meshCol != null) Destroy(meshCol);
            var box = tile.AddComponent<BoxCollider>();
            box.size = new Vector3(10f, 0.1f, 10f); // matches Plane mesh extents in local space

            if (TileMaterial != null)
                tile.GetComponent<Renderer>().material = TileMaterial;
            else
            {
                // Checkerboard pattern using default material
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = (x + z) % 2 == 0
                    ? new Color(0.2f, 0.25f, 0.3f)
                    : new Color(0.15f, 0.18f, 0.22f);
                tile.GetComponent<Renderer>().material = mat;
            }

            return tile;
        }

        public GridCell GetCell(int x, int z)
        {
            if (x < 0 || x >= GridWidth || z < 0 || z >= GridHeight) return null;
            return _cells[x, z];
        }

        public GridCell GetCellAtWorldPos(Vector3 worldPos)
        {
            Vector3 local = worldPos - GridOrigin;
            int x = Mathf.FloorToInt(local.x / CellSize);
            int z = Mathf.FloorToInt(local.z / CellSize);
            return GetCell(x, z);
        }

        public void HighlightCell(int x, int z, bool highlight)
        {
            if (x < 0 || x >= GridWidth || z < 0 || z >= GridHeight) return;
            var tile = _tileObjects[x, z];
            if (tile == null) return;

            var renderer = tile.GetComponent<Renderer>();
            if (highlight && TileHighlightMaterial != null)
            {
                renderer.material = TileHighlightMaterial;
            }
            else
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = (x + z) % 2 == 0
                    ? new Color(0.2f, 0.25f, 0.3f)
                    : new Color(0.15f, 0.18f, 0.22f);
                renderer.material = mat;
            }
        }

        public bool PlaceBuilding(int x, int z, GameObject building)
        {
            var cell = GetCell(x, z);
            if (cell == null || cell.IsOccupied) return false;

            building.transform.position = cell.WorldPosition;
            cell.PlaceBuilding(building);
            return true;
        }

        public void RemoveBuilding(int x, int z)
        {
            var cell = GetCell(x, z);
            cell?.ClearBuilding();
        }
    }
}
