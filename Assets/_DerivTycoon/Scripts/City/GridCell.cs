using UnityEngine;

namespace DerivTycoon.City
{
    public class GridCell
    {
        public int X { get; }
        public int Y { get; }
        public Vector3 WorldPosition { get; }
        public bool IsOccupied { get; private set; }
        public GameObject Building { get; private set; }

        public GridCell(int x, int y, Vector3 worldPosition)
        {
            X = x;
            Y = y;
            WorldPosition = worldPosition;
        }

        public void PlaceBuilding(GameObject building)
        {
            Building = building;
            IsOccupied = true;
        }

        public void ClearBuilding()
        {
            Building = null;
            IsOccupied = false;
        }
    }
}
