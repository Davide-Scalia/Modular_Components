using System.Collections.Generic;
using ModularGrid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RpgToolkit
{
    // Scene-owned terrain and actor positions; independent of GridManager's placed pieces.
    public sealed class GameplayGrid2D : MonoBehaviour
    {
        [SerializeField] private Tilemap ground;
        [SerializeField] private TopDownTileRules rules;
        private readonly Dictionary<GridCoordinate, Component> occupants = new Dictionary<GridCoordinate, Component>();

        public GridCoordinate WorldToCell(Vector3 world)
        {
            Vector3Int cell = ground.WorldToCell(world);
            return new GridCoordinate(cell.x, cell.y);
        }

        public Vector3 CellToWorld(GridCoordinate cell) => ground.GetCellCenterWorld(new Vector3Int(cell.X, cell.Y, 0));

        public bool IsWalkable(GridCoordinate cell, out TopDownTileRule rule)
        {
            rule = default;
            return ground != null && rules != null &&
                rules.TryGet(ground.GetTile(new Vector3Int(cell.X, cell.Y, 0)), out rule) &&
                rule.Walkable && rule.EntryCost > 0;
        }

        public bool TryGetOccupant(GridCoordinate cell, out Component actor) => occupants.TryGetValue(cell, out actor);

        public bool TryRegister(GridCoordinate cell, Component actor)
        {
            if (actor == null || !IsWalkable(cell, out _) || occupants.ContainsKey(cell)) return false;
            occupants.Add(cell, actor);
            return true;
        }

        public void Unregister(GridCoordinate cell, Component actor)
        {
            if (occupants.TryGetValue(cell, out Component current) && current == actor)
                occupants.Remove(cell);
        }

        public bool TryMove(GridCoordinate from, GridCoordinate to, Component actor, out int cost)
        {
            cost = 0;
            if (!occupants.TryGetValue(from, out Component current) || current != actor ||
                !TryTraversal(from, to, out cost)) return false;
            occupants.Remove(from);
            occupants[to] = actor;
            return true;
        }

        public bool TryTraversal(GridCoordinate from, GridCoordinate to, out int cost)
        {
            cost = 0;
            if (occupants.ContainsKey(to) || !IsWalkable(from, out TopDownTileRule a) ||
                !IsWalkable(to, out TopDownTileRule b)) return false;

            CardinalEdge exit, entry;
            int dx = to.X - from.X, dy = to.Y - from.Y;
            if (dx == 1 && dy == 0) { exit = CardinalEdge.East; entry = CardinalEdge.West; }
            else if (dx == -1 && dy == 0) { exit = CardinalEdge.West; entry = CardinalEdge.East; }
            else if (dx == 0 && dy == 1) { exit = CardinalEdge.North; entry = CardinalEdge.South; }
            else if (dx == 0 && dy == -1) { exit = CardinalEdge.South; entry = CardinalEdge.North; }
            else return false;

            if ((a.OpenEdges & exit) == 0 || (b.OpenEdges & entry) == 0) return false;
            cost = b.EntryCost;
            return true;
        }

        public int MinimumCost => 1; // Valid lower bound even when all configured costs are higher.
    }
}
