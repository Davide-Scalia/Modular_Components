using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit
{
    [Serializable]
    public struct LootEntry
    {
        public LootItem Item;
        [Min(1)] public int Weight;
        [Min(1)] public int MinCount;
        [Min(1)] public int MaxCount;
    }

    public struct LootDrop
    {
        public LootItem Item;
        public int Count;
        public LootDrop(LootItem item, int count) { Item = item; Count = count; }
    }

    [CreateAssetMenu(menuName = "RPG Toolkit/Loot Pool")]
    public sealed class LootPool : ScriptableObject
    {
        [SerializeField, Min(0)] private int rolls = 1;
        [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

        public List<LootDrop> Roll(System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            List<LootDrop> result = new List<LootDrop>();
            long total = 0;
            foreach (LootEntry entry in entries)
                if (entry.Item != null && entry.Weight > 0 && entry.MinCount > 0 && entry.MaxCount >= entry.MinCount)
                    total += entry.Weight;
            if (total == 0) return result;
            for (int i = 0; i < rolls; i++)
            {
                long pick = (long)(random.NextDouble() * total);
                foreach (LootEntry entry in entries)
                {
                    if (entry.Item == null || entry.Weight <= 0 || entry.MinCount <= 0 || entry.MaxCount < entry.MinCount) continue;
                    pick -= entry.Weight;
                    if (pick >= 0) continue;
                    result.Add(new LootDrop(entry.Item, random.Next(entry.MinCount, entry.MaxCount + 1)));
                    break;
                }
            }
            return result;
        }
    }
}
