using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Motion/Homing")]
    public sealed class HomingSpellMotion2D : SpellMotion2D
    {
        [SerializeField, Min(0.1f)] private float acquisitionRange = 8f;
        [SerializeField, Min(0f)] private float turnDegreesPerSecond = 180f;

        public override void Initialize(SpellEvaluationContext context, Vector2 direction)
        {
            Vector2 heading = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            context.Velocity = heading * context.GetSpellStat(SpellStat.Speed);
            context.AcquireNearestTarget(acquisitionRange);
        }

        public override void Advance(SpellEvaluationContext context, float deltaTime)
        {
            if (!context.HasValidHomingTarget(acquisitionRange)) context.AcquireNearestTarget(acquisitionRange);
            Vector2 heading = context.Velocity.sqrMagnitude > 0f ? context.Velocity.normalized : Vector2.right;
            if (context.HomingTarget != null)
            {
                Vector2 desired = ((Vector2)context.HomingTarget.bounds.center - context.Position).normalized;
                heading = Vector3.RotateTowards(heading, desired,
                    turnDegreesPerSecond * Mathf.Deg2Rad * deltaTime, 0f);
            }
            context.Velocity = heading.normalized * context.GetSpellStat(SpellStat.Speed);
            context.Position += context.Velocity * deltaTime;
        }
    }
}
