using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class SpellWorld2D : MonoBehaviour
    {
        [SerializeField, Min(1)] private int defaultPoolCapacity = 16;
        [SerializeField, FormerlySerializedAs("maximumPoolSize"), Min(1),
         Tooltip("Maximum inactive instances retained per spell definition; not an active-spell limit.")]
        private int maximumInactivePerSpell = 128;
        private readonly Dictionary<SpellDefinition, ObjectPool<SpellInstance2D>> pools =
            new Dictionary<SpellDefinition, ObjectPool<SpellInstance2D>>();
        private readonly Dictionary<SpellInstance2D, SpellCastFamily> active =
            new Dictionary<SpellInstance2D, SpellCastFamily>();
        private bool disposing;

        public int ActiveCount => active.Count;

        public bool TrySpawn(in SpellSpawnRequest request, out SpellInstance2D instance) =>
            TrySpawn(request, out instance, out _);

        internal bool TrySpawn(in SpellSpawnRequest request, out SpellInstance2D instance, out string reason)
        {
            instance = null;
            reason = disposing ? "world is shutting down" :
                request.Definition == null || request.Definition.RuntimePrefab == null ? "missing spell or runtime prefab" :
                request.Family == null ? "missing cast family" : null;
            if (reason != null || !request.Family.TryAcquire(request.Depth, out reason)) return false;
            try
            {
                instance = GetPool(request.Definition).Get();
                active.Add(instance, request.Family);
                instance.Initialize(request);
                return true;
            }
            catch
            {
                if (instance != null && active.ContainsKey(instance)) Release(instance);
                else request.Family.Release();
                throw;
            }
        }

        internal void Release(SpellInstance2D instance)
        {
            if (instance == null || !Forget(instance)) return;
            SpellDefinition definition = instance.Definition;
            instance.ResetForPool();
            if (!disposing && definition != null && pools.TryGetValue(definition, out ObjectPool<SpellInstance2D> pool))
                pool.Release(instance);
            else DestroyInstance(instance);
        }

        internal bool Forget(SpellInstance2D instance)
        {
            if (!active.TryGetValue(instance, out SpellCastFamily family)) return false;
            active.Remove(instance);
            family.Release();
            return true;
        }

        private ObjectPool<SpellInstance2D> GetPool(SpellDefinition definition)
        {
            if (pools.TryGetValue(definition, out ObjectPool<SpellInstance2D> existing)) return existing;
            ObjectPool<SpellInstance2D> created = new ObjectPool<SpellInstance2D>(
                () =>
                {
                    SpellInstance2D value = Instantiate(definition.RuntimePrefab, transform);
                    value.SetWorld(this);
                    value.gameObject.SetActive(false);
                    return value;
                },
                null,
                value => value.gameObject.SetActive(false),
                DestroyInstance,
                true, Mathf.Max(1, defaultPoolCapacity), Mathf.Max(1, maximumInactivePerSpell));
            pools.Add(definition, created);
            return created;
        }

        private static void DestroyInstance(SpellInstance2D instance)
        {
            if (instance == null) return;
            if (Application.isPlaying) Destroy(instance.gameObject);
            else DestroyImmediate(instance.gameObject);
        }

        private void OnDestroy()
        {
            disposing = true;
            var instances = new List<SpellInstance2D>(active.Keys);
            foreach (SpellCastFamily family in active.Values) family.Release();
            active.Clear();
            foreach (SpellInstance2D instance in instances)
            {
                if (instance == null) continue;
                instance.ResetForPool();
                DestroyInstance(instance);
            }
            foreach (ObjectPool<SpellInstance2D> pool in pools.Values) pool.Dispose();
            pools.Clear();
        }
    }
}
