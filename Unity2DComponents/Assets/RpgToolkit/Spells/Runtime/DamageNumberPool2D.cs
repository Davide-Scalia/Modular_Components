using UnityEngine;
using UnityEngine.Pool;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class DamageNumberPool2D : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private DamageNumberView2D prefab;
        [SerializeField, Min(0)] private int defaultCapacity = 16;
        [SerializeField, Min(1)] private int maximumSize = 64;

        [Header("Spawn")]
        [SerializeField] private Vector2 minimumSpawnOffset = new Vector2(-0.35f, 0.15f);
        [SerializeField] private Vector2 maximumSpawnOffset = new Vector2(0.35f, 0.65f);
        [SerializeField] private Vector2 minimumVelocity = new Vector2(-0.8f, 1.2f);
        [SerializeField] private Vector2 maximumVelocity = new Vector2(0.8f, 2.2f);
        [SerializeField] private float gravity = -2.5f;
        [SerializeField, Min(0.01f)] private float lifetime = 0.8f;

        [Header("Appearance")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = new Color(1f, 0.45f, 0.05f);
        [SerializeField, Min(0.01f)] private float normalFontSize = 3f;
        [SerializeField, Min(0.01f)] private float criticalFontSize = 4f;
        [SerializeField] private AnimationCurve alphaOverLifetime =
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private AnimationCurve scaleOverLifetime =
            AnimationCurve.EaseInOut(0f, 0.65f, 0.2f, 1f);

        private ObjectPool<DamageNumberView2D> pool;

        public int CountActive => pool?.CountActive ?? 0;

        private void Awake() => EnsurePool();

        public void Spawn(Vector2 anchor, int amount, bool critical)
        {
            if (amount <= 0 || prefab == null) return;
            EnsurePool();
            DamageNumberView2D view = pool.Get();
            Vector2 position = anchor + RandomBetween(minimumSpawnOffset, maximumSpawnOffset);
            Vector2 velocity = RandomBetween(minimumVelocity, maximumVelocity);
            view.Show(amount, critical, position, velocity, gravity, lifetime, normalColor, criticalColor,
                normalFontSize, criticalFontSize, alphaOverLifetime, scaleOverLifetime, Release);
        }

        private void EnsurePool()
        {
            if (pool != null || prefab == null) return;
            pool = new ObjectPool<DamageNumberView2D>(Create, OnGet, OnRelease, OnDestroyView,
                true, Mathf.Max(0, defaultCapacity), Mathf.Max(1, maximumSize));
        }

        private DamageNumberView2D Create()
        {
            DamageNumberView2D view = Instantiate(prefab, transform);
            view.gameObject.SetActive(false);
            return view;
        }

        private static void OnGet(DamageNumberView2D view) => view.gameObject.SetActive(true);

        private static void OnRelease(DamageNumberView2D view)
        {
            view.ResetView();
            view.gameObject.SetActive(false);
        }

        private static void OnDestroyView(DamageNumberView2D view)
        {
            if (view != null) Destroy(view.gameObject);
        }

        private void Release(DamageNumberView2D view)
        {
            if (pool != null && view != null) pool.Release(view);
        }

        private static Vector2 RandomBetween(Vector2 minimum, Vector2 maximum) => new Vector2(
            Random.Range(Mathf.Min(minimum.x, maximum.x), Mathf.Max(minimum.x, maximum.x)),
            Random.Range(Mathf.Min(minimum.y, maximum.y), Mathf.Max(minimum.y, maximum.y)));
    }
}
