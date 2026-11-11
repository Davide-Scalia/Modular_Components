using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RpgToolkit.Spells.Editor
{
    public static class SpellSmokeChecks
    {
        [MenuItem("Window/RPG Toolkit/Spells/Run Smoke Checks")]
        public static void Run()
        {
            var objects = new List<UnityEngine.Object>();
            try
            {
                Check(Mathf.Approximately(SpellMath.Modified(10f, 5f, 0.2f), 18f), "modifier formula");
                SpellCastBudget budget = new SpellCastBudget(2, 1);
                Check(budget.TryConsume(0) && budget.TryConsume(1) && !budget.TryConsume(1), "spawn count limit");
                Check(!new SpellCastBudget(5, 1).TryConsume(2), "spawn depth limit");

                GameObject statObject = Track(objects, new GameObject("Spell Check Stats"));
                EntityStats stats = statObject.AddComponent<EntityStats>();
                DamageSpellEffect statSource = Track(objects, ScriptableObject.CreateInstance<DamageSpellEffect>());
                stats.ApplyModifier(statSource, ActorStat.Attack, 5f, 0.2f);
                Check(Mathf.Approximately(stats.GetStat(ActorStat.Attack), 18f), "actor modifier application");
                stats.RemoveModifiers(statSource);
                Check(Mathf.Approximately(stats.GetStat(ActorStat.Attack), 10f), "actor modifier removal");

                GameObject healthObject = Track(objects, new GameObject("Damage Result Check"));
                Health resultHealth = healthObject.AddComponent<Health>();
                DamageResult reported = default;
                resultHealth.Damaged += result => reported = result;
                resultHealth.ApplyDamage(150, true);
                Check(reported.Requested == 150 && reported.Applied == 100 && reported.IsCritical,
                    "damage event reports actual overkill damage and critical state");

                BallisticSpellMotion2D straight = Track(objects, ScriptableObject.CreateInstance<BallisticSpellMotion2D>());
                SpellDefinition previewSpell = Track(objects, ScriptableObject.CreateInstance<SpellDefinition>());
                ConfigureSpell(previewSpell, null, SpellForm.Projectile,
                    new SpellBaseStats { Damage = 10f, Speed = 10f, Lifetime = 1f, Radius = 0.1f },
                    straight, 0, 0, 0, 0);
                var points = new List<Vector3>();
                SpellPreviewSimulator2D.Fill(previewSpell, statObject, Vector2.zero, Vector2.right, points, 0.1f, 2f);
                Check(points.Count > 2 && Mathf.Abs(points[^1].x - 10f) < 0.05f, "straight preview distance");

                BallisticSpellMotion2D gravity = Track(objects, ScriptableObject.CreateInstance<BallisticSpellMotion2D>());
                Set(gravity, data => data.FindProperty("acceleration").vector2Value = new Vector2(0f, -10f));
                ConfigureSpell(previewSpell, null, SpellForm.Projectile,
                    new SpellBaseStats { Damage = 10f, Speed = 5f, Lifetime = 1f, Radius = 0.1f },
                    gravity, 0, 0, 0, 0);
                SpellPreviewSimulator2D.Fill(previewSpell, statObject, Vector2.zero, Vector2.right, points, 0.1f, 2f);
                Check(points[^1].y < -4f, "gravity preview");

                GameObject obstacle = Track(objects, new GameObject("Spell Check Obstacle"));
                obstacle.transform.position = new Vector2(2f, 0f);
                obstacle.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 2f);
                statObject.AddComponent<CircleCollider2D>().radius = 0.5f;
                ConfigureSpell(previewSpell, null, SpellForm.Projectile,
                    new SpellBaseStats { Damage = 10f, Speed = 10f, Lifetime = 1f, Radius = 0.1f },
                    straight, 0, 1, 0, 0);
                Physics2D.SyncTransforms();
                SpellPreviewSimulator2D.Fill(previewSpell, statObject, Vector2.zero, Vector2.right, points, 0.1f, 2f);
                Check(points[^1].x > 1f && points[^1].x < 2.1f, "swept collision and caster exclusion");

                LifetimeSpellCondition afterPointTwo = Track(objects, ScriptableObject.CreateInstance<LifetimeSpellCondition>());
                Set(afterPointTwo, data => data.FindProperty("seconds").floatValue = 0.2f);
                ModifySpellStatEffect speedBoost = Track(objects, ScriptableObject.CreateInstance<ModifySpellStatEffect>());
                Set(speedBoost, data =>
                {
                    data.FindProperty("stat").enumValueIndex = (int)SpellStat.Speed;
                    data.FindProperty("percent").floatValue = 1f;
                });
                ConfigureSpell(previewSpell, null, SpellForm.Projectile,
                    new SpellBaseStats { Damage = 10f, Speed = 2f, Lifetime = 1f, Radius = 0.1f },
                    straight, 0, 0, 0, 0,
                    new RuleSpec(SpellTrigger.OnTick, SpellRuleMode.Once,
                        new SpellCondition[] { afterPointTwo }, speedBoost));
                SpellPreviewSimulator2D.Fill(previewSpell, statObject, Vector2.zero, Vector2.right, points, 0.1f, 2f);
                Check(points[^1].x > 3.4f, "conditional preview stat change");

                RunRuntimeChecks(objects, straight);
                Debug.Log("Spell system smoke checks passed: stats, budgets, previews, conditions, collision, pooling, piercing, area pulses, critical and applied damage reporting, and death.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                for (int i = objects.Count - 1; i >= 0; i--)
                    if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
            }
        }

        public static void RunFromCommandLine() => Run();

        private static void RunRuntimeChecks(List<UnityEngine.Object> objects, BallisticSpellMotion2D straight)
        {
            GameObject worldObject = Track(objects, new GameObject("Spell Check World"));
            SpellWorld2D world = worldObject.AddComponent<SpellWorld2D>();
            GameObject templateObject = Track(objects, new GameObject("Spell Check Template"));
            SpellInstance2D template = templateObject.AddComponent<SpellInstance2D>();
            templateObject.SetActive(false);

            DamageSpellEffect damage = Track(objects, ScriptableObject.CreateInstance<DamageSpellEffect>());
            SpellDefinition projectile = Track(objects, ScriptableObject.CreateInstance<SpellDefinition>());
            ConfigureSpell(projectile, template, SpellForm.Projectile,
                new SpellBaseStats { Damage = 10f, Speed = 10f, Lifetime = 1f, Radius = 0.05f },
                straight, 1 << 2, 0, 1, 0,
                new RuleSpec(SpellTrigger.OnHit, SpellRuleMode.Repeat, null, damage));

            GameObject casterObject = Track(objects, new GameObject("Spell Check Caster"));
            EntityStats casterStats = casterObject.AddComponent<EntityStats>();
            Set(casterStats, data =>
            {
                data.FindProperty("critRate").intValue = 100;
                data.FindProperty("critDamage").intValue = 200;
            });
            SpellCaster2D caster = casterObject.AddComponent<SpellCaster2D>();
            Set(caster, data => data.FindProperty("world").objectReferenceValue = world);
            Health first = Target(objects, "Spell Check Target A", new Vector2(1f, 0f));
            Health second = Target(objects, "Spell Check Target B", new Vector2(2f, 0f));
            DamageResult spellDamage = default;
            first.Damaged += result => spellDamage = result;
            Physics2D.SyncTransforms();
            Check(caster.TryCast(projectile, Vector2.zero, Vector2.right), "runtime cast");
            SpellInstance2D instance = ActiveSpell(world);
            Check(instance != null && Mathf.Approximately(instance.Age, 0f), "pooled instance initialized");
            instance.Simulate(0.3f);
            Check(first.Current == 0 && second.Current == 0 && world.ActiveCount == 0,
                "piercing repeated hit damage and death");
            Check(spellDamage.IsCritical && spellDamage.Requested == 20 && spellDamage.Applied == 10,
                "spell critical uses caster stats and reports actual applied damage");

            first.GetComponent<Collider2D>().enabled = false;
            second.GetComponent<Collider2D>().enabled = false;
            Health third = Target(objects, "Spell Check Target C", new Vector2(1f, 0f));
            Physics2D.SyncTransforms();
            Check(caster.TryCast(projectile, Vector2.zero, Vector2.right), "second runtime cast");
            SpellInstance2D reused = ActiveSpell(world);
            Check(reused == instance && Mathf.Approximately(reused.Age, 0f), "pool reuse resets lifetime");
            reused.Simulate(0.2f);
            Check(third.Current == 0, "pool reuse resets hit history");
            reused.Simulate(1f);
            Check(world.ActiveCount == 0, "traveling projectile expires after piercing");
            third.GetComponent<Collider2D>().enabled = false;

            SpellDefinition area = Track(objects, ScriptableObject.CreateInstance<SpellDefinition>());
            ConfigureSpell(area, template, SpellForm.Area,
                new SpellBaseStats { Damage = 10f, Speed = 0f, Lifetime = 0.1f, Radius = 1f },
                null, 1 << 2, 0, 0, 0,
                new RuleSpec(SpellTrigger.OnAreaPulse, SpellRuleMode.Repeat, null, damage));
            Health areaTarget = Target(objects, "Spell Check Area Target", Vector2.zero);
            Physics2D.SyncTransforms();
            Check(Physics2D.OverlapCircleAll(Vector2.zero, 1f, 1 << 2).Length > 0,
                "area target physics query");
            Check(caster.TryCast(area, Vector2.zero, Vector2.right), "area cast");
            SpellInstance2D areaInstance = ActiveSpell(world);
            areaInstance.Simulate(0.02f);
            Check(areaTarget.Current == 0, "area pulse damage");
            areaInstance.Simulate(0.08f);
            Check(world.ActiveCount == 0, "exact lifetime expiry returns to pool");
        }

        private static Health Target(List<UnityEngine.Object> objects, string name, Vector2 position)
        {
            GameObject value = Track(objects, new GameObject(name));
            value.layer = 2;
            value.transform.position = position;
            value.AddComponent<BoxCollider2D>().size = Vector2.one * 0.2f;
            Health health = value.AddComponent<Health>();
            Set(health, data =>
            {
                data.FindProperty("maximum").intValue = 10;
                data.FindProperty("current").intValue = 10;
            });
            return health;
        }

        private static SpellInstance2D ActiveSpell(SpellWorld2D world)
        {
            foreach (SpellInstance2D instance in world.GetComponentsInChildren<SpellInstance2D>(true))
                if (instance.gameObject.activeSelf) return instance;
            return null;
        }

        private static void ConfigureSpell(SpellDefinition spell, SpellInstance2D prefab, SpellForm form,
            SpellBaseStats stats, SpellMotion2D motion, int targets, int obstacles, int pierces, int bounces,
            params RuleSpec[] ruleSpecs)
        {
            Set(spell, data =>
            {
                data.FindProperty("form").enumValueIndex = (int)form;
                data.FindProperty("runtimePrefab").objectReferenceValue = prefab;
                SerializedProperty baseStats = data.FindProperty("baseStats");
                baseStats.FindPropertyRelative("Damage").floatValue = stats.Damage;
                baseStats.FindPropertyRelative("Speed").floatValue = stats.Speed;
                baseStats.FindPropertyRelative("Lifetime").floatValue = stats.Lifetime;
                baseStats.FindPropertyRelative("Radius").floatValue = stats.Radius;
                data.FindProperty("motion").objectReferenceValue = motion;
                data.FindProperty("targetLayers").intValue = targets;
                data.FindProperty("obstacleLayers").intValue = obstacles;
                data.FindProperty("pierces").intValue = pierces;
                data.FindProperty("bounces").intValue = bounces;
                SerializedProperty rules = data.FindProperty("rules");
                rules.arraySize = ruleSpecs.Length;
                for (int i = 0; i < ruleSpecs.Length; i++)
                {
                    RuleSpec source = ruleSpecs[i];
                    SerializedProperty rule = rules.GetArrayElementAtIndex(i);
                    rule.FindPropertyRelative("trigger").enumValueIndex = (int)source.Trigger;
                    rule.FindPropertyRelative("conditionMode").enumValueIndex = (int)ConditionMode.All;
                    rule.FindPropertyRelative("mode").enumValueIndex = (int)source.Mode;
                    rule.FindPropertyRelative("repeatInterval").floatValue = 0f;
                    SerializedProperty conditions = rule.FindPropertyRelative("conditions");
                    conditions.arraySize = source.Conditions?.Length ?? 0;
                    for (int j = 0; j < conditions.arraySize; j++)
                        conditions.GetArrayElementAtIndex(j).objectReferenceValue = source.Conditions[j];
                    SerializedProperty effects = rule.FindPropertyRelative("effects");
                    effects.arraySize = source.Effects.Length;
                    for (int j = 0; j < effects.arraySize; j++)
                        effects.GetArrayElementAtIndex(j).objectReferenceValue = source.Effects[j];
                }
            });
        }

        private static void Set(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            SerializedObject data = new SerializedObject(target);
            data.Update();
            edit(data);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Track<T>(List<UnityEngine.Object> objects, T value) where T : UnityEngine.Object
        {
            objects.Add(value);
            return value;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Spell check failed: " + message);
        }

        private readonly struct RuleSpec
        {
            public SpellTrigger Trigger { get; }
            public SpellRuleMode Mode { get; }
            public SpellCondition[] Conditions { get; }
            public SpellEffect[] Effects { get; }

            public RuleSpec(SpellTrigger trigger, SpellRuleMode mode, SpellCondition[] conditions,
                params SpellEffect[] effects)
            {
                Trigger = trigger;
                Mode = mode;
                Conditions = conditions;
                Effects = effects ?? Array.Empty<SpellEffect>();
            }
        }
    }
}
