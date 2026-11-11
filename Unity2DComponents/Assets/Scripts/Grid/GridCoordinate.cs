using System;
using UnityEngine;

namespace ModularGrid
{
    [Serializable]
    public struct GridCoordinate : IEquatable<GridCoordinate>
    {
        public int X;
        public int Y;

        public GridCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static GridCoordinate operator +(GridCoordinate left, GridCoordinate right) =>
            new GridCoordinate(left.X + right.X, left.Y + right.Y);

        public static bool operator ==(GridCoordinate left, GridCoordinate right) => left.Equals(right);
        public static bool operator !=(GridCoordinate left, GridCoordinate right) => !left.Equals(right);

        public bool Equals(GridCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoordinate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";
    }
}
