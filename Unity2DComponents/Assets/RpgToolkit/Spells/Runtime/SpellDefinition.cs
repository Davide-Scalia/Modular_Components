using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Spell")]
    public sealed class SpellDefinition : ScriptableObject
    {
        [SerializeField] private SpellForm form;
        [SerializeField] private SpellInstance2D runtimePrefab;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private SpellBaseStats baseStats = new SpellBaseStats
        {
            Damage = 10f,
            Speed = 8f,
            Lifetime = 2f,
            Radius = 0.15f
        };
        [SerializeField] private SpellMotion2D motion;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField, Min(0)] private int pierces;
        [SerializeField, Min(0)] private int bounces;
        [SerializeField, Min(0.01f)] private float areaPulseInterval = 0.25f;
        [SerializeField] private SpellRule[] rules = Array.Empty<SpellRule>();

        public SpellForm Form => form;
        public SpellInstance2D RuntimePrefab => runtimePrefab;
        public Color Color => color;
        public SpellBaseStats BaseStats => baseStats;
        public SpellMotion2D Motion => motion;
        public LayerMask TargetLayers => targetLayers;
        public LayerMask ObstacleLayers => obstacleLayers;
        public int Pierces => Mathf.Max(0, pierces);
        public int Bounces => Mathf.Max(0, bounces);
        public float AreaPulseInterval => Mathf.Max(0.01f, areaPulseInterval);
        public IReadOnlyList<SpellRule> Rules => rules ?? Array.Empty<SpellRule>();

        private void OnValidate()
        {
            baseStats.Damage = Mathf.Max(0f, baseStats.Damage);
            baseStats.Speed = Mathf.Max(0f, baseStats.Speed);
            baseStats.Lifetime = Mathf.Max(0.01f, baseStats.Lifetime);
            baseStats.Radius = Mathf.Max(0f, baseStats.Radius);
            pierces = Mathf.Max(0, pierces);
            bounces = Mathf.Max(0, bounces);
            areaPulseInterval = Mathf.Max(0.01f, areaPulseInterval);
        }
    }
}
