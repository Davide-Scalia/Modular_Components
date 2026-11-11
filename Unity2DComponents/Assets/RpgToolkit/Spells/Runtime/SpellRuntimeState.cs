using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    public sealed class CasterStatSnapshot
    {
        private readonly float[] values = new float[Enum.GetValues(typeof(ActorStat)).Length];
        public bool HasStats { get; }

        public CasterStatSnapshot(EntityStats stats)
        {
            HasStats = stats != null;
            if (!HasStats) return;
            foreach (ActorStat stat in Enum.GetValues(typeof(ActorStat))) values[(int)stat] = stats.GetStat(stat);
        }

        public bool TryGet(ActorStat stat, out float value)
        {
            value = HasStats ? values[(int)stat] : 0f;
            return HasStats;
        }
    }

    internal struct SpellModifier
    {
        public UnityEngine.Object Source;
        public SpellStat Stat;
        public float Flat;
        public float Percent;
        public float ExpiresAt;
    }

    internal struct SpellRuleState
    {
        public bool Fired;
        public float NextAllowedTime;
    }

    internal sealed class SpellRuntimeState
    {
        public SpellDefinition Definition;
        public GameObject Caster;
        public EntityStats CasterStats;
        public CasterStatSnapshot Snapshot;
        public SpellCastBudget Budget;
        public int Depth;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Age;
        public float LastEffectiveSpeed;
        public int PiercesRemaining;
        public int BouncesRemaining;
        public float NextAreaPulse;
        public Collider2D HomingTarget;
        public SpellRuleState[] RuleStates = Array.Empty<SpellRuleState>();
        public readonly List<SpellModifier> Modifiers = new List<SpellModifier>();
        public readonly HashSet<UnityEngine.Object> HitTargets = new HashSet<UnityEngine.Object>();
        public readonly List<RaycastHit2D> PhysicsHits = new List<RaycastHit2D>(16);
        public readonly List<Collider2D> AreaTargets = new List<Collider2D>(16);
        // ponytail: 64 overlaps cover the intended moderate scenes; grow this only if profiling shows saturation.
        public readonly Collider2D[] OverlapBuffer = new Collider2D[64];
        public readonly HashSet<UnityEngine.Object> PulseTargets = new HashSet<UnityEngine.Object>();

        public void Initialize(in SpellSpawnRequest request)
        {
            Definition = request.Definition;
            Caster = request.Caster;
            CasterStats = request.CasterStats;
            Snapshot = request.Snapshot;
            Budget = request.Budget;
            Depth = request.Depth;
            Position = request.Position;
            Velocity = Vector2.zero;
            Age = 0f;
            LastEffectiveSpeed = Definition.BaseStats.Get(SpellStat.Speed);
            PiercesRemaining = Definition.Pierces;
            BouncesRemaining = Definition.Bounces;
            NextAreaPulse = 0f;
            HomingTarget = null;
            RuleStates = new SpellRuleState[Definition.Rules.Count];
            Modifiers.Clear();
            HitTargets.Clear();
            PhysicsHits.Clear();
            AreaTargets.Clear();
            Array.Clear(OverlapBuffer, 0, OverlapBuffer.Length);
            PulseTargets.Clear();
        }

        public float GetStat(SpellStat stat)
        {
            RemoveExpiredModifiers();
            float flat = 0f;
            float percent = 0f;
            for (int i = 0; i < Modifiers.Count; i++)
            {
                SpellModifier modifier = Modifiers[i];
                if (modifier.Stat != stat) continue;
                flat += modifier.Flat;
                percent += modifier.Percent;
            }
            float value = SpellMath.Modified(Definition.BaseStats.Get(stat), flat, percent);
            Debug.Log($"STAT IS {value}");
            return stat == SpellStat.Lifetime ? Mathf.Max(0.01f, value) : Mathf.Max(0f, value);
        }

        public void ApplyModifier(UnityEngine.Object source, SpellStat stat, float flat, float percent, float duration)
        {
            if (source == null) return;
            float expiresAt = duration > 0f ? Age + duration : float.PositiveInfinity;
            for (int i = 0; i < Modifiers.Count; i++)
            {
                SpellModifier current = Modifiers[i];
                if (current.Source != source || current.Stat != stat) continue;
                current.Flat = flat;
                current.Percent = percent;
                current.ExpiresAt = expiresAt;
                Modifiers[i] = current;
                return;
            }
            Modifiers.Add(new SpellModifier
            {
                Source = source,
                Stat = stat,
                Flat = flat,
                Percent = percent,
                ExpiresAt = expiresAt
            });
        }

        public void SyncSpeed()
        {
            float speed = GetStat(SpellStat.Speed);
            if (Velocity.sqrMagnitude > 0f && LastEffectiveSpeed > 0f && !Mathf.Approximately(speed, LastEffectiveSpeed))
                Velocity *= speed / LastEffectiveSpeed;
            LastEffectiveSpeed = speed;
        }

        public void Reset()
        {
            Definition = null;
            Caster = null;
            CasterStats = null;
            Snapshot = null;
            Budget = null;
            HomingTarget = null;
            RuleStates = Array.Empty<SpellRuleState>();
            Modifiers.Clear();
            HitTargets.Clear();
            PhysicsHits.Clear();
            AreaTargets.Clear();
            Array.Clear(OverlapBuffer, 0, OverlapBuffer.Length);
            PulseTargets.Clear();
        }

        private void RemoveExpiredModifiers()
        {
            Modifiers.RemoveAll(modifier => modifier.Source == null || modifier.ExpiresAt <= Age);
        }
    }

    public readonly struct SpellSpawnRequest
    {
        public SpellDefinition Definition { get; }
        public GameObject Caster { get; }
        public EntityStats CasterStats { get; }
        public CasterStatSnapshot Snapshot { get; }
        public SpellCastBudget Budget { get; }
        public int Depth { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }

        public SpellSpawnRequest(SpellDefinition definition, GameObject caster, EntityStats casterStats,
            CasterStatSnapshot snapshot, SpellCastBudget budget, int depth, Vector2 position, Vector2 direction)
        {
            Definition = definition;
            Caster = caster;
            CasterStats = casterStats;
            Snapshot = snapshot;
            Budget = budget;
            Depth = depth;
            Position = position;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }
    }
}
