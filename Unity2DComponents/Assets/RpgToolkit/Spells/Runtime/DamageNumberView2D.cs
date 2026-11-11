using System;
using TMPro;
using UnityEngine;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class DamageNumberView2D : MonoBehaviour
    {
        [SerializeField] private TextMeshPro label;
        private Action<DamageNumberView2D> release;
        private Vector2 velocity;
        private float gravity;
        private float lifetime;
        private float age;
        private float initialScale;
        private Color color;
        private AnimationCurve alphaCurve;
        private AnimationCurve scaleCurve;

        public TextMeshPro Label => label;

        private void Awake()
        {
            if (label == null) label = GetComponent<TextMeshPro>();
        }

        internal void Show(int amount, bool critical, Vector2 position, Vector2 initialVelocity,
            float gravityAcceleration, float duration, Color normalColor, Color criticalColor,
            float normalFontSize, float criticalFontSize, AnimationCurve alphaOverLifetime,
            AnimationCurve scaleOverLifetime, Action<DamageNumberView2D> returnToPool)
        {
            if (label == null) label = GetComponent<TextMeshPro>();
            transform.position = position;
            transform.localScale = Vector3.one;
            label.SetText("{0}", amount);
            label.enableAutoSizing = false;
            label.fontSize = critical ? criticalFontSize : normalFontSize;
            color = critical ? criticalColor : normalColor;
            label.color = color;
            velocity = initialVelocity;
            gravity = gravityAcceleration;
            lifetime = Mathf.Max(0.01f, duration);
            age = 0f;
            initialScale = 1f;
            alphaCurve = alphaOverLifetime;
            scaleCurve = scaleOverLifetime;
            release = returnToPool;
            gameObject.SetActive(true);
            ApplyCurves(0f);
        }

        internal void ResetView()
        {
            release = null;
            velocity = Vector2.zero;
            age = 0f;
            if (label != null)
            {
                label.text = string.Empty;
                label.color = Color.white;
            }
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            age += delta;
            velocity.y += gravity * delta;
            transform.position += (Vector3)(velocity * delta);
            float normalized = Mathf.Clamp01(age / lifetime);
            ApplyCurves(normalized);
            if (age < lifetime) return;
            Action<DamageNumberView2D> callback = release;
            release = null;
            callback?.Invoke(this);
        }

        private void ApplyCurves(float normalized)
        {
            float alpha = alphaCurve == null ? 1f - normalized : alphaCurve.Evaluate(normalized);
            float scale = scaleCurve == null ? 1f : scaleCurve.Evaluate(normalized);
            if (label != null)
            {
                Color next = color;
                next.a *= Mathf.Clamp01(alpha);
                label.color = next;
            }
            transform.localScale = Vector3.one * (initialScale * Mathf.Max(0f, scale));
        }
    }
}
