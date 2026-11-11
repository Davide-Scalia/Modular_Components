using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Motion/Ballistic")]
    public sealed class BallisticSpellMotion2D : SpellMotion2D
    {
        [SerializeField] private Vector2 acceleration;

        public override void Initialize(SpellEvaluationContext context, Vector2 direction)
        {
            Vector2 heading = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            context.Velocity = heading * context.GetSpellStat(SpellStat.Speed);
        }

        public override void Advance(SpellEvaluationContext context, float deltaTime)
        {
            context.Velocity += acceleration * deltaTime;
            context.Position += context.Velocity * deltaTime;
        }
    }
}
