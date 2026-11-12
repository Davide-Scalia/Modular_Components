using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class DamageOverTimeReceiver : MonoBehaviour
    {
        private readonly List<Status> statuses = new List<Status>();
        private Health health;
        internal int ActiveCount => statuses.Count;

        private void OnEnable()
        {
            SubscribeToDeath();
        }

        internal void Apply(DamageOverTimeSpellEffect source, EntityId casterId, IDamageable target,
            CapturedSpellDamage damage, float duration, float interval)
        {
            if (!isActiveAndEnabled || target == null || !target.IsAlive) return;
            SubscribeToDeath();
            interval = Mathf.Max(SpellMath.MinimumInterval, interval);
            duration = Mathf.Max(SpellMath.MinimumInterval, duration);
            foreach (Status status in statuses)
            {
                if (status.Source != source || status.CasterId != casterId) continue;
                status.Damage = damage;
                status.Remaining = duration;
                status.Interval = interval;
                // Refresh duration without indefinitely postponing damage under frequent reapplication.
                status.UntilTick = Mathf.Min(status.UntilTick, interval);
                return;
            }
            statuses.Add(new Status
            {
                Source = source, CasterId = casterId, Target = target,
                TargetObject = target as Object, Damage = damage,
                Remaining = duration, Interval = interval, UntilTick = interval
            });
        }

        internal void Simulate(float deltaTime)
        {
            if (!isActiveAndEnabled) return;
            float delta = Mathf.Max(0f, deltaTime);
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                Status status = statuses[i];
                if (status.TargetObject == null || !status.Target.IsAlive)
                {
                    statuses.RemoveAt(i);
                    continue;
                }
                float step = Mathf.Min(delta, status.Remaining);
                status.Remaining -= step;
                status.UntilTick -= step;
                if (status.UntilTick <= 0.00001f)
                {
                    status.UntilTick = status.Interval;
                    status.Damage.Apply(status.Target);
                    // Damage can synchronously kill/disable the owner and clear the list.
                    if (!status.Target.IsAlive) { Clear(); return; }
                    if (!isActiveAndEnabled || statuses.Count == 0) return;
                }
                if (status.Remaining <= 0.00001f) statuses.RemoveAt(i);
            }
        }

        private void SubscribeToDeath()
        {
            if (health != null) return;
            health = GetComponent<Health>();
            if (health != null) health.Died += Clear;
        }

        private void FixedUpdate() => Simulate(Time.fixedDeltaTime);
        private void Clear() => statuses.Clear();
        private void OnDisable()
        {
            if (health != null) health.Died -= Clear;
            health = null;
            Clear();
        }

        private sealed class Status
        {
            public DamageOverTimeSpellEffect Source;
            public EntityId CasterId;
            public IDamageable Target;
            public Object TargetObject;
            public CapturedSpellDamage Damage;
            public float Remaining;
            public float Interval;
            public float UntilTick;
        }
    }
}
