using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class SpellWorld2D : MonoBehaviour
    {
        [SerializeField, Min(1)] private int defaultPoolCapacity = 16;
        [SerializeField, Min(1)] private int maximumPoolSize = 128;
        private readonly Dictionary<SpellDefinition, ObjectPool<SpellInstance2D>> pools =
            new Dictionary<SpellDefinition, ObjectPool<SpellInstance2D>>();

        public int ActiveCount { get; private set; }

        public bool TrySpawn(in SpellSpawnRequest request, out SpellInstance2D instance)
        {
            instance = null;
            if (request.Definition == null || request.Definition.RuntimePrefab == null || request.Budget == null ||
                !request.Budget.TryConsume(request.Depth)) return false;
            ObjectPool<SpellInstance2D> pool = GetPool(request.Definition);
            instance = pool.Get();
            ActiveCount++;
            instance.Initialize(request);
            return true;
        }

        internal void Release(SpellInstance2D instance)
        {
            if (instance == null || instance.Definition == null) return;
            SpellDefinition definition = instance.Definition;
            if (!pools.TryGetValue(definition, out ObjectPool<SpellInstance2D> pool))
            {
                Destroy(instance.gameObject);
                return;
            }
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
            pool.Release(instance);
        }

        private ObjectPool<SpellInstance2D> GetPool(SpellDefinition definition)
        {
            if (pools.TryGetValue(definition, out ObjectPool<SpellInstance2D> existing)) return existing;
            ObjectPool<SpellInstance2D> created = null;
            created = new ObjectPool<SpellInstance2D>(
                () =>
                {
                    SpellInstance2D value = Instantiate(definition.RuntimePrefab, transform);
                    value.SetWorld(this);
                    value.gameObject.SetActive(false);
                    return value;
                },
                value => value.gameObject.SetActive(true),
                value =>
                {
                    value.ResetForPool();
                    value.gameObject.SetActive(false);
                },
                value => Destroy(value.gameObject),
                true,
                Mathf.Max(1, defaultPoolCapacity),
                Mathf.Max(1, maximumPoolSize));
            pools.Add(definition, created);
            return created;
        }

        private void OnDestroy()
        {
            foreach (ObjectPool<SpellInstance2D> pool in pools.Values) pool.Dispose();
            pools.Clear();
            ActiveCount = 0;
        }
    }
}
