using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularGrid
{
    public enum GridConnectionSetupMode
    {
        EntrancesAndExits,
        Openings
    }

    public enum GridConnectionFlowRole
    {
        Either,
        Entrance,
        Exit
    }

    [Serializable]
    public struct GridConnectionEnd
    {
        [Tooltip("Cell coordinate in the GridFootprint editor grid.")]
        public GridCoordinate Cell;
        public GridDirection Facing;
        [Tooltip("Only used in Openings mode. Either accepts both incoming and outgoing connections.")]
        public GridConnectionFlowRole FlowRole;
    }

    // Kept serialized for existing prefab compatibility. Use MigrateLegacyPaths in the Inspector to convert.
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
        public GridConnectionFlowRole FlowRole;

        public ResolvedGridConnectionEnd(GridCoordinate cellOffset, GridDirection facing, GridConnectionFlowRole flowRole)
        {
            CellOffset = cellOffset;
            Facing = facing;
            FlowRole = flowRole;
        }
    }

    /// <summary>Defines the available connection openings for a footprint.</summary>
    [RequireComponent(typeof(GridFootprint))]
    public sealed class GridPieceConnections : MonoBehaviour
    {
        [SerializeField] private GridConnectionSetupMode setupMode = GridConnectionSetupMode.EntrancesAndExits;
        [SerializeField] private List<GridConnectionEnd> entrances = new List<GridConnectionEnd>();
        [SerializeField] private List<GridConnectionEnd> exits = new List<GridConnectionEnd>();
        [SerializeField] private List<GridConnectionEnd> openings = new List<GridConnectionEnd>();
        [SerializeField, HideInInspector] private List<GridConnectionPath> paths = new List<GridConnectionPath>();

        public GridConnectionSetupMode SetupMode => setupMode;
        public bool HasConnections => setupMode == GridConnectionSetupMode.Openings
            ? openings != null && openings.Count > 0
            : (entrances != null && entrances.Count > 0) || (exits != null && exits.Count > 0) || (paths != null && paths.Count > 0);
        public bool HasPaths => HasConnections;
        public int PathCount
        {
            get
            {
                if (setupMode == GridConnectionSetupMode.Openings)
                    return openings == null ? 0 : openings.Count;
                if ((entrances == null || entrances.Count == 0) && (exits == null || exits.Count == 0))
                    return paths == null ? 0 : paths.Count;
                return (entrances == null ? 0 : entrances.Count) * (exits == null ? 0 : exits.Count);
            }
        }

        public void MigrateLegacyPaths()
        {
            if (paths == null || paths.Count == 0)
                return;

            setupMode = GridConnectionSetupMode.EntrancesAndExits;
            if (entrances == null) entrances = new List<GridConnectionEnd>();
            if (exits == null) exits = new List<GridConnectionEnd>();

            foreach (GridConnectionPath path in paths)
            {
                if (path == null)
                    continue;

                AddUnique(entrances, path.Start);
                AddUnique(exits, path.End);
            }
            paths.Clear();
        }

        public bool TryGetResolvedEnds(GridFootprint footprint, GridShape shape, int rotationSteps,
            List<ResolvedGridConnectionEnd> result)
        {
            result.Clear();
            if (footprint == null || !footprint.UsesEditorGrid)
                return false;

            bool valid;
            if (setupMode == GridConnectionSetupMode.Openings)
            {
                valid = TryResolveList(footprint, openings, GridConnectionFlowRole.Either, false,
                    shape, rotationSteps, result);
            }
            else
            {
                valid = TryResolveList(footprint, entrances, GridConnectionFlowRole.Entrance, true,
                            shape, rotationSteps, result) &&
                        TryResolveList(footprint, exits, GridConnectionFlowRole.Exit, true,
                            shape, rotationSteps, result);
                // Preserve old serialized path entries until the Inspector migration button is applied.
                if (valid && paths != null && paths.Count > 0)
                    valid = TryResolveLegacyPaths(footprint, shape, rotationSteps, result);
            }

            if (!valid)
            {
                result.Clear();
                return false;
            }
            return result.Count > 0;
        }

        private bool TryResolveLegacyPaths(GridFootprint footprint, GridShape shape, int rotationSteps,
            List<ResolvedGridConnectionEnd> result)
        {
            foreach (GridConnectionPath path in paths)
            {
                if (path == null ||
                    !TryResolveEnd(footprint, path.Start, GridConnectionFlowRole.Entrance, shape, rotationSteps, result) ||
                    !TryResolveEnd(footprint, path.End, GridConnectionFlowRole.Exit, shape, rotationSteps, result))
                    return false;
            }
            return true;
        }

        private static bool TryResolveList(GridFootprint footprint, List<GridConnectionEnd> ends,
            GridConnectionFlowRole role, bool overrideRole, GridShape shape, int rotationSteps,
            List<ResolvedGridConnectionEnd> result)
        {
            if (ends == null)
                return true;

            foreach (GridConnectionEnd end in ends)
            {
                GridConnectionFlowRole resolvedRole = overrideRole ? role : end.FlowRole;
                if (!TryResolveEnd(footprint, end, resolvedRole, shape, rotationSteps, result))
                    return false;
            }
            return true;
        }

        private static bool TryResolveEnd(GridFootprint footprint, GridConnectionEnd end,
            GridConnectionFlowRole role, GridShape shape, int rotationSteps,
            List<ResolvedGridConnectionEnd> result)
        {
            if (!footprint.TryGetEditorCellOffset(end.Cell.X, end.Cell.Y, out GridCoordinate cellOffset) ||
                !footprint.IsEditorCellOccupied(end.Cell.X, end.Cell.Y) ||
                !GridLayout.TryGetDirectionOffset(end.Facing, shape, out _) ||
                !GridLayout.TryRotateDirection(end.Facing, shape, rotationSteps, out GridDirection facing))
                return false;

            GridCoordinate rotatedOffset = GridLayout.RotateOffset(cellOffset, shape, rotationSteps);
            for (int i = 0; i < result.Count; i++)
                if (result[i].CellOffset == rotatedOffset && result[i].Facing == facing && result[i].FlowRole == role)
                    return true;

            result.Add(new ResolvedGridConnectionEnd(rotatedOffset, facing, role));
            return true;
        }

        private static void AddUnique(List<GridConnectionEnd> list, GridConnectionEnd end)
        {
            foreach (GridConnectionEnd existing in list)
                if (existing.Cell.Equals(end.Cell) && existing.Facing == end.Facing)
                    return;
            end.FlowRole = GridConnectionFlowRole.Either;
            list.Add(end);
        }
    }
}
