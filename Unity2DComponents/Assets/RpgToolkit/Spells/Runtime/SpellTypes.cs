using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    public enum SpellForm { Projectile, Area }
    public enum SpellStat { Damage, Speed, Lifetime, Radius }
    public enum SpellTrigger { OnSpawn, OnTick, OnHit, OnBounce, OnExpire, OnAreaPulse }
    public enum SpellRuleMode { Once, Repeat }
    public enum ConditionMode { All, Any }
    public enum NumericComparison { Less, LessOrEqual, Equal, GreaterOrEqual, Greater }
    public enum CasterStatReadMode { CapturedAtCast, Current }
    public enum SpawnDirection { Forward, CollisionNormal, Radial }

    [Serializable]
    public struct SpellBaseStats
    {
        [Min(0f)] public float Damage;
        [Min(0f)] public float Speed;
        [Min(0.01f)] public float Lifetime;
        [Min(0f)] public float Radius;

        public float Get(SpellStat stat) => stat switch
        {
            SpellStat.Damage => Mathf.Max(0f, Damage),
            SpellStat.Speed => Mathf.Max(0f, Speed),
            SpellStat.Lifetime => Mathf.Max(0.01f, Lifetime),
            SpellStat.Radius => Mathf.Max(0f, Radius),
            _ => 0f
        };
    }

    [Serializable]
    public sealed class SpellRule
    {
        [SerializeField] private SpellTrigger trigger;
        [SerializeField] private ConditionMode conditionMode;
        [SerializeField] private SpellCondition[] conditions = Array.Empty<SpellCondition>();
        [SerializeField] private SpellEffect[] effects = Array.Empty<SpellEffect>();
        [SerializeField] private SpellRuleMode mode;
        [SerializeField, Min(0f)] private float repeatInterval;

        public SpellTrigger Trigger => trigger;
        public ConditionMode ConditionMode => conditionMode;
        public IReadOnlyList<SpellCondition> Conditions => conditions ?? Array.Empty<SpellCondition>();
        public IReadOnlyList<SpellEffect> Effects => effects ?? Array.Empty<SpellEffect>();
        public SpellRuleMode Mode => mode;
        public float RepeatInterval => Mathf.Max(0f, repeatInterval);
    }

    public sealed class SpellCastBudget
    {
        private readonly int maximumCount;
        private readonly int maximumDepth;
        public int SpawnedCount { get; private set; }

        public SpellCastBudget(int maximumCount, int maximumDepth)
        {
            this.maximumCount = Mathf.Max(1, maximumCount);
            this.maximumDepth = Mathf.Max(0, maximumDepth);
        }

        public bool TryConsume(int depth)
        {
            if (depth > maximumDepth || SpawnedCount >= maximumCount) return false;
            SpawnedCount++;
            return true;
        }
    }

    public static class SpellMath
    {
        public static float Modified(float baseValue, float flat, float percent) =>
            (baseValue + flat) * (1f + percent);

        public static bool Compare(float left, NumericComparison comparison, float right) => comparison switch
        {
            NumericComparison.Less => left < right,
            NumericComparison.LessOrEqual => left <= right,
            NumericComparison.Equal => Mathf.Approximately(left, right),
            NumericComparison.GreaterOrEqual => left >= right,
            NumericComparison.Greater => left > right,
            _ => false
        };
    }
}
