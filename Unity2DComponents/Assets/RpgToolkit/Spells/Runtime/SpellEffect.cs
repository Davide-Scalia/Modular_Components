using UnityEngine;

namespace RpgToolkit.Spells
{
    public abstract class SpellEffect : ScriptableObject
    {
        public abstract void Apply(SpellEvaluationContext context);
    }
}
