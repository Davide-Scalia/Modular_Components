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
            ExecuteRules(context, SpellTrigger.OnTick);
            context.NextConditionPoll = SpellMath.MinimumInterval;
        }

        public static SpellStepResult Step(SpellEvaluationContext context, float deltaTime)
        {
            if (context.Definition == null) return SpellStepResult.Completed;
            context.StepIndex++;
            context.ClearCollision();
            float step = Mathf.Min(Mathf.Max(0f, deltaTime), Mathf.Max(0f, context.Lifetime - context.Age));
            SpellStepResult result = context.Definition.Form == SpellForm.Area
                ? SpellStepResult.Active : StepProjectile(context, step);
            context.Age += step;
            if (result != SpellStepResult.Active) return result;

            context.ClearCollision();
            RunScheduledRules(context);
            if (context.Age + 0.00001f >= context.NextConditionPoll)
            {
                ExecuteRules(context, SpellTrigger.OnTick);
                context.NextConditionPoll = context.Age + SpellMath.MinimumInterval;
            }
            if (context.Definition.Form == SpellForm.Area && context.Age + 0.00001f >= context.NextAreaPulse)
            {
                ExecuteRules(context, SpellTrigger.OnAreaPulse);
                context.NextAreaPulse = context.Age + context.Definition.AreaPulseInterval;
            }
            if (context.Age + 0.00001f < context.Lifetime) return SpellStepResult.Active;
            ExecuteRules(context, SpellTrigger.OnExpire);
            return SpellStepResult.Expired;
        }

        private static SpellStepResult StepProjectile(SpellEvaluationContext context, float deltaTime)
        {
            context.SyncSpeed();
            Vector2 origin = context.Position;
            if (context.Definition.Motion != null) context.Definition.Motion.Advance(context, deltaTime);
            else context.Position += context.Velocity * deltaTime;
            Vector2 destination = context.Position;
            context.Position = origin;
            return Sweep(context, destination);
        }

        private static SpellStepResult Sweep(SpellEvaluationContext context, Vector2 destination)
        {
            Vector2 delta = destination - context.Position;
            float distance = delta.magnitude;
            if (distance <= 0.00001f)
            {
                context.Position = destination;
                return SpellStepResult.Active;
            }

            int mask = context.Definition.TargetLayers.value | context.Definition.ObstacleLayers.value;
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = true;
            context.PhysicsHits.Clear();
            Physics2D.CircleCast(context.Position, context.GetSpellStat(SpellStat.Radius), delta / distance,
                filter, context.PhysicsHits, distance);
            context.PhysicsHits.Sort((left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < context.PhysicsHits.Count; i++)
            {
                RaycastHit2D hit = context.PhysicsHits[i];
                Collider2D collider = hit.collider;
                if (collider == null || context.IsCaster(collider)) continue;
                bool target = Includes(context.Definition.TargetLayers, collider.gameObject.layer);
                bool obstacle = Includes(context.Definition.ObstacleLayers, collider.gameObject.layer);
                if (!target && !obstacle) continue;

                context.Position = hit.centroid;
                context.SetCollision(target ? collider : null, hit.point, hit.normal);
                bool newTarget = false;
                if (target)
                {
                    UnityEngine.Object key = context.HitKey(collider);
                    newTarget = key != null && context.HitTargets.Add(key);
                }
                if (obstacle || newTarget) ExecuteRules(context, SpellTrigger.OnHit);

                if (obstacle)
                {
                    if (context.BouncesRemaining <= 0) return SpellStepResult.Completed;
                    context.BouncesRemaining--;
                    context.Velocity = Vector2.Reflect(context.Velocity, hit.normal);
                    context.Position = hit.centroid + hit.normal * 0.001f;
                    ExecuteRules(context, SpellTrigger.OnBounce);
                    return SpellStepResult.Active;
                }
                if (!newTarget) continue;
                if (context.PiercesRemaining <= 0) return SpellStepResult.Completed;
                context.PiercesRemaining--;
            }

            context.Position = destination;
            return SpellStepResult.Active;
        }

        private static void ExecuteRules(SpellEvaluationContext context, SpellTrigger trigger)
        {
            IReadOnlyList<SpellRule> rules = context.Definition.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                SpellRule rule = rules[i];
                if (rule == null || rule.Trigger != trigger) continue;
                SpellRuleState runtime = context.RuleStates[i];
                if (rule.Mode != SpellRuleMode.EveryEvent && runtime.Fired) continue;
                if (!ConditionsPass(rule, context)) continue;
                runtime.Fired = true;
                runtime.Target = context.TargetCollider;
                runtime.CollisionPoint = context.CollisionPoint;
                runtime.CollisionNormal = context.CollisionNormal;
                runtime.NextAllowedTime = context.Age + rule.RepeatInterval;
                runtime.LastExecutionStep = context.StepIndex;
                context.RuleStates[i] = runtime;
                ApplyEffects(rule, context);
            }
        }

        private static void RunScheduledRules(SpellEvaluationContext context)
        {
            IReadOnlyList<SpellRule> rules = context.Definition.Rules;
            Collider2D target = context.TargetCollider;
            Vector2 point = context.CollisionPoint;
            Vector2 normal = context.CollisionNormal;
            for (int i = 0; i < rules.Count; i++)
            {
                SpellRule rule = rules[i];
                SpellRuleState runtime = context.RuleStates[i];
                if (rule == null || rule.Mode != SpellRuleMode.RepeatOnInterval || !runtime.Fired ||
                    runtime.LastExecutionStep == context.StepIndex ||
                    context.Age + 0.00001f < runtime.NextAllowedTime) continue;
                context.SetCollision(runtime.Target, runtime.CollisionPoint, runtime.CollisionNormal);
                runtime.NextAllowedTime = context.Age + rule.RepeatInterval;
                runtime.LastExecutionStep = context.StepIndex;
                context.RuleStates[i] = runtime;
                ApplyEffects(rule, context);
            }
            context.SetCollision(target, point, normal);
        }

        private static void ApplyEffects(SpellRule rule, SpellEvaluationContext context)
        {
            if (context.Definition.Form == SpellForm.Area) context.QueryArea();
            IReadOnlyList<SpellEffect> effects = rule.Effects;
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null) effects[i].Apply(context);
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

    }
}
