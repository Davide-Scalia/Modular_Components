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
            if (!Application.isPlaying) throw new InvalidOperationException("Run spell smoke checks in Play mode to exercise Unity lifecycle callbacks.");
            var objects = new List<UnityEngine.Object>();
            try
            {
                Check(Mathf.Approximately(SpellMath.Modified(10f, 5f, 0.2f), 18f), "modifier formula");
                SpellCastFamily family = new SpellCastFamily(2, 1);
                Check(family.TryAcquire(0, out _) && family.TryAcquire(1, out _) && !family.TryAcquire(1, out _), "spawn count limit");
                Check(!new SpellCastFamily(5, 1).TryAcquire(2, out _), "spawn depth limit");

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
                RunBehaviorChecks(objects);
                Debug.Log("Spell system smoke checks passed: stats, concurrent families, previews, percentages, scheduled repeats, modifiers, frozen lifetime, target statuses, area occupancy, collision, pooling, critical damage, and death.");
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

        private const string BatchPendingKey = "RpgToolkit.Spells.SmokeChecks.Pending";

        [MenuItem("Window/RPG Toolkit/Spells/Run Smoke Checks", true)]
        private static bool CanRun() => Application.isPlaying;

        // Batch callers omit -quit: the runner exits explicitly after the Play-mode checks.
        public static void RunFromCommandLine()
        {
            SessionState.SetBool(BatchPendingKey, true);
            SubscribeBatchRunner();
            if (Application.isPlaying) FinishBatchRun(PlayModeStateChange.EnteredPlayMode);
            else EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void SubscribeBatchRunner()
        {
            if (!SessionState.GetBool(BatchPendingKey, false)) return;
            EditorApplication.playModeStateChanged -= FinishBatchRun;
            EditorApplication.playModeStateChanged += FinishBatchRun;
        }

        private static void FinishBatchRun(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(BatchPendingKey, false)) return;
            SessionState.SetBool(BatchPendingKey, false);
            EditorApplication.playModeStateChanged -= FinishBatchRun;
            int code = 0;
            try { Run(); }
            catch { code = 1; }
            EditorApplication.Exit(code);
        }

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
                new RuleSpec(SpellTrigger.OnHit, SpellRuleMode.EveryEvent, null, damage));

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
                new RuleSpec(SpellTrigger.OnAreaPulse, SpellRuleMode.EveryEvent, null, damage));
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

        private static void RunBehaviorChecks(List<UnityEngine.Object> objects)
        {
            ModifySpellStatEffect grow = Track(objects, ScriptableObject.CreateInstance<ModifySpellStatEffect>());
            Set(grow, data =>
            {
                data.FindProperty("stat").enumValueIndex = (int)SpellStat.Radius;
                data.FindProperty("flat").floatValue = 0.1f;
                data.FindProperty("mode").enumValueIndex = (int)SpellModifierMode.Add;
            });
            SpellStatCondition half = Track(objects, ScriptableObject.CreateInstance<SpellStatCondition>());
            Set(half, data =>
            {
                data.FindProperty("stat").enumValueIndex = (int)SpellStat.Lifetime;
                data.FindProperty("value").floatValue = 0.5f;
            });
            SpellDefinition spell = TestSpell(objects, null, SpellForm.Projectile, 5f,
                new RuleSpec(SpellTrigger.OnTick, SpellRuleMode.RepeatOnInterval, new SpellCondition[] { half }, grow));
            SpellEvaluationContext context = TestContext(spell);
            SpellSimulation2D.Begin(context, Vector2.right);
            for (int i = 0; i < 24; i++) SpellSimulation2D.Step(context, 0.1f);
            Check(Mathf.Approximately(context.GetSpellStat(SpellStat.Radius), 1f), "50% condition waits until halfway");
            SpellSimulation2D.Step(context, 0.1f);
            Check(Mathf.Approximately(context.GetSpellStat(SpellStat.Radius), 1.1f), "five-second spell triggers at 2.5 seconds");
            Set(half, data => data.FindProperty("value").floatValue = 10f);
            SpellSimulation2D.Step(context, 0.25f);
            Check(Mathf.Approximately(context.GetSpellStat(SpellStat.Radius), 1.2f), "started repeat ignores later false condition");
            SpellSimulation2D.Step(context, 1f);
            Check(Mathf.Approximately(context.GetSpellStat(SpellStat.Radius), 1.3f), "delayed step does not burst or duplicate timers");

            foreach (SpellRuleMode mode in new[] { SpellRuleMode.Once, SpellRuleMode.EveryEvent, SpellRuleMode.RepeatOnInterval })
            {
                SpellDefinition ruleSpell = TestSpell(objects, null, SpellForm.Projectile, 5f,
                    new RuleSpec(SpellTrigger.OnTick, mode, null, grow));
                SpellEvaluationContext ruleContext = TestContext(ruleSpell);
                SpellSimulation2D.Begin(ruleContext, Vector2.right);
                SpellSimulation2D.Step(ruleContext, 0.1f);
                float expected = mode == SpellRuleMode.EveryEvent ? 1.2f : 1.1f;
                Check(Mathf.Approximately(ruleContext.GetSpellStat(SpellStat.Radius), expected), mode + " execution semantics");
                SpellSimulation2D.Step(ruleContext, 0.15f);
                if (mode == SpellRuleMode.RepeatOnInterval)
                    Check(Mathf.Approximately(ruleContext.GetSpellStat(SpellStat.Radius), 1.2f), "repeat uses its own interval");
            }
            SpellDefinition spawnRepeat = TestSpell(objects, null, SpellForm.Projectile, 5f,
                new RuleSpec(SpellTrigger.OnSpawn, SpellRuleMode.RepeatOnInterval, null, grow));
            Set(spawnRepeat, data => data.FindProperty("rules").GetArrayElementAtIndex(0).FindPropertyRelative("repeatInterval").floatValue = 0f);
            SpellEvaluationContext spawned = TestContext(spawnRepeat);
            SpellSimulation2D.Begin(spawned, Vector2.right);
            SpellSimulation2D.Step(spawned, 0.05f);
            Check(Mathf.Approximately(spawned.GetSpellStat(SpellStat.Radius), 1.1f), "interval floor prevents per-frame repeats");
            SpellSimulation2D.Step(spawned, 0.05f);
            Check(Mathf.Approximately(spawned.GetSpellStat(SpellStat.Radius), 1.2f), "OnSpawn starts an independent timer");

            SpellDefinition plain = TestSpell(objects, null, SpellForm.Projectile, 5f);
            SpellEvaluationContext first = TestContext(plain);
            SpellEvaluationContext second = TestContext(plain);
            first.ApplySpellModifier(grow, SpellStat.Radius, 0.1f, 0f, 0.2f);
            first.ApplySpellModifier(grow, SpellStat.Radius, 0.1f, 0f, 0.3f);
            Check(Mathf.Approximately(first.GetSpellStat(SpellStat.Radius), 1.1f), "Refresh replaces one modifier");
            first.ApplySpellModifier(grow, SpellStat.Radius, 0.1f, 0f, 0.1f, SpellModifierMode.Add);
            first.Age = 0.15f;
            Check(Mathf.Approximately(first.GetSpellStat(SpellStat.Radius), 1.1f), "Add expires independently");
            Check(Mathf.Approximately(second.GetSpellStat(SpellStat.Radius), 1f), "casts do not share modifiers");
            first.Age = 0.31f;
            Check(Mathf.Approximately(first.GetSpellStat(SpellStat.Radius), 1f), "refreshed modifier expiry");
            bool lifetimeRejected = false;
            try { first.ApplySpellModifier(grow, SpellStat.Lifetime, 1f, 0f); }
            catch (InvalidOperationException) { lifetimeRejected = true; }
            Check(lifetimeRejected, "runtime lifetime changes are rejected");
            Set(plain, data => data.FindProperty("baseStats").FindPropertyRelative("Lifetime").floatValue = 10f);
            Check(Mathf.Approximately(first.Lifetime, 5f), "spawned lifetime is frozen even if definition changes");
            Set(half, data =>
            {
                data.FindProperty("stat").enumValueIndex = (int)SpellStat.Radius;
                data.FindProperty("value").floatValue = 1.5f;
            });
            Check(!half.Evaluate(second), "stat fraction uses original base");
            second.ApplySpellModifier(grow, SpellStat.Radius, 0f, 0.5f);
            Check(half.Evaluate(second), "150% stat comparison");
            Set(half, data => data.FindProperty("stat").enumValueIndex = (int)SpellStat.Speed);
            Check(half.Evaluate(second), "zero base stat compares without division");

            GameObject caster = Track(objects, new GameObject("Frozen Caster"));
            EntityStats stats = caster.AddComponent<EntityStats>();
            Set(stats, data => { data.FindProperty("critRate").intValue = 100; data.FindProperty("critDamage").intValue = 200; });
            CasterStatSnapshot snapshot = new CasterStatSnapshot(stats);
            SpellEvaluationContext frozen = new SpellEvaluationContext(new SpellSpawnRequest(plain, caster, snapshot,
                new SpellCastFamily(4, 8), 0, Vector2.zero, Vector2.right), null, false);
            Set(stats, data => data.FindProperty("critRate").intValue = 0);
            Check(frozen.TryGetCasterStat(ActorStat.CritRate, out float chance) && chance == 100f, "caster stats captured once");

            GameObject worldObject = Track(objects, new GameObject("Family Check World"));
            SpellWorld2D world = worldObject.AddComponent<SpellWorld2D>();
            GameObject prefabObject = Track(objects, new GameObject("Family Check Prefab"));
            SpellInstance2D prefab = prefabObject.AddComponent<SpellInstance2D>();
            prefabObject.SetActive(false);
            SpellDefinition pooled = TestSpell(objects, prefab, SpellForm.Projectile, 0.5f);
            SpellCastFamily family = new SpellCastFamily(2, 2);
            SpellSpawnRequest root = new SpellSpawnRequest(pooled, caster, snapshot, family, 0, Vector2.zero, Vector2.right);
            Check(world.TrySpawn(root, out SpellInstance2D parent), "root occupies family slot");
            parent.Context.SpawnChild(pooled, Vector2.right);
            Check(family.ActiveCount == 2 && world.ActiveCount == 2, "children share family");
            Check(!world.TrySpawn(root, out _, out string reason) && reason.Contains("concurrent"), "concurrent capacity rejection is explicit");
            SpellInstance2D child = null;
            foreach (SpellInstance2D value in world.GetComponentsInChildren<SpellInstance2D>()) if (value != parent) child = value;
            parent.Simulate(0.5f);
            Check(family.ActiveCount == 1 && child != null, "parent expiry releases capacity but preserves child family");
            child.Context.SpawnChild(pooled, Vector2.right);
            Check(family.ActiveCount == 2, "descendant can use recovered capacity");
            UnityEngine.Object.DestroyImmediate(child.gameObject);
            Check(family.ActiveCount == 1 && world.ActiveCount == 1, "external destruction releases exactly once");
            Check(!world.TrySpawn(new SpellSpawnRequest(pooled, caster, snapshot, family, 3, Vector2.zero, Vector2.right),
                out _, out reason) && reason.Contains("depth"), "child depth protected");
            foreach (SpellInstance2D value in world.GetComponentsInChildren<SpellInstance2D>()) value.Simulate(1f);
            Check(family.ActiveCount == 0 && world.ActiveCount == 0, "all family leases released");

            SpawnSpellEffect spawn = Track(objects, ScriptableObject.CreateInstance<SpawnSpellEffect>());
            Set(spawn, data => data.FindProperty("spell").objectReferenceValue = pooled);
            SpellDefinition repeatingParent = TestSpell(objects, prefab, SpellForm.Projectile, 2f,
                new RuleSpec(SpellTrigger.OnSpawn, SpellRuleMode.RepeatOnInterval, null, spawn));
            SpellCastFamily retryFamily = new SpellCastFamily(2, 8);
            Check(world.TrySpawn(new SpellSpawnRequest(repeatingParent, caster, snapshot, retryFamily, 0,
                Vector2.zero, Vector2.right), out SpellInstance2D retryParent), "spawn repeat starts");
            retryParent.Simulate(0.25f);
            Check(retryFamily.ActiveCount == 2, "failed scheduled spawn does not consume a slot");
            foreach (SpellInstance2D value in world.GetComponentsInChildren<SpellInstance2D>())
                if (value != retryParent) value.Simulate(0.5f);
            Check(retryFamily.ActiveCount == 1, "child completion frees capacity for retry");
            retryParent.Simulate(0.25f);
            Check(retryFamily.ActiveCount == 2, "scheduled spawning resumes after capacity recovers");
            foreach (SpellInstance2D value in world.GetComponentsInChildren<SpellInstance2D>())
                if (value != retryParent)
                    Check(value.Context.TryGetCasterStat(ActorStat.CritRate, out float childChance) && childChance == 100f,
                        "child inherits frozen caster snapshot");
            retryParent.Simulate(3f);
            foreach (SpellInstance2D value in world.GetComponentsInChildren<SpellInstance2D>()) value.Simulate(3f);
            Check(world.ActiveCount == 0 && retryFamily.ActiveCount == 0, "retry family fully released");

            // Actual impact applies a target-owned status and immediately finishes the projectile.
            DamageOverTimeSpellEffect burn = Track(objects, ScriptableObject.CreateInstance<DamageOverTimeSpellEffect>());
            Set(burn, data => { data.FindProperty("multiplier").floatValue = 0.1f; data.FindProperty("interval").floatValue = 0.25f; });
            Health victim = Target(objects, "Burn Target", new Vector2(1000f, 0f));
            SpellDefinition fire = TestSpell(objects, prefab, SpellForm.Projectile, 2f,
                new RuleSpec(SpellTrigger.OnHit, SpellRuleMode.EveryEvent, null, burn));
            Set(fire, data => { data.FindProperty("baseStats").FindPropertyRelative("Speed").floatValue = 10f; data.FindProperty("targetLayers").intValue = 1 << 2; });
            Physics2D.SyncTransforms();
            Check(world.TrySpawn(new SpellSpawnRequest(fire, caster, snapshot, new SpellCastFamily(2, 2), 0,
                new Vector2(998f, 0f), Vector2.right), out SpellInstance2D projectile), "status projectile cast");
            projectile.Simulate(0.3f);
            DamageOverTimeReceiver receiver = victim.GetComponent<DamageOverTimeReceiver>();
            Check(receiver != null && receiver.ActiveCount == 1 && victim.Current == 10 && world.ActiveCount == 0,
                "impact transfers status without immediate tick and releases projectile");
            UnityEngine.Object.DestroyImmediate(caster);
            receiver.Simulate(0.25f);
            Check(victim.Current == 8, "status survives projectile/caster loss and retains captured critical stats");
            receiver.Apply(burn, worldObject.GetEntityId(), victim, new CapturedSpellDamage(1f, 0f, 100f), 1f, 0.25f);
            receiver.Apply(burn, worldObject.GetEntityId(), victim, new CapturedSpellDamage(1f, 0f, 100f), 2f, 0.25f);
            Check(receiver.ActiveCount == 2, "same caster refreshes and different caster stacks independently");
            receiver.Simulate(0.01f);
            Check(victim.Current == 8, "status waits for interval");
            victim.ApplyDamage(100, false);
            Check(receiver.ActiveCount == 0, "death clears statuses immediately");

            Health expiringTarget = Target(objects, "Expiring Status Target", new Vector2(1005f, 0f));
            DamageOverTimeReceiver expiring = expiringTarget.gameObject.AddComponent<DamageOverTimeReceiver>();
            expiring.Apply(burn, default, expiringTarget, new CapturedSpellDamage(1f, 0f, 100f), 0.5f, 0.25f);
            expiring.Simulate(0.2f);
            expiring.Apply(burn, default, expiringTarget, new CapturedSpellDamage(1f, 0f, 100f), 0.5f, 0.25f);
            expiring.Simulate(0.05f);
            Check(expiringTarget.Current == 9, "status refresh does not postpone pending tick");
            expiring.Simulate(1f);
            Check(expiringTarget.Current == 8 && expiring.ActiveCount == 0, "status expires without catch-up burst");

            Check(world.TrySpawn(root, out _), "world shutdown has an active lease");
            UnityEngine.Object.DestroyImmediate(worldObject);
            Check(family.ActiveCount == 0, "world shutdown releases active leases");
            RunAreaAndPreviewChecks(objects, grow, burn);
        }

        private static void RunAreaAndPreviewChecks(List<UnityEngine.Object> objects, SpellEffect grow, DamageOverTimeSpellEffect burn)
        {
            Vector2 center = new Vector2(2000f, 2000f);
            SpellDefinition area = TestSpell(objects, null, SpellForm.Area, 5f);
            DamageSpellEffect damage = Track(objects, ScriptableObject.CreateInstance<DamageSpellEffect>());
            Set(damage, data => { data.FindProperty("multiplier").floatValue = 0.1f; data.FindProperty("canCritical").boolValue = false; });
            ConfigureSpell(area, null, SpellForm.Area, new SpellBaseStats { Damage = 10f, Lifetime = 5f, Radius = 1f },
                null, 1 << 2, 0, 0, 0, new RuleSpec(SpellTrigger.OnAreaPulse, SpellRuleMode.EveryEvent, null, damage));
            var targets = new List<Health>();
            for (int i = 0; i < 70; i++) targets.Add(Target(objects, "Crowded Area " + i, center));
            targets[0].gameObject.AddComponent<CircleCollider2D>().radius = 0.1f;
            Physics2D.SyncTransforms();
            SpellEvaluationContext context = TestContext(area, center);
            SpellSimulation2D.Begin(context, Vector2.right);
            SpellSimulation2D.Step(context, 0.02f);
            foreach (Health target in targets) Check(target.Current == 9, "area includes >64 colliders and deduplicates enemies");
            targets[0].transform.position = center + Vector2.right * 5f;
            Health newcomer = Target(objects, "Area Entrant", center);
            Physics2D.SyncTransforms();
            SpellSimulation2D.Step(context, 0.1f);
            Check(newcomer.Current == 10, "area does not query/damage every frame");
            SpellSimulation2D.Step(context, 0.15f);
            Check(targets[0].Current == 9 && newcomer.Current == 9, "area refreshes occupancy each pulse");
            context.QueryArea();
            burn.Apply(context);
            Check(newcomer.GetComponent<DamageOverTimeReceiver>() != null && targets[0].GetComponent<DamageOverTimeReceiver>() == null,
                "area statuses attach only to current occupants");
            DamageOverTimeReceiver receiver = newcomer.GetComponent<DamageOverTimeReceiver>();
            newcomer.gameObject.SetActive(false);
            Check(receiver.ActiveCount == 0, "disabling target clears statuses");
            foreach (Health target in targets) target.gameObject.SetActive(false);

            Health previewTarget = Target(objects, "Preview Target", center);
            SpellDefinition previewArea = TestSpell(objects, null, SpellForm.Area, 1f,
                new RuleSpec(SpellTrigger.OnAreaPulse, SpellRuleMode.EveryEvent, null, damage, burn));
            Set(previewArea, data => data.FindProperty("targetLayers").intValue = 1 << 2);
            Physics2D.SyncTransforms();
            SpellEvaluationContext preview = TestContext(previewArea, center, true);
            SpellSimulation2D.Begin(preview, Vector2.right);
            SpellSimulation2D.Step(preview, 0.1f);
            Check(previewTarget.Current == 10 && previewTarget.GetComponent<DamageOverTimeReceiver>() == null,
                "preview never damages or adds statuses");
            preview.SpawnChild(previewArea, Vector2.right);
            Check(preview.Family.ActiveCount == 0, "preview does not spawn or acquire capacity");

            SpellDefinition moving = TestSpell(objects, null, SpellForm.Projectile, 1f,
                new RuleSpec(SpellTrigger.OnSpawn, SpellRuleMode.RepeatOnInterval, null, grow));
            Set(moving, data => data.FindProperty("baseStats").FindPropertyRelative("Speed").floatValue = 3f);
            SpellEvaluationContext live = TestContext(moving);
            preview = TestContext(moving, Vector2.zero, true);
            SpellSimulation2D.Begin(live, Vector2.right);
            SpellSimulation2D.Begin(preview, Vector2.right);
            for (int i = 0; i < 10; i++)
            {
                SpellSimulation2D.Step(live, 0.1f);
                SpellSimulation2D.Step(preview, 0.1f);
                Check(Vector2.Distance(live.Position, preview.Position) < 0.0001f &&
                    Mathf.Approximately(live.GetSpellStat(SpellStat.Radius), preview.GetSpellStat(SpellStat.Radius)),
                    "runtime and preview share movement and scheduled modifiers");
            }
        }

        private static SpellDefinition TestSpell(List<UnityEngine.Object> objects, SpellInstance2D prefab,
            SpellForm form, float lifetime, params RuleSpec[] rules)
        {
            SpellDefinition spell = Track(objects, ScriptableObject.CreateInstance<SpellDefinition>());
            ConfigureSpell(spell, prefab, form, new SpellBaseStats { Damage = 10f, Lifetime = lifetime, Radius = 1f },
                null, 0, 0, 0, 0, rules);
            return spell;
        }

        private static SpellEvaluationContext TestContext(SpellDefinition spell, Vector2 position = default, bool preview = false) =>
            new SpellEvaluationContext(new SpellSpawnRequest(spell, null, new CasterStatSnapshot(null),
                new SpellCastFamily(64, 8), 0, position, Vector2.right), null, preview);

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
                    rule.FindPropertyRelative("repeatInterval").floatValue = source.Interval;
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
            public float Interval { get; }

            public RuleSpec(SpellTrigger trigger, SpellRuleMode mode, SpellCondition[] conditions,
                params SpellEffect[] effects)
            {
                Interval = 0.25f;
                Trigger = trigger;
                Mode = mode;
                Conditions = conditions;
                Effects = effects ?? Array.Empty<SpellEffect>();
            }
        }
    }
}
