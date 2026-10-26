using System.Collections.Generic;

namespace ModularGrid
{
    /// <summary>Tracks placed connection paths and matches reciprocal ends during adjacent placement.</summary>
    public sealed class GridConnectionResolver
    {
        private readonly List<PlacedPiece> placedPieces = new List<PlacedPiece>();
        private readonly List<ResolvedGridConnectionEnd> candidateEnds = new List<ResolvedGridConnectionEnd>();
        private readonly List<ResolvedGridConnectionEnd> existingEnds = new List<ResolvedGridConnectionEnd>();

        public void Register(GridFootprint footprint, GridPieceConnections connections, GridCoordinate anchor)
        {
            placedPieces.Add(new PlacedPiece(footprint, connections, anchor, footprint.RotationSteps));
        }

        public bool RequiresConnection(GridPieceConnections candidate, IReadOnlyList<GridCoordinate> candidateCells, GridShape shape)
        {
            PruneDestroyedPieces();
            if (candidate != null)
                return true;

            foreach (PlacedPiece placed in placedPieces)
            {
                if (placed.Connections == null)
                    continue;

                foreach (GridCoordinate existingCell in placed.Footprint.GetOccupiedCells(placed.Anchor, shape, placed.RotationSteps))
                {
                    foreach (GridCoordinate candidateCell in candidateCells)
                    {
                        foreach (GridCoordinate direction in GridLayout.GetNeighbors(shape))
                            if (existingCell + direction == candidateCell)
                                return true;
                    }
                }
            }
            return false;
        }

        public bool HasMatchingConnection(GridFootprint footprint, GridPieceConnections connections,
            GridCoordinate anchor, GridShape shape, int rotationSteps,
            GridConnectionDirectionality directionality)
        {
            PruneDestroyedPieces();
            if (connections == null || !connections.TryGetResolvedEnds(footprint, shape, rotationSteps, candidateEnds))
                return false;

            foreach (PlacedPiece placed in placedPieces)
            {
                if (placed.Connections == null ||
                    !placed.Connections.TryGetResolvedEnds(placed.Footprint, shape, placed.RotationSteps, existingEnds))
                    continue;

                foreach (ResolvedGridConnectionEnd candidateEnd in candidateEnds)
                {
                    GridCoordinate candidateCell = anchor + candidateEnd.CellOffset;
                    if (!GridLayout.TryGetDirectionOffset(candidateEnd.Facing, shape, out GridCoordinate candidateFacing))
                        continue;

                    foreach (ResolvedGridConnectionEnd existingEnd in existingEnds)
                    {
                        if (directionality == GridConnectionDirectionality.Unidirectional &&
                            candidateEnd.FlowRole != GridConnectionFlowRole.Either &&
                            existingEnd.FlowRole != GridConnectionFlowRole.Either &&
                            candidateEnd.FlowRole == existingEnd.FlowRole)
                            continue;

                        GridCoordinate existingCell = placed.Anchor + existingEnd.CellOffset;
                        if (candidateCell + candidateFacing != existingCell ||
                            !GridLayout.TryGetDirectionOffset(existingEnd.Facing, shape, out GridCoordinate existingFacing) ||
                            existingCell + existingFacing != candidateCell)
                            continue;

                        return true;
                    }
                }
            }
            return false;
        }

        public void Clear() => placedPieces.Clear();

        private void PruneDestroyedPieces()
        {
            for (int i = placedPieces.Count - 1; i >= 0; i--)
                if (placedPieces[i].Footprint == null)
                    placedPieces.RemoveAt(i);
        }

        private readonly struct PlacedPiece
        {
            public readonly GridFootprint Footprint;
            public readonly GridPieceConnections Connections;
            public readonly GridCoordinate Anchor;
            public readonly int RotationSteps;

            public PlacedPiece(GridFootprint footprint, GridPieceConnections connections, GridCoordinate anchor, int rotationSteps)
            {
                Footprint = footprint;
                Connections = connections;
                Anchor = anchor;
                RotationSteps = rotationSteps;
            }
        }
    }
}
