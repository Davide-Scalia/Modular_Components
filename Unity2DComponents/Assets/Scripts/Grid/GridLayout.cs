using UnityEngine;
using System.Collections.Generic;

namespace ModularGrid
{
    public enum GridShape
    {
        Square,
        Hexagon
    }

    public enum GridDirection
    {
        East,
        NorthEast,
        North,
        NorthWest,
        West,
        SouthWest,
        South,
        SouthEast
    }

    public static class GridLayout
    {
        private const float Sqrt3 = 1.73205080757f;
        private static readonly GridCoordinate[] SquareNeighbors =
        {
            new GridCoordinate(1, 0), new GridCoordinate(-1, 0),
            new GridCoordinate(0, 1), new GridCoordinate(0, -1)
        };
        private static readonly GridCoordinate[] HexNeighbors =
        {
            new GridCoordinate(1, 0), new GridCoordinate(1, -1), new GridCoordinate(0, -1),
            new GridCoordinate(-1, 0), new GridCoordinate(-1, 1), new GridCoordinate(0, 1)
        };
        private static readonly GridDirection[] SquareDirections =
        {
            GridDirection.East, GridDirection.North, GridDirection.West, GridDirection.South
        };
        private static readonly GridDirection[] HexDirections =
        {
            GridDirection.East, GridDirection.NorthEast, GridDirection.NorthWest,
            GridDirection.West, GridDirection.SouthWest, GridDirection.SouthEast
        };

        public static IReadOnlyList<GridCoordinate> GetNeighbors(GridShape shape) =>
            shape == GridShape.Square ? SquareNeighbors : HexNeighbors;

        public static bool TryGetDirectionOffset(GridDirection direction, GridShape shape, out GridCoordinate offset)
        {
            if (shape == GridShape.Square)
            {
                switch (direction)
                {
                    case GridDirection.East: offset = new GridCoordinate(1, 0); return true;
                    case GridDirection.North: offset = new GridCoordinate(0, 1); return true;
                    case GridDirection.West: offset = new GridCoordinate(-1, 0); return true;
                    case GridDirection.South: offset = new GridCoordinate(0, -1); return true;
                }
            }
            else
            {
                switch (direction)
                {
                    case GridDirection.East: offset = new GridCoordinate(1, 0); return true;
                    case GridDirection.SouthEast: offset = new GridCoordinate(1, -1); return true;
                    case GridDirection.SouthWest: offset = new GridCoordinate(0, -1); return true;
                    case GridDirection.West: offset = new GridCoordinate(-1, 0); return true;
                    case GridDirection.NorthWest: offset = new GridCoordinate(-1, 1); return true;
                    case GridDirection.NorthEast: offset = new GridCoordinate(0, 1); return true;
                }
            }

            offset = default;
            return false;
        }

        public static bool TryRotateDirection(GridDirection direction, GridShape shape, int steps, out GridDirection rotated)
        {
            GridDirection[] directions = shape == GridShape.Square ? SquareDirections : HexDirections;
            int index = System.Array.IndexOf(directions, direction);
            if (index < 0)
            {
                rotated = default;
                return false;
            }

            int count = directions.Length;
            rotated = directions[((index + steps) % count + count) % count];
            return true;
        }

        public static Vector2 CellToLocal(GridCoordinate coordinate, GridShape shape, float cellSize)
            => CellToLocal(new Vector2(coordinate.X, coordinate.Y), shape, cellSize);

        public static Vector2 CellToLocal(Vector2 coordinate, GridShape shape, float cellSize)
        {
            if (shape == GridShape.Square)
                return coordinate * cellSize;

            // Pointy-top axial hexes: X is q and Y is r.
            return new Vector2(
                cellSize * Sqrt3 * (coordinate.x + coordinate.y * 0.5f),
                cellSize * 1.5f * coordinate.y);
        }

        public static GridCoordinate RotateOffset(GridCoordinate offset, GridShape shape, int steps)
        {
            int turns = shape == GridShape.Square ? 4 : 6;
            steps = ((steps % turns) + turns) % turns;

            for (int i = 0; i < steps; i++)
            {
                offset = shape == GridShape.Square
                    ? new GridCoordinate(-offset.Y, offset.X)
                    : new GridCoordinate(-offset.Y, offset.X + offset.Y);
            }

            return offset;
        }

        public static float RotationDegrees(GridShape shape, int steps) =>
            (shape == GridShape.Square ? 90f : 60f) * steps;
    }
}
