using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Condition/Lifetime")]
    public sealed class LifetimeSpellCondition : SpellCondition
    {
        [SerializeField] private NumericComparison comparison = NumericComparison.GreaterOrEqual;
        [SerializeField, Min(0f), InspectorName("Elapsed Seconds")] private float seconds;

        public override bool Evaluate(SpellEvaluationContext context) =>
            SpellMath.Compare(context.Age, comparison, Mathf.Max(0f, seconds));
    }
}
