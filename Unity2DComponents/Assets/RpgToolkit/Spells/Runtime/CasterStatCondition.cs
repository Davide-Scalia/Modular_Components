using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Condition/Caster Stat")]
    public sealed class CasterStatCondition : SpellCondition
    {
        [SerializeField] private ActorStat stat;
        [SerializeField] private NumericComparison comparison = NumericComparison.GreaterOrEqual;
        [SerializeField] private float value;

        public override bool Evaluate(SpellEvaluationContext context) =>
            context.TryGetCasterStat(stat, out float actual) &&
            SpellMath.Compare(actual, comparison, value);
    }
}
