using System.Collections.Generic;
using UnityEngine;

namespace DerivTycoon.City
{
    public class ConstructionPlot : MonoBehaviour
    {
        private readonly List<GameObject> _props = new();
        private bool _cleared;
        private float _cellSize = 2f;

        private GameObject[] _smallPropPrefabs;
        private GameObject[] _vehiclePrefabs;

        public void Init(float cellSize, GameObject[] smallProps, GameObject[] vehicles)
        {
            _cellSize = cellSize;
            _smallPropPrefabs = smallProps;
            _vehiclePrefabs = vehicles;
        }

        private void Start()
        {
            SpawnProps();
        }

        private void SpawnProps()
        {
            if (_cleared) return;
            if (_smallPropPrefabs == null || _smallPropPrefabs.Length == 0) return;

            int propCount = Random.Range(1, 3);
            float halfCell = _cellSize * 0.4f;

            for (int i = 0; i < propCount; i++)
            {
                var prefab = _smallPropPrefabs[Random.Range(0, _smallPropPrefabs.Length)];
                if (prefab == null) continue;

                Vector2 offset = Random.insideUnitCircle * halfCell * 0.7f;
                Vector3 pos = transform.position + new Vector3(offset.x, 0f, offset.y);
                float yRot = Random.Range(0f, 360f);

                var prop = Instantiate(prefab, pos, Quaternion.Euler(0, yRot, 0), transform);
                prop.transform.localScale = Vector3.one * Random.Range(0.8f, 1.1f);
                RemoveCollidersRecursive(prop);
                _props.Add(prop);
            }

            // 1 in 5 tiles gets a vehicle
            if (Random.value < 0.2f && _vehiclePrefabs != null && _vehiclePrefabs.Length > 0)
            {
                var prefab = _vehiclePrefabs[Random.Range(0, _vehiclePrefabs.Length)];
                if (prefab != null)
                {
                    Vector2 offset = Random.insideUnitCircle * halfCell * 0.5f;
                    Vector3 pos = transform.position + new Vector3(offset.x, 0f, offset.y);
                    var vehicle = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), transform);
                    vehicle.transform.localScale = Vector3.one * 0.5f;
                    RemoveCollidersRecursive(vehicle);
                    _props.Add(vehicle);
                }
            }
        }

        // Remove colliders from props so they don't block raycasts on the tile
        private static void RemoveCollidersRecursive(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>())
                Destroy(col);
        }

        public void ClearProps()
        {
            _cleared = true;
            foreach (var prop in _props)
                if (prop != null) Destroy(prop);
            _props.Clear();
        }

        public void RestoreProps()
        {
            _cleared = false;
            SpawnProps();
        }
    }
}
