using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    public static class SpellPreviewSimulator2D
    {
        public static void Fill(SpellDefinition definition, GameObject caster, Vector2 origin, Vector2 direction,
            List<Vector3> points, float step = 0.02f, float maximumTime = 5f, int maximumPoints = 256)
        {
            points.Clear();
            if (definition == null || direction.sqrMagnitude <= 0f || maximumPoints <= 0) return;
            EntityStats stats = caster != null ? caster.GetComponent<EntityStats>() : null;
            SpellCastBudget budget = new SpellCastBudget(1, 0);
            CasterStatSnapshot snapshot = new CasterStatSnapshot(stats);
            SpellSpawnRequest request = new SpellSpawnRequest(definition, caster, stats, snapshot, budget, 0,
                origin, direction);
            SpellRuntimeState state = new SpellRuntimeState();
            state.Initialize(request);
            SpellEvaluationContext context = new SpellEvaluationContext(state, null, true);
            SpellSimulation2D.Begin(context, request.Direction);
            points.Add(origin);
            if (definition.Form == SpellForm.Area) return;

            float elapsed = 0f;
            float simulationStep = Mathf.Max(0.001f, step);
            float limit = Mathf.Max(0f, maximumTime);
            int pointLimit = Mathf.Max(1, maximumPoints);
            while (elapsed < limit && points.Count < pointLimit)
            {
                float deltaTime = Mathf.Min(simulationStep, limit - elapsed);
                SpellStepResult result = SpellSimulation2D.Step(context, deltaTime);
                elapsed += deltaTime;
                points.Add(state.Position);
                if (result != SpellStepResult.Active) break;
            }
        }
    }
}
