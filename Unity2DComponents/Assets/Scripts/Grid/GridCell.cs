using UnityEngine;

namespace ModularGrid
{
    public sealed class GridCell : MonoBehaviour
    {
        public GridCoordinate Coordinate { get; private set; }

        public void Initialize(GridCoordinate coordinate)
        {
            Coordinate = coordinate;
        }
    }
}
