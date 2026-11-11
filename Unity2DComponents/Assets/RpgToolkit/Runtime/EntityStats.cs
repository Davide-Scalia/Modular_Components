using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit
{
    public enum ActorStat
    {
        Attack,
        Defense,
        SpecialAttack,
        SpecialDefense,
        CritRate,
        CritDamage,
        Velocity,
        MaxMovement
    }

    [DisallowMultipleComponent]
    public sealed class EntityStats : MonoBehaviour
    {
        [SerializeField, Min(1)] private int attack = 10;
        [SerializeField, Min(0)] private int defense = 5;
        [SerializeField, Min(1)] private int specialAttack = 10;
        [SerializeField, Min(0)] private int specialDefense = 5;
        [SerializeField, Range(0, 100)] private int critRate = 5;
        [SerializeField, Min(100)] private int critDamage = 150;
        [SerializeField, Min(1)] private int velocity = 100;
        [SerializeField, Min(0)] private int maxMovement = 100;
        private readonly List<Modifier> modifiers = new List<Modifier>();

        public int Attack => Mathf.RoundToInt(GetStat(ActorStat.Attack));
        public int Defense => Mathf.RoundToInt(GetStat(ActorStat.Defense));
        public int SpecialAttack => Mathf.RoundToInt(GetStat(ActorStat.SpecialAttack));
        public int SpecialDefense => Mathf.RoundToInt(GetStat(ActorStat.SpecialDefense));
        public int CritRate => Mathf.RoundToInt(GetStat(ActorStat.CritRate));
        public int CritDamage => Mathf.RoundToInt(GetStat(ActorStat.CritDamage));
        public int Velocity => Mathf.RoundToInt(GetStat(ActorStat.Velocity));
        public int MaxMovement => Mathf.RoundToInt(GetStat(ActorStat.MaxMovement));

        public float GetStat(ActorStat stat)
        {
            RemoveExpiredModifiers();
            float flat = 0f;
            float percent = 0f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Stat != stat) continue;
                flat += modifiers[i].Flat;
                percent += modifiers[i].Percent;
            }
            float value = (GetBaseStat(stat) + flat) * (1f + percent);
            return stat switch
            {
                ActorStat.Attack or ActorStat.SpecialAttack or ActorStat.Velocity => Mathf.Max(1f, value),
                ActorStat.CritRate => Mathf.Clamp(value, 0f, 100f),
                ActorStat.CritDamage => Mathf.Max(100f, value),
                _ => Mathf.Max(0f, value)
            };
        }

        public void ApplyModifier(Object source, ActorStat stat, float flat, float percent, float duration = 0f)
        {
            if (source == null) return;
            float expiresAt = duration > 0f ? Time.time + duration : float.PositiveInfinity;
            for (int i = 0; i < modifiers.Count; i++)
            {
                Modifier current = modifiers[i];
                if (current.Source != source || current.Stat != stat) continue;
                current.Flat = flat;
                current.Percent = percent;
                current.ExpiresAt = expiresAt;
                modifiers[i] = current;
                return;
            }
            modifiers.Add(new Modifier(source, stat, flat, percent, expiresAt));
        }

        public void RemoveModifiers(Object source)
        {
            if (source == null) return;
            modifiers.RemoveAll(modifier => modifier.Source == source);
        }

        public void SetVelocity(int value) => velocity = Mathf.Max(1, value);
        public void SetMaxMovement(int value) => maxMovement = Mathf.Max(0, value);

        private float GetBaseStat(ActorStat stat) => stat switch
        {
            ActorStat.Attack => attack,
            ActorStat.Defense => defense,
            ActorStat.SpecialAttack => specialAttack,
            ActorStat.SpecialDefense => specialDefense,
            ActorStat.CritRate => critRate,
            ActorStat.CritDamage => critDamage,
            ActorStat.Velocity => velocity,
            ActorStat.MaxMovement => maxMovement,
            _ => 0f
        };

        private void Update() => RemoveExpiredModifiers();

        private void RemoveExpiredModifiers()
        {
            float now = Time.time;
            modifiers.RemoveAll(modifier => modifier.ExpiresAt <= now || modifier.Source == null);
        }

        private struct Modifier
        {
            public Object Source;
            public ActorStat Stat;
            public float Flat;
            public float Percent;
            public float ExpiresAt;

            public Modifier(Object source, ActorStat stat, float flat, float percent, float expiresAt)
            {
                Source = source;
                Stat = stat;
                Flat = flat;
                Percent = percent;
                ExpiresAt = expiresAt;
            }
        }
    }
}
