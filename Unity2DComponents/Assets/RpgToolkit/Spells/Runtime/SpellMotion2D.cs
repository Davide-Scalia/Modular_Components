using UnityEngine;

namespace RpgToolkit.Spells
{
    public abstract class SpellMotion2D : ScriptableObject
    {
        public abstract void Initialize(SpellEvaluationContext context, Vector2 direction);
        public abstract void Advance(SpellEvaluationContext context, float deltaTime);
    }
}
