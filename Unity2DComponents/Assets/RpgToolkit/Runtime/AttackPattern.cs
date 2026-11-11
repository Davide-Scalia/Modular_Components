using System.Collections.Generic;
using ModularGrid;
using UnityEngine;

namespace RpgToolkit
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Attack Pattern")]
    public sealed class AttackPattern : ScriptableObject
    {
        [SerializeField, Min(1)] private int width = 3;
        [SerializeField, Min(1)] private int height = 3;
        [SerializeField] private bool[] cells = new bool[9];
        public int Width => width;
        public int Height => height;

        public IEnumerable<GridCoordinate> Cells(GridCoordinate origin, Vector2Int facing)
        {
            for (int y = 0; y < height; y++)
                for (int x = -width / 2; x <= width / 2; x++)
                {
                    if (!Contains(x, y)) continue;
                    yield return new GridCoordinate(origin.X + x * facing.y + y * facing.x,
                        origin.Y + y * facing.y - x * facing.x);
                }
        }

        public bool Contains(int x, int y)
        {
            if (y < 0 || y >= height || x < -width / 2 || x > width / 2 || (x == 0 && y == 0)) return false;
            int index = y * width + x + width / 2;
            return cells != null && index < cells.Length && cells[index];
        }

        private void OnValidate()
        {
            width = Mathf.Clamp(width | 1, 1, 21);
            height = Mathf.Clamp(height, 1, 21);
            if (cells != null && cells.Length == width * height) return;
            bool[] resized = new bool[width * height];
            if (cells != null) System.Array.Copy(cells, resized, Mathf.Min(cells.Length, resized.Length));
            cells = resized;
        }
    }
}
