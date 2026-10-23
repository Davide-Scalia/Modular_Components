using UnityEngine;
using UnityEngine.InputSystem;

namespace ModularGrid
{
    public sealed class GridPointerPicker : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private Camera pointerCamera;

        public bool TryGetHoveredCell(out GridCoordinate coordinate)
        {
            if (TryGetHoveredCell(out GridCell cell))
            {
                coordinate = cell.Coordinate;
                return true;
            }
            coordinate = default;
            return false;
        }

        public bool TryGetHoveredCell(out GridCell hoveredCell)
        {
            hoveredCell = null;
            if (grid == null || Mouse.current == null)
                return false;

            Camera cameraToUse = pointerCamera != null ? pointerCamera : Camera.main;
            if (cameraToUse == null)
                return false;

            Ray ray = cameraToUse.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (grid.Use2DPhysics)
            {
                RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity, grid.TileLayerMask);
                foreach (RaycastHit2D hit in hits)
                {
                    GridCell cell = hit.collider.GetComponentInParent<GridCell>();
                    if (cell == null)
                        continue;

                    hoveredCell = cell;
                    return true;
                }

                return false;
            }

            RaycastHit[] hits3D = Physics.RaycastAll(ray, Mathf.Infinity, grid.TileLayerMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits3D)
            {
                GridCell cell = hit.collider.GetComponentInParent<GridCell>();
                if (cell == null)
                    continue;

                hoveredCell = cell;
                return true;
            }

            return false;
        }
    }
}
