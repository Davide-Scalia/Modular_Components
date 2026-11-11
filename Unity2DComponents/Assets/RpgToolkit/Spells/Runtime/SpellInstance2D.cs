using UnityEngine;

namespace RpgToolkit.Spells
{
    [DisallowMultipleComponent]
    public sealed class SpellInstance2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        private readonly SpellRuntimeState state = new SpellRuntimeState();
        private SpellWorld2D world;
        private SpellEvaluationContext context;
        private bool running;

        public SpellDefinition Definition => state.Definition;
        public float Age => state.Age;
        public Vector2 Velocity => state.Velocity;

        internal void SetWorld(SpellWorld2D value) => world = value;

        internal void Initialize(in SpellSpawnRequest request)
        {
            state.Initialize(request);
            context = new SpellEvaluationContext(state, world, false);
            transform.position = state.Position;
            float diameter = Mathf.Max(0.01f, state.GetStat(SpellStat.Radius) * 2f);
            transform.localScale = new Vector3(diameter, diameter, 1f);
            if (visual != null) visual.color = request.Definition.Color;
            running = true;
            SpellSimulation2D.Begin(context, request.Direction);
        }

        public void Simulate(float deltaTime)
        {
            if (!running || context == null) return;
            SpellStepResult result = SpellSimulation2D.Step(context, deltaTime);
            transform.position = state.Position;
            float diameter = Mathf.Max(0.01f, state.GetStat(SpellStat.Radius) * 2f);
            transform.localScale = new Vector3(diameter, diameter, 1f);
            if (result == SpellStepResult.Active) return;
            running = false;
            world.Release(this);
        }

        internal void ResetForPool()
        {
            running = false;
            context = null;
            state.Reset();
            transform.localScale = Vector3.one;
        }

        private void FixedUpdate() => Simulate(Time.fixedDeltaTime);
    }
}
