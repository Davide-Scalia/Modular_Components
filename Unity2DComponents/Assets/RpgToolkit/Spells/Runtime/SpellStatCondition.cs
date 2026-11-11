using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Condition/Spell Stat")]
    public sealed class SpellStatCondition : SpellCondition
    {
        [SerializeField] private SpellStat stat;
        [SerializeField] private NumericComparison comparison = NumericComparison.GreaterOrEqual;
        [SerializeField] private float value;

        public override bool Evaluate(SpellEvaluationContext context) 
        {
            Debug.Log("STAT IS" + context.GetSpellStat(stat));
            return SpellMath.Compare(context.GetSpellStat(stat), comparison, value);
        }
            

    }
}
