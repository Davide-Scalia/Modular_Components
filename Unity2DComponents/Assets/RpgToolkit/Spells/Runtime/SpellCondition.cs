using UnityEngine;

namespace RpgToolkit.Spells
{
    public abstract class SpellCondition : ScriptableObject
    {
        public abstract bool Evaluate(SpellEvaluationContext context);
    }
}
