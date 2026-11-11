using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit
{
    // Session inventory handoff; the full inventory UI can consume these drops later.
    public sealed class RunLootLedger : MonoBehaviour
    {
        private static RunLootLedger instance;
        private readonly List<LootDrop> drops = new List<LootDrop>();
        public static RunLootLedger Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject owner = new GameObject("RPG Run Loot");
                    instance = owner.AddComponent<RunLootLedger>();
                    DontDestroyOnLoad(owner);
                }
                return instance;
            }
        }
        public IReadOnlyList<LootDrop> Drops => drops;
        public void Add(IEnumerable<LootDrop> rewards) { if (rewards != null) drops.AddRange(rewards); }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
