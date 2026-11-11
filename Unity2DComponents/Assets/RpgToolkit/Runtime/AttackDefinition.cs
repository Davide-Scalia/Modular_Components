using UnityEngine;

namespace RpgToolkit
{
    public enum DamageKind { Physical, Magical }

    [CreateAssetMenu(menuName = "RPG Toolkit/Attack")]
    public sealed class AttackDefinition : ScriptableObject
    {
        [SerializeField] private DamageKind damageKind;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private int scalingPercent = 100;
        [SerializeField, Min(1)] private int preparationCost = 10;
        [SerializeField, Min(1)] private int completionCost = 20;
        [SerializeField] private AttackPattern preparationPattern;
        [SerializeField, Min(0)] private int preparationHits;
        [SerializeField] private AttackPattern completionPattern;
        [SerializeField, Min(1)] private int completionHits = 1;
        public DamageKind DamageKind => damageKind;
        public Sprite Icon => icon;
        public int ScalingPercent => Mathf.Max(0, scalingPercent);
        public int PreparationCost => Mathf.Max(1, preparationCost);
        public int CompletionCost => Mathf.Max(1, completionCost);
        public AttackPattern PreparationPattern => preparationPattern;
        public int PreparationHits => Mathf.Max(0, preparationHits);
        public AttackPattern CompletionPattern => completionPattern;
        public int CompletionHits => Mathf.Max(1, completionHits);
    }
}
