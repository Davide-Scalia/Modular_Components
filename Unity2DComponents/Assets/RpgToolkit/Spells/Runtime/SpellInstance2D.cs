using UnityEngine;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class SpellInstance2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        private SpellWorld2D world;
        private SpellEvaluationContext context;
        private bool running;

        public SpellDefinition Definition => context?.Definition;
        public float Age => context?.Age ?? 0f;
        public Vector2 Velocity => context?.Velocity ?? Vector2.zero;
        internal SpellEvaluationContext Context => context;
        internal void SetWorld(SpellWorld2D value) => world = value;

        internal void Initialize(in SpellSpawnRequest request)
        {
            context = new SpellEvaluationContext(request, world, false);
            UpdateVisual();
            if (visual != null) visual.color = request.Definition.Color;
            running = true;
            gameObject.SetActive(true);
            SpellSimulation2D.Begin(context, request.Direction);
        }

        public void Simulate(float deltaTime)
        {
            if (!running || context == null) return;
            SpellStepResult result;
            try { result = SpellSimulation2D.Step(context, deltaTime); }
            catch { world.Release(this); throw; }
            UpdateVisual();
            if (result != SpellStepResult.Active) world.Release(this);
        }

        private void UpdateVisual()
        {
            transform.position = context.Position;
            float diameter = Mathf.Max(0.01f, context.GetSpellStat(SpellStat.Radius) * 2f);
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        internal void ResetForPool()
        {
            running = false;
            context = null;
            transform.localScale = Vector3.one;
        }

        private void FixedUpdate() => Simulate(Time.fixedDeltaTime);
        private void OnDisable()
        {
            // A disabled object may be in Destroy: never return it to the pool here.
            if (!running) return;
            if (world != null) world.Forget(this);
            ResetForPool();
        }
        private void OnDestroy()
        {
            if (world != null) world.Forget(this);
        }
    }
}
