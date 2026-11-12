using System;
using System.Collections.Generic;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace RpgToolkit.Spells
{
    public enum SpellForm { Projectile, Area }
    public enum SpellStat { Damage, Speed, Lifetime, Radius}
    public enum SpellTrigger { OnSpawn, OnTick, OnHit, OnBounce, OnExpire, OnAreaPulse }
    public enum SpellRuleMode { Once = 0, EveryEvent = 1, RepeatOnInterval = 2 }
    public enum SpellModifierMode { Refresh = 0, Add = 1 }
    public enum ConditionMode { All, Any }
    public enum NumericComparison { Less, LessOrEqual, Equal, GreaterOrEqual, Greater }
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
        [SerializeField, Min(SpellMath.MinimumInterval)] private float repeatInterval = 0.25f;

        public SpellTrigger Trigger => trigger;
        public ConditionMode ConditionMode => conditionMode;
        public IReadOnlyList<SpellCondition> Conditions => conditions ?? Array.Empty<SpellCondition>();
        public IReadOnlyList<SpellEffect> Effects => effects ?? Array.Empty<SpellEffect>();
        public SpellRuleMode Mode => mode;
        public float RepeatInterval => Mathf.Max(SpellMath.MinimumInterval, repeatInterval);
    }

    public sealed class SpellCastFamily
    {
        private readonly int maximumCount;
        private readonly int maximumDepth;
        private readonly HashSet<string> reportedFailures = new HashSet<string>();
        public int ActiveCount { get; private set; }

        public SpellCastFamily(int maximumCount, int maximumDepth)
        {
            this.maximumCount = Mathf.Max(1, maximumCount);
            this.maximumDepth = Mathf.Max(0, maximumDepth);
        }

        internal bool TryAcquire(int depth, out string reason)
        {
            reason = depth < 0 || depth > maximumDepth ? "child depth limit reached" :
                ActiveCount >= maximumCount ? "concurrent cast-family limit reached" : null;
            if (reason != null) return false;
            ActiveCount++;
            return true;
        }

        internal void Release() => ActiveCount = Mathf.Max(0, ActiveCount - 1);

        internal void ReportFailure(string reason, UnityEngine.Object source)
        {
            if (reportedFailures.Add(reason))
                Debug.LogWarning($"Spell spawn rejected: {reason}. Further identical warnings for this cast are suppressed.", source);
        }
    }

    public static class SpellMath
    {
        public const float MinimumInterval = 0.1f;
        public static float Modified(float baseValue, float flat, float percent) =>
            (baseValue + flat) * (1f + percent);

        public static bool Compare(float left, NumericComparison comparison, float right) => comparison switch
        {
            NumericComparison.Less => left < right,
            NumericComparison.LessOrEqual => left <= right || Mathf.Approximately(left, right),
            NumericComparison.Equal => Mathf.Approximately(left, right),
            NumericComparison.GreaterOrEqual => left >= right || Mathf.Approximately(left, right),
            NumericComparison.Greater => left > right,
            _ => false
        };
    }
}
