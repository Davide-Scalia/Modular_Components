using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    public sealed class SpellEvaluationContext
    {
        private readonly SpellRuntimeState state;
        private readonly SpellWorld2D world;
        private readonly bool preview;
        private Collider2D currentTarget;
        private Vector2 collisionPoint;
        private Vector2 collisionNormal;

        internal SpellEvaluationContext(SpellRuntimeState state, SpellWorld2D world, bool preview)
        {
            this.state = state;
            this.world = world;
            this.preview = preview;
        }

        public SpellDefinition Definition => state.Definition;
        public GameObject Caster => state.Caster;
        public float Age => state.Age;
        public bool IsPreview => preview;
        public Collider2D TargetCollider => currentTarget;
        public Collider2D HomingTarget => state.HomingTarget;
        public Vector2 CollisionPoint => collisionPoint;
        public Vector2 CollisionNormal => collisionNormal;
        public IReadOnlyList<Collider2D> AreaTargets => state.AreaTargets;
        internal SpellRuntimeState State => state;

        public Vector2 Position
        {
            get => state.Position;
            set => state.Position = value;
        }

        public Vector2 Velocity
        {
            get => state.Velocity;
            set => state.Velocity = value;
        }

        public float GetSpellStat(SpellStat stat) => state.GetStat(stat);

        public bool TryGetCasterStat(ActorStat stat, CasterStatReadMode mode, out float value)
        {
            if (mode == CasterStatReadMode.CapturedAtCast)
            {
                value = 0f;
                return state.Snapshot != null && state.Snapshot.TryGet(stat, out value);
            }
            if (state.Caster != null && state.CasterStats != null)
            {
                value = state.CasterStats.GetStat(stat);
                return true;
            }
            value = 0f;
            return false;
        }

        public void ApplySpellModifier(Object source, SpellStat stat, float flat, float percent, float duration = 0f) =>
            state.ApplyModifier(source, stat, flat, percent, duration);

        public void DamageTargets(float amount, bool canCritical = false,
            CasterStatReadMode statReadMode = CasterStatReadMode.CapturedAtCast)
        {
            if (preview || amount <= 0f) return;
            state.PulseTargets.Clear();
            if (currentTarget != null) Damage(currentTarget, amount, canCritical, statReadMode);
            else
                for (int i = 0; i < state.AreaTargets.Count; i++)
                    Damage(state.AreaTargets[i], amount, canCritical, statReadMode);
        }

        public void SpawnChild(SpellDefinition definition, Vector2 direction)
        {
            if (preview || world == null || definition == null || state.Budget == null) return;
            world.TrySpawn(new SpellSpawnRequest(definition, state.Caster, state.CasterStats, state.Snapshot,
                state.Budget, state.Depth + 1, state.Position, direction), out _);
        }

        public void AcquireNearestTarget(float range)
        {
            state.HomingTarget = null;
            state.AreaTargets.Clear();
#pragma warning disable CS0618
            int count = Physics2D.OverlapCircleNonAlloc(state.Position, Mathf.Max(0f, range),
                state.OverlapBuffer, state.Definition.TargetLayers);
#pragma warning restore CS0618
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = state.OverlapBuffer[i];
                if (candidate == null || IsCaster(candidate)) continue;
                float distance = ((Vector2)candidate.bounds.center - state.Position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                state.HomingTarget = candidate;
            }
            System.Array.Clear(state.OverlapBuffer, 0, count);
        }

        public bool HasValidHomingTarget(float range)
        {
            Collider2D target = state.HomingTarget;
            return target != null && target.enabled && target.gameObject.activeInHierarchy && !IsCaster(target) &&
                   ((Vector2)target.bounds.center - state.Position).sqrMagnitude <= range * range;
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
            collisionPoint = state.Position;
            collisionNormal = Vector2.zero;
        }

        internal bool IsCaster(Collider2D collider) =>
            collider != null && state.Caster != null &&
            (collider.gameObject == state.Caster || collider.transform.IsChildOf(state.Caster.transform));

        internal UnityEngine.Object HitKey(Collider2D collider)
        {
            if (collider == null) return null;
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            return damageable as UnityEngine.Object ?? collider;
        }

        private void Damage(Collider2D collider, float amount, bool canCritical, CasterStatReadMode statReadMode)
        {
            if (collider == null || IsCaster(collider)) return;
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            UnityEngine.Object key = damageable as UnityEngine.Object;
            if (damageable == null || key == null || !damageable.IsAlive || !state.PulseTargets.Add(key)) return;

            bool isCritical = false;
            float finalAmount = amount;
            if (canCritical && TryGetCasterStat(ActorStat.CritRate, statReadMode, out float chance) &&
                TryGetCasterStat(ActorStat.CritDamage, statReadMode, out float criticalDamage) &&
                Random.value * 100f < chance)
            {
                isCritical = true;
                finalAmount *= Mathf.Max(100f, criticalDamage) / 100f;
            }

            int rounded = Mathf.Max(1, Mathf.RoundToInt(finalAmount));
            if (damageable is Health health) health.ApplyDamage(rounded, isCritical);
            else damageable.TakeDamage(rounded);
        }
    }
}
