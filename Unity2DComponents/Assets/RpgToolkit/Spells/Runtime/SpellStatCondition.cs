using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Condition/Spell Stat Percentage")]
    public sealed class SpellStatCondition : SpellCondition
    {
        [SerializeField] private SpellStat stat;
        [SerializeField] private NumericComparison comparison = NumericComparison.GreaterOrEqual;
        [SerializeField, InspectorName("Fraction of Original Stat"),
         Tooltip("0.5 = 50%, 1.5 = 150%. Lifetime measures elapsed time as a fraction of fixed lifetime.")]
        private float value;

        public override bool Evaluate(SpellEvaluationContext context) => SpellMath.Compare(
            stat == SpellStat.Lifetime ? context.Age : context.GetSpellStat(stat),
            comparison, context.GetBaseSpellStat(stat) * value);
    }
}
