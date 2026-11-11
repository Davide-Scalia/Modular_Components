using UnityEngine;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHitFeedback2D : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private DamageNumberPool2D damageNumberPool;
        [SerializeField] private Transform spawnAnchor;

        public void Configure(Health source, DamageNumberPool2D pool, Transform anchor = null)
        {
            if (isActiveAndEnabled && health != null) health.Damaged -= OnDamaged;
            health = source;
            damageNumberPool = pool;
            spawnAnchor = anchor;
            if (isActiveAndEnabled && health != null) health.Damaged += OnDamaged;
        }

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (damageNumberPool == null) damageNumberPool = FindAnyObjectByType<DamageNumberPool2D>();
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void OnDamaged(DamageResult result)
        {
            if (result.Applied <= 0 || damageNumberPool == null) return;
            Vector2 position = spawnAnchor != null ? spawnAnchor.position : transform.position;
            damageNumberPool.Spawn(position, result.Applied, result.IsCritical);
        }
    }
}
