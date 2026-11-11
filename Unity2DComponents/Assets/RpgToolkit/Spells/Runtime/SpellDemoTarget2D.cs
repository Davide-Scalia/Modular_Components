using UnityEngine;

namespace RpgToolkit.Spells
{
    [RequireComponent(typeof(Health))]
    public sealed class SpellDemoTarget2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Collider2D body;
        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        private void OnDied()
        {
            if (body != null) body.enabled = false;
            if (visual != null) visual.color = Color.black;
            transform.localScale *= 0.7f;
        }
    }
}
