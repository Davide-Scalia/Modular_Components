using UnityEngine;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class SpellCaster2D : MonoBehaviour
    {
        [SerializeField] private SpellWorld2D world;
        [SerializeField, Min(1)] private int maximumSpellsPerCast = 64;
        [SerializeField, Min(0)] private int maximumSpawnDepth = 8;
        private EntityStats stats;

        public SpellWorld2D World => world;

        private void Awake() => stats = GetComponent<EntityStats>();

        public bool TryCast(SpellDefinition definition, Vector2 origin, Vector2 direction)
        {
            if (world == null || definition == null || direction.sqrMagnitude <= 0f) return false;
            if (stats == null) stats = GetComponent<EntityStats>();
            CasterStatSnapshot snapshot = new CasterStatSnapshot(stats);
            SpellCastBudget budget = new SpellCastBudget(maximumSpellsPerCast, maximumSpawnDepth);
            return world.TrySpawn(new SpellSpawnRequest(definition, gameObject, stats, snapshot, budget, 0,
                origin, direction), out _);
        }

        public bool TryCast(SpellDefinition definition, Vector2 direction) =>
            TryCast(definition, transform.position, direction);
    }
}
