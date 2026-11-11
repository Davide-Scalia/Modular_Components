using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RpgToolkit
{
    [Flags]
    public enum CardinalEdge { None = 0, North = 1, East = 2, South = 4, West = 8, All = 15 }

    [Serializable]
    public struct TopDownTileRule
    {
        public TileBase Tile;
        public bool Walkable;
        [Min(1)] public int EntryCost;
        public CardinalEdge OpenEdges;
        public bool BlocksSight;
    }

    [CreateAssetMenu(menuName = "RPG Toolkit/Top Down Tile Rules")]
    public sealed class TopDownTileRules : ScriptableObject
    {
        [SerializeField] private List<TopDownTileRule> entries = new List<TopDownTileRule>();

        public bool TryGet(TileBase tile, out TopDownTileRule rule)
        {
            foreach (TopDownTileRule entry in entries)
                if (entry.Tile == tile && tile != null)
                {
                    rule = entry;
                    return true;
                }
            rule = default;
            return false;
        }

        private void OnValidate()
        {
            HashSet<TileBase> seen = new HashSet<TileBase>();
            foreach (TopDownTileRule entry in entries)
            {
                if (entry.Tile != null && !seen.Add(entry.Tile))
                    Debug.LogWarning($"Duplicate gameplay rule for {entry.Tile.name}.", this);
                if (entry.Walkable && entry.EntryCost < 1)
                    Debug.LogWarning($"Walkable tile {entry.Tile?.name} needs a positive entry cost.", this);
            }
        }
    }
}
