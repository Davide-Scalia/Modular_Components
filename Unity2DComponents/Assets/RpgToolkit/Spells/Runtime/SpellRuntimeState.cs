using System;
using UnityEngine;

namespace RpgToolkit.Spells
{
    public sealed class CasterStatSnapshot
    {
        private readonly float[] values = new float[Enum.GetValues(typeof(ActorStat)).Length];
        public bool HasStats { get; }
        public EntityId CasterId { get; }

        public CasterStatSnapshot(EntityStats stats, GameObject caster = null)
        {
            GameObject owner = caster != null ? caster : stats != null ? stats.gameObject : null;
            CasterId = owner != null ? owner.GetEntityId() : default;
            HasStats = stats != null;
            if (!HasStats) return;
            foreach (ActorStat stat in Enum.GetValues(typeof(ActorStat))) values[(int)stat] = stats.GetStat(stat);
        }

        public bool TryGet(ActorStat stat, out float value)
        {
            value = HasStats ? values[(int)stat] : 0f;
            return HasStats;
        }
    }

    public readonly struct SpellSpawnRequest
    {
        public SpellDefinition Definition { get; }
        public GameObject Caster { get; }
        public CasterStatSnapshot Snapshot { get; }
        public SpellCastFamily Family { get; }
        public int Depth { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }

        public SpellSpawnRequest(SpellDefinition definition, GameObject caster,
            CasterStatSnapshot snapshot, SpellCastFamily family, int depth, Vector2 position, Vector2 direction)
        {
            Definition = definition;
            Caster = caster;
            Snapshot = snapshot;
            Family = family;
            Depth = depth;
            Position = position;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }
    }
}
