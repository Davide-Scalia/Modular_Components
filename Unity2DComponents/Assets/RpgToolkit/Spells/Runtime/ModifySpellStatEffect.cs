using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Effect/Modify Spell Stat")]
    public sealed class ModifySpellStatEffect : SpellEffect
    {
        [SerializeField] private SpellStat stat;
        [SerializeField] private SpellModifierMode mode;
        public bool IsValid => stat != SpellStat.Lifetime;
        [SerializeField] private float flat;
        [SerializeField, Tooltip("0.25 means +25%.")] private float percent;
        [SerializeField, Min(0f), Tooltip("Zero lasts for the remaining spell lifetime.")]
        private float duration;

        public override void Apply(SpellEvaluationContext context) =>
            context.ApplySpellModifier(this, stat, flat, percent, duration, mode);

        private void OnValidate()
        {
            if (!IsValid) Debug.LogError("Lifetime cannot be modified after a spell spawns. Choose another stat.", this);
        }
    }
}
