using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RpgToolkit.Spells
{
    // Per-instance data and the API used by conditions, motion and effects.
    public sealed class SpellEvaluationContext
    {
        private readonly SpellWorld2D world;
        private readonly CasterStatSnapshot snapshot;
        private readonly SpellBaseStats baseStats;
        private readonly EntityId casterId;
        private readonly List<SpellModifier> modifiers = new List<SpellModifier>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        private readonly List<Collider2D> areaTargets = new List<Collider2D>(16);
        private readonly HashSet<Object> effectTargets = new HashSet<Object>();
        private Collider2D currentTarget;
        private Vector2 collisionPoint;
        private Vector2 collisionNormal;
        private float lastEffectiveSpeed;

        public SpellDefinition Definition { get; }
        public GameObject Caster { get; }
        public float Age { get; internal set; }
        public float Lifetime => baseStats.Get(SpellStat.Lifetime);
        public bool IsPreview { get; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Transform Parent { get; set; }
        public Collider2D TargetCollider => currentTarget;
        public Collider2D HomingTarget { get; private set; }
        public Vector2 CollisionPoint => collisionPoint;
        public Vector2 CollisionNormal => collisionNormal;
        public IReadOnlyList<Collider2D> AreaTargets => areaTargets;
        internal SpellCastFamily Family { get; }
        internal int Depth { get; }
        internal int PiercesRemaining;
        internal int BouncesRemaining;
        internal float NextAreaPulse;
        internal float NextConditionPoll;
        internal int StepIndex;
        internal readonly SpellRuleState[] RuleStates;
        internal readonly HashSet<Object> HitTargets = new HashSet<Object>();
        internal readonly List<RaycastHit2D> PhysicsHits = new List<RaycastHit2D>(16);

        internal SpellEvaluationContext(in SpellSpawnRequest request, SpellWorld2D world, bool preview)
        {
            this.world = world;
            Definition = request.Definition;
            Caster = request.Caster;
            casterId = request.Snapshot?.CasterId ?? (Caster != null ? Caster.GetEntityId() : default);
            snapshot = request.Snapshot;
            Family = request.Family;
            Depth = request.Depth;
            Position = request.Position;
            IsPreview = preview;
            baseStats = Definition.BaseStats;
            lastEffectiveSpeed = GetBaseSpellStat(SpellStat.Speed);
            PiercesRemaining = Definition.Pierces;
            BouncesRemaining = Definition.Bounces;
            RuleStates = new SpellRuleState[Definition.Rules.Count];
        }

        public float GetBaseSpellStat(SpellStat stat) => baseStats.Get(stat);

        public float GetSpellStat(SpellStat stat)
        {
            if (stat == SpellStat.Lifetime) return Lifetime;
            modifiers.RemoveAll(modifier => modifier.ExpiresAt <= Age);
            float flat = 0f;
            float percent = 0f;
            foreach (SpellModifier modifier in modifiers)
            {
                if (modifier.Stat != stat) continue;
                flat += modifier.Flat;
                percent += modifier.Percent;
            }
            return Mathf.Max(0f, SpellMath.Modified(GetBaseSpellStat(stat), flat, percent));
        }

        public bool TryGetCasterStat(ActorStat stat, out float value)
        {
            value = 0f;
            return snapshot != null && snapshot.TryGet(stat, out value);
        }

        public void ApplySpellModifier(Object source, SpellStat stat, float flat, float percent,
            float duration = 0f, SpellModifierMode mode = SpellModifierMode.Refresh)
        {
            if (stat == SpellStat.Lifetime)
                throw new InvalidOperationException("A spawned spell's lifetime is fixed. Lifetime modifiers are invalid.");
            if (source == null) return;
            SpellModifier replacement = new SpellModifier
            {
                Source = source, Stat = stat, Flat = flat, Percent = percent,
                ExpiresAt = duration > 0f ? Age + duration : float.PositiveInfinity
            };
            if (mode == SpellModifierMode.Refresh)
                for (int i = 0; i < modifiers.Count; i++)
                    if (modifiers[i].Source == source && modifiers[i].Stat == stat)
                    {
                        modifiers[i] = replacement;
                        return;
                    }
            modifiers.Add(replacement);
        }

        internal void SyncSpeed()
        {
            float speed = GetSpellStat(SpellStat.Speed);
            if (Velocity.sqrMagnitude > 0f && lastEffectiveSpeed > 0f && !Mathf.Approximately(speed, lastEffectiveSpeed))
                Velocity *= speed / lastEffectiveSpeed;
            lastEffectiveSpeed = speed;
        }

        internal void QueryArea()
        {
            areaTargets.Clear();
            effectTargets.Clear();
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(Definition.TargetLayers);
            filter.useTriggers = true;
            Physics2D.OverlapCircle(Position, GetSpellStat(SpellStat.Radius), filter, overlaps);
            foreach (Collider2D target in overlaps)
                if (target != null && !IsCaster(target) && effectTargets.Add(HitKey(target))) areaTargets.Add(target);
            overlaps.Clear();
        }

        public void DamageTargets(float amount, bool canCritical = false)
        {
            if (IsPreview || amount <= 0f) return;
            CapturedSpellDamage damage = CaptureDamage(amount, canCritical);
            ForEachTarget(target => damage.Apply(target));
        }

        public void ApplyDamageOverTime(DamageOverTimeSpellEffect effect, float amount,
            bool canCritical, float duration, float interval)
        {
            if (IsPreview || effect == null || amount <= 0f) return;
            CapturedSpellDamage damage = CaptureDamage(amount, canCritical);
            ForEachTarget(target =>
            {
                Component component = target as Component;
                if (component == null) return;
                DamageOverTimeReceiver receiver = component.GetComponent<DamageOverTimeReceiver>();
                if (receiver == null) receiver = component.gameObject.AddComponent<DamageOverTimeReceiver>();
                receiver.Apply(effect, casterId, target, damage, duration, interval);
            });
        }

        private CapturedSpellDamage CaptureDamage(float amount, bool canCritical)
        {
            TryGetCasterStat(ActorStat.CritRate, out float chance);
            TryGetCasterStat(ActorStat.CritDamage, out float multiplier);
            return new CapturedSpellDamage(amount, canCritical ? chance : 0f, multiplier);
        }

        private void ForEachTarget(Action<IDamageable> action)
        {
            effectTargets.Clear();
            if (currentTarget != null) Visit(currentTarget);
            else if (Definition.Form == SpellForm.Area)
                for (int i = 0; i < areaTargets.Count; i++) Visit(areaTargets[i]);

            void Visit(Collider2D collider)
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || IsCaster(collider)) return;
                IDamageable target = collider.GetComponentInParent<IDamageable>();
                Object key = target as Object;
                if (key != null && target.IsAlive && effectTargets.Add(key)) action(target);
            }
        }

        public void SpawnChild(SpellDefinition definition, Vector2 direction)
        {
            if (IsPreview || world == null || Family == null) return;
            if (!world.TrySpawn(new SpellSpawnRequest(definition, Caster, snapshot,
                Family, Depth + 1, Position, direction), out _, out string reason))
                Family.ReportFailure(reason, Definition);
        }

        public void AcquireNearestTarget(float range)
        {
            HomingTarget = null;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(Definition.TargetLayers);
            filter.useTriggers = true;
            Physics2D.OverlapCircle(Position, Mathf.Max(0f, range), filter, overlaps);
            int count = overlaps.Count;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = overlaps[i];
                if (candidate == null || IsCaster(candidate)) continue;
                float distance = ((Vector2)candidate.bounds.center - Position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                HomingTarget = candidate;
            }
            overlaps.Clear();
        }

        public bool HasValidHomingTarget(float range)
        {
            Collider2D target = HomingTarget;
            return target != null && target.enabled && target.gameObject.activeInHierarchy && !IsCaster(target) &&
                   ((Vector2)target.bounds.center - Position).sqrMagnitude <= range * range;
        }

        internal void SetCollision(Collider2D target, Vector2 point, Vector2 normal)
        {
            currentTarget = target;
            collisionPoint = point;
            collisionNormal = normal;
        }

        internal void ClearCollision()
        {
            currentTarget = null;
            collisionPoint = Position;
            collisionNormal = Vector2.zero;
        }

        internal bool IsCaster(Collider2D collider) =>
            collider != null && Caster != null &&
            (collider.gameObject == Caster || collider.transform.IsChildOf(Caster.transform));

        internal UnityEngine.Object HitKey(Collider2D collider)
        {
            if (collider == null) return null;
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            return damageable as UnityEngine.Object ?? collider;
        }

    }

    internal struct SpellModifier
    {
        public Object Source;
        public SpellStat Stat;
        public float Flat;
        public float Percent;
        public float ExpiresAt;
    }

    internal struct SpellRuleState
    {
        public bool Fired;
        public float NextAllowedTime;
        public int LastExecutionStep;
        public Collider2D Target;
        public Vector2 CollisionPoint;
        public Vector2 CollisionNormal;
    }

    internal readonly struct CapturedSpellDamage
    {
        private readonly float amount;
        private readonly float criticalChance;
        private readonly float criticalMultiplier;

        public CapturedSpellDamage(float amount, float criticalChance, float criticalMultiplier)
        {
            this.amount = amount;
            this.criticalChance = criticalChance;
            this.criticalMultiplier = criticalMultiplier;
        }

        public void Apply(IDamageable target)
        {
            if (target == null || !target.IsAlive) return;
            bool critical = criticalChance > 0f && UnityEngine.Random.value * 100f < criticalChance;
            float value = critical ? amount * Mathf.Max(100f, criticalMultiplier) / 100f : amount;
            int rounded = Mathf.Max(1, Mathf.RoundToInt(value));
            if (target is Health health) health.ApplyDamage(rounded, critical);
            else target.TakeDamage(rounded);
        }
    }
}
