using System.Collections.Generic;
using UnityEngine;

namespace RpgToolkit.Spells
{
    internal enum SpellStepResult { Active, Completed, Expired }

    internal static class SpellSimulation2D
    {
        public static void Begin(SpellEvaluationContext context, Vector2 direction)
        {
            if (context.Definition.Form == SpellForm.Projectile)
            {
                if (context.Definition.Motion != null) context.Definition.Motion.Initialize(context, direction);
                else context.Velocity = direction.normalized * context.GetSpellStat(SpellStat.Speed);
            }
            ExecuteRules(context, SpellTrigger.OnSpawn);
        }

        public static SpellStepResult Step(SpellEvaluationContext context, float deltaTime)
        {
            SpellRuntimeState state = GetState(context);
            if (state.Definition == null) return SpellStepResult.Completed;
            context.ClearCollision();
            ExecuteRules(context, SpellTrigger.OnTick);
            float lifetime = state.GetStat(SpellStat.Lifetime);
            if (state.Age >= lifetime)
            {
                ExecuteRules(context, SpellTrigger.OnExpire);
                return SpellStepResult.Expired;
            }

            float step = Mathf.Min(Mathf.Max(0f, deltaTime), lifetime - state.Age);
            SpellStepResult result = state.Definition.Form == SpellForm.Area
                ? StepArea(context, step)
                : StepProjectile(context, step);
            state.Age += step;
            if (result != SpellStepResult.Active) return result;
            if (state.Age + 0.00001f < state.GetStat(SpellStat.Lifetime)) return SpellStepResult.Active;
            ExecuteRules(context, SpellTrigger.OnExpire);
            return SpellStepResult.Expired;
        }

        private static SpellStepResult StepArea(SpellEvaluationContext context, float deltaTime)
        {
            SpellRuntimeState state = GetState(context);
            if (state.Age + deltaTime + 0.00001f < state.NextAreaPulse) return SpellStepResult.Active;
            QueryArea(context);
            ExecuteRules(context, SpellTrigger.OnAreaPulse);
            state.NextAreaPulse = state.Age + deltaTime + state.Definition.AreaPulseInterval;
            return SpellStepResult.Active;
        }

        private static SpellStepResult StepProjectile(SpellEvaluationContext context, float deltaTime)
        {
            SpellRuntimeState state = GetState(context);
            state.SyncSpeed();
            Vector2 origin = state.Position;
            if (state.Definition.Motion != null) state.Definition.Motion.Advance(context, deltaTime);
            else state.Position += state.Velocity * deltaTime;
            Vector2 destination = state.Position;
            state.Position = origin;
            return Sweep(context, destination);
        }

        private static SpellStepResult Sweep(SpellEvaluationContext context, Vector2 destination)
        {
            SpellRuntimeState state = GetState(context);
            Vector2 delta = destination - state.Position;
            float distance = delta.magnitude;
            if (distance <= 0.00001f)
            {
                state.Position = destination;
                return SpellStepResult.Active;
            }

            int mask = state.Definition.TargetLayers.value | state.Definition.ObstacleLayers.value;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = true;
            state.PhysicsHits.Clear();
            Physics2D.CircleCast(state.Position, state.GetStat(SpellStat.Radius), delta / distance,
                filter, state.PhysicsHits, distance);
            state.PhysicsHits.Sort((left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < state.PhysicsHits.Count; i++)
            {
                RaycastHit2D hit = state.PhysicsHits[i];
                Collider2D collider = hit.collider;
                if (collider == null || context.IsCaster(collider)) continue;
                bool target = Includes(state.Definition.TargetLayers, collider.gameObject.layer);
                bool obstacle = Includes(state.Definition.ObstacleLayers, collider.gameObject.layer);
                if (!target && !obstacle) continue;

                state.Position = hit.centroid;
                context.SetCollision(target ? collider : null, hit.point, hit.normal);
                bool newTarget = false;
                if (target)
                {
                    UnityEngine.Object key = context.HitKey(collider);
                    newTarget = key != null && state.HitTargets.Add(key);
                }
                if (obstacle || newTarget) ExecuteRules(context, SpellTrigger.OnHit);

                if (obstacle)
                {
                    if (state.BouncesRemaining <= 0) return SpellStepResult.Completed;
                    state.BouncesRemaining--;
                    state.Velocity = Vector2.Reflect(state.Velocity, hit.normal);
                    state.Position = hit.centroid + hit.normal * 0.001f;
                    ExecuteRules(context, SpellTrigger.OnBounce);
                    return SpellStepResult.Active;
                }
                if (!newTarget) continue;
                if (state.PiercesRemaining <= 0) return SpellStepResult.Completed;
                state.PiercesRemaining--;
            }

            state.Position = destination;
            return SpellStepResult.Active;
        }

        private static void QueryArea(SpellEvaluationContext context)
        {
            SpellRuntimeState state = GetState(context);
            state.AreaTargets.Clear();
#pragma warning disable CS0618
            int count = Physics2D.OverlapCircleNonAlloc(state.Position, state.GetStat(SpellStat.Radius),
                state.OverlapBuffer, state.Definition.TargetLayers);
#pragma warning restore CS0618
            for (int i = 0; i < count; i++)
            {
                Collider2D target = state.OverlapBuffer[i];
                if (target != null && !context.IsCaster(target)) state.AreaTargets.Add(target);
            }
            System.Array.Clear(state.OverlapBuffer, 0, count);
        }

        private static void ExecuteRules(SpellEvaluationContext context, SpellTrigger trigger)
        {
            SpellRuntimeState state = GetState(context);
            IReadOnlyList<SpellRule> rules = state.Definition.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                SpellRule rule = rules[i];
                if (rule == null || rule.Trigger != trigger) continue;
                SpellRuleState runtime = state.RuleStates[i];
                if (rule.Mode == SpellRuleMode.Once && runtime.Fired) continue;
                if (rule.Mode == SpellRuleMode.Repeat && state.Age + 0.00001f < runtime.NextAllowedTime) continue;
                //Debug.Log($"this is the age : {state.Age}");
                if (!ConditionsPass(rule, context)) continue;

                IReadOnlyList<SpellEffect> effects = rule.Effects;
                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                    if (effects[effectIndex] != null) effects[effectIndex].Apply(context);

                if (rule.Mode == SpellRuleMode.Once) runtime.Fired = true;
                else runtime.NextAllowedTime = state.Age + rule.RepeatInterval;
                //Debug.Log(runtime.NextAllowedTime);
                state.RuleStates[i] = runtime;
            }
        }

        private static bool ConditionsPass(SpellRule rule, SpellEvaluationContext context)
        {
            IReadOnlyList<SpellCondition> conditions = rule.Conditions;
            if (conditions.Count == 0) return true;
            if (rule.ConditionMode == ConditionMode.All)
            {
                for (int i = 0; i < conditions.Count; i++)
                    if (conditions[i] == null || !conditions[i].Evaluate(context)) return false;
                return true;
            }
            for (int i = 0; i < conditions.Count; i++)
                if (conditions[i] != null && conditions[i].Evaluate(context)) return true;
            return false;
        }

        private static bool Includes(LayerMask mask, int layer) => (mask.value & (1 << layer)) != 0;

        private static SpellRuntimeState GetState(SpellEvaluationContext context) => context.State;
    }
}
