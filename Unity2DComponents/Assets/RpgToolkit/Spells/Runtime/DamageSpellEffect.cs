using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Effect/Damage")]
    public sealed class DamageSpellEffect : SpellEffect
    {
        [SerializeField, Min(0f)] private float multiplier = 1f;
        [SerializeField] private bool canCritical = true;

        public override void Apply(SpellEvaluationContext context) =>
            context.DamageTargets(context.GetSpellStat(SpellStat.Damage) * Mathf.Max(0f, multiplier),
                canCritical);
    }
}


    