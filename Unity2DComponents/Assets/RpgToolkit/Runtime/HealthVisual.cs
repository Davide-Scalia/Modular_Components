using UnityEngine;
using UnityEngine.UI;

namespace RpgToolkit
{
    // Red shows current health; yellow retains recent damage briefly.
    public sealed class HealthVisual : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Image redFill;
        [SerializeField] private Image yellowFill;
        [SerializeField, Min(0)] private float delay = 0.3f;
        [SerializeField, Min(0.01f)] private float followSpeed = 4f;
        private float target;
        private float delayRemaining;

        public void Configure(Health source, Image red, Image yellow)
        {
            if (isActiveAndEnabled && health != null) health.Changed -= OnChanged;
            health = source;
            redFill = red;
            yellowFill = yellow;
            if (isActiveAndEnabled && health != null) health.Changed += OnChanged;
            if (health != null) SetBoth((float)health.Current / health.Maximum);
        }

        public void Bind(Health source)
        {
            if (isActiveAndEnabled && health != null) health.Changed -= OnChanged;
            health = source;
            if (isActiveAndEnabled && health != null) health.Changed += OnChanged;
            if (health != null) SetBoth((float)health.Current / health.Maximum);
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.Changed += OnChanged;
            SetBoth((float)health.Current / health.Maximum);
        }

        private void OnDisable()
        {
            if (health != null) health.Changed -= OnChanged;
        }

        private void SetBoth(float value)
        {
            target = Mathf.Clamp01(value);
            delayRemaining = 0;
            if (redFill != null) redFill.fillAmount = target;
            if (yellowFill != null) yellowFill.fillAmount = target;
        }

        private void OnChanged(int current, int maximum)
        {
            float next = Mathf.Clamp01((float)current / Mathf.Max(1, maximum));
            if (next >= target)
            {
                SetBoth(next);
                return;
            }
            target = next;
            delayRemaining = delay;
            if (redFill != null) redFill.fillAmount = target;
        }

        private void Update()
        {
            if (yellowFill == null || yellowFill.fillAmount <= target) return;
            if (delayRemaining > 0)
            {
                delayRemaining -= Time.deltaTime;
                return;
            }
            yellowFill.fillAmount = Mathf.Lerp(yellowFill.fillAmount, target,
                1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            if (yellowFill.fillAmount - target < 0.001f) yellowFill.fillAmount = target;
        }
    }
}
