using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Effect/Spawn Spell")]
    public sealed class SpawnSpellEffect : SpellEffect
    {
        [SerializeField] private SpellDefinition spell;
        [SerializeField] private SpawnDirection direction;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField, Range(0f, 360f)] private float arc;

        public override void Apply(SpellEvaluationContext context)
        {
            if (spell == null) return;
            int total = Mathf.Max(1, count);
            Vector2 baseDirection = direction switch
            {
                SpawnDirection.CollisionNormal when context.CollisionNormal.sqrMagnitude > 0f => context.CollisionNormal,
                _ when context.Velocity.sqrMagnitude > 0f => context.Velocity.normalized,
                _ => Vector2.right
            };
            for (int i = 0; i < total; i++)
            {
                float angle = direction == SpawnDirection.Radial
                    ? i * 360f / total
                    : total == 1 ? 0f : Mathf.Lerp(-arc * 0.5f, arc * 0.5f, i / (total - 1f));
                Vector2 childDirection = Quaternion.Euler(0f, 0f, angle) * baseDirection;
                context.SpawnChild(spell, childDirection);
            }
        }
    }
}
