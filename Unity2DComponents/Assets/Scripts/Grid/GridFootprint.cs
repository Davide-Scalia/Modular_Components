using System.Collections.Generic;
using UnityEngine;

namespace ModularGrid
{
    public sealed class GridFootprint : MonoBehaviour
    {
        [Tooltip("One grid offset per occupied tile, relative to this object's pivot.")]
        [SerializeField] private List<GridCoordinate> cells = new List<GridCoordinate> { new GridCoordinate(0, 0) };
        [SerializeField, Min(1)] private int editorGridWidth = 1;
        [SerializeField, Min(1)] private int editorGridHeight = 1;
        [SerializeField] private List<GridCoordinate> editorOccupiedCells = new List<GridCoordinate> { new GridCoordinate(0, 0) };
        [SerializeField, HideInInspector] private bool useEditorGrid;

        private Quaternion baseRotation;

        public int RotationSteps { get; private set; }
        public bool UsesEditorGrid => useEditorGrid;
        public int EditorGridWidth => editorGridWidth;
        public int EditorGridHeight => editorGridHeight;

        private void Awake()
        {
            baseRotation = transform.localRotation;
        }

        public IEnumerable<GridCoordinate> GetOccupiedCells(GridCoordinate anchor, GridShape shape)
            => GetOccupiedCells(anchor, shape, RotationSteps);

        public IEnumerable<GridCoordinate> GetOccupiedCells(GridCoordinate anchor, GridShape shape, int rotationSteps)
        {
            foreach (GridCoordinate offset in GetOffsets())
                yield return anchor + GridLayout.RotateOffset(offset, shape, rotationSteps);
        }

        public bool TryGetEditorCellOffset(int x, int y, out GridCoordinate offset)
        {
            offset = default;
            if (!useEditorGrid || x < 0 || x >= editorGridWidth || y < 0 || y >= editorGridHeight)
                return false;

            offset = new GridCoordinate(x - (editorGridWidth - 1) / 2, y - (editorGridHeight - 1) / 2);
            return true;
        }

        public bool IsEditorCellOccupied(int x, int y)
        {
            if (!useEditorGrid)
                return false;
            foreach (GridCoordinate cell in editorOccupiedCells)
                if (cell.X == x && cell.Y == y)
                    return true;
            return false;
        }

        public Vector2 PivotOffset(GridShape shape)
            => PivotOffset(shape, RotationSteps);

        public Vector2 PivotOffset(GridShape shape, int rotationSteps)
        {
            if (!useEditorGrid)
                return Vector2.zero;
            Vector2 center = new Vector2((editorGridWidth - 1) * 0.5f, (editorGridHeight - 1) * 0.5f);
            Vector2 anchorCell = new Vector2((editorGridWidth - 1) / 2, (editorGridHeight - 1) / 2);
            Vector2 offset = center - anchorCell;
            for (int i = 0; i < rotationSteps; i++)
                offset = shape == GridShape.Square ? new Vector2(-offset.y, offset.x) : new Vector2(-offset.y, offset.x + offset.y);
            return offset;
        }

        private IEnumerable<GridCoordinate> GetOffsets()
        {
            if (!useEditorGrid)
            {
                foreach (GridCoordinate cell in cells)
                    yield return cell;
                yield break;
            }

            int pivotX = (editorGridWidth - 1) / 2;
            int pivotY = (editorGridHeight - 1) / 2;
            foreach (GridCoordinate cell in editorOccupiedCells)
                yield return new GridCoordinate(cell.X - pivotX, cell.Y - pivotY);
        }

        public void Rotate(GridShape shape, int steps = 1)
        {
            int turnCount = shape == GridShape.Square ? 4 : 6;
            RotationSteps = ((RotationSteps + steps) % turnCount + turnCount) % turnCount;
        }

        public void ApplyRotation(GridShape shape, bool use2DPhysics)
        {
            float degrees = GridLayout.RotationDegrees(shape, RotationSteps);
            Quaternion rotation = use2DPhysics
                ? Quaternion.Euler(0f, 0f, degrees)
                : Quaternion.Euler(0f, -degrees, 0f);
            transform.localRotation = baseRotation * rotation;
        }
    }
}
