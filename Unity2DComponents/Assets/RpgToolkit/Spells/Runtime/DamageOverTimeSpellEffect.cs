using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Effect/Damage Over Time")]
    public sealed class DamageOverTimeSpellEffect : SpellEffect
    {
        [SerializeField, Min(0f)] private float multiplier = 1f;
        [SerializeField] private bool canCritical = true;
        [SerializeField, Min(SpellMath.MinimumInterval)] private float duration = 3f;
        [SerializeField, Min(SpellMath.MinimumInterval)] private float interval = 0.25f;

        public override void Apply(SpellEvaluationContext context) => context.ApplyDamageOverTime(this,
            context.GetSpellStat(SpellStat.Damage) * Mathf.Max(0f, multiplier), canCritical,
            Mathf.Max(SpellMath.MinimumInterval, duration), Mathf.Max(SpellMath.MinimumInterval, interval));
    }
}
