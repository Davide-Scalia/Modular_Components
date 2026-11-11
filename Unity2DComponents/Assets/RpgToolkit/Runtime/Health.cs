using System;
using UnityEngine;

namespace RpgToolkit
{
    public readonly struct DamageResult
    {
        public int Requested { get; }
        public int Applied { get; }
        public bool IsCritical { get; }

        public DamageResult(int requested, int applied, bool isCritical)
        {
            Requested = requested;
            Applied = applied;
            IsCritical = isCritical;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(int amount);
    }

    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maximum = 100;
        [SerializeField, Min(0)] private int current = 100;
        public int Maximum => Mathf.Max(1, maximum);
        public int Current => Mathf.Clamp(current, 0, Maximum);
        public bool IsAlive => Current > 0;
        public event Action<int, int> Changed;
        public event Action<DamageResult> Damaged;
        public event Action Died;

        private void Awake() => current = Mathf.Clamp(current, 0, Maximum);

        public void TakeDamage(int amount) => ApplyDamage(amount, false);

        public DamageResult ApplyDamage(int amount, bool isCritical = false)
        {
            if (amount <= 0 || !IsAlive) return new DamageResult(Mathf.Max(0, amount), 0, isCritical);
            int applied = Mathf.Min(Current, amount);
            current = Mathf.Max(0, current - applied);
            Changed?.Invoke(current, Maximum);
            Damaged?.Invoke(new DamageResult(amount, applied, isCritical));
            if (current != 0) return new DamageResult(amount, applied, isCritical);
            Died?.Invoke();
            CombatActor actor = GetComponent<CombatActor>();
            if (actor != null) actor.Timeline?.Remove(actor);
            return new DamageResult(amount, applied, isCritical);
        }

        public void Restore(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            current = Mathf.Min(Maximum, current + amount);
            Changed?.Invoke(current, Maximum);
        }
    }
}
