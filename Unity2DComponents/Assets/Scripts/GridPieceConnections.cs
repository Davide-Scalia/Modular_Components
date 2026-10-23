using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularGrid
{
    [Serializable]
    public struct GridConnectionEnd
    {
        [Tooltip("Cell coordinate in the GridFootprint editor grid.")]
        public GridCoordinate Cell;
        public GridDirection Facing;
    }

    [Serializable]
    public sealed class GridConnectionPath
    {
        public GridConnectionEnd Start;
        public GridConnectionEnd End;
    }

    public struct ResolvedGridConnectionEnd
    {
        public GridCoordinate CellOffset;
        public GridDirection Facing;
        public ResolvedGridConnectionEnd(GridCoordinate cellOffset, GridDirection facing)
        {
            CellOffset = cellOffset;
            Facing = facing;
        }
    }

    /// <summary>Optional route connections for a footprint. Each path has two ends; ends may share a cell.</summary>
    [RequireComponent(typeof(GridFootprint))]
    public sealed class GridPieceConnections : MonoBehaviour
    {
        [SerializeField] private List<GridConnectionPath> paths = new List<GridConnectionPath>();

        public bool HasPaths => paths != null && paths.Count > 0;
        public int PathCount => paths == null ? 0 : paths.Count;

        public bool TryGetResolvedEnds(GridFootprint footprint, GridShape shape, int rotationSteps, List<ResolvedGridConnectionEnd> result)
        {
            result.Clear();
            if (footprint == null || !footprint.UsesEditorGrid || !HasPaths)
                return false;

            foreach (GridConnectionPath path in paths)
            {
                if (path == null || !TryResolveEnd(footprint, path.Start, shape, rotationSteps, result) ||
                    !TryResolveEnd(footprint, path.End, shape, rotationSteps, result))
                {
                    result.Clear();
                    return false;
                }
            }

            for (int i = 0; i < result.Count; i++)
            {
                for (int j = i + 1; j < result.Count; j++)
                {
                    if (result[i].CellOffset == result[j].CellOffset && result[i].Facing == result[j].Facing)
                    {
                        result.Clear();
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool TryResolveEnd(GridFootprint footprint, GridConnectionEnd end,
            GridShape shape, int rotationSteps, List<ResolvedGridConnectionEnd> result)
        {
            if (!footprint.TryGetEditorCellOffset(end.Cell.X, end.Cell.Y, out GridCoordinate cellOffset) ||
                !footprint.IsEditorCellOccupied(end.Cell.X, end.Cell.Y) ||
                !GridLayout.TryGetDirectionOffset(end.Facing, shape, out _) ||
                !GridLayout.TryRotateDirection(end.Facing, shape, rotationSteps, out GridDirection facing))
                return false;

            result.Add(new ResolvedGridConnectionEnd(
                GridLayout.RotateOffset(cellOffset, shape, rotationSteps), facing));
            return true;
        }
    }
}
