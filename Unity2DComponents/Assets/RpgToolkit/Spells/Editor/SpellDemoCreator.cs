using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace RpgToolkit.Spells.Editor
{
    public static class SpellDemoCreator
    {
        private const string Root = "Assets/RpgToolkit/Spells/Demo";
        private const int TargetLayer = 2;
        private static Sprite BuiltinSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("Window/RPG Toolkit/Spells/Create Demo Content")]
        public static void Create()
        {
            EnsureFolders();
            SpellInstance2D prefab = EnsureRuntimePrefab();
            DamageNumberView2D damageNumberPrefab = EnsureDamageNumberPrefab();
            Material lineMaterial = EnsureLineMaterial();

            BallisticSpellMotion2D straightMotion = Asset<BallisticSpellMotion2D>(Root + "/Straight Motion.asset");
            Set(straightMotion, data => data.FindProperty("acceleration").vector2Value = Vector2.zero);
            BallisticSpellMotion2D gravityMotion = Asset<BallisticSpellMotion2D>(Root + "/Gravity Motion.asset");
            Set(gravityMotion, data => data.FindProperty("acceleration").vector2Value = new Vector2(0f, -9.8f));
            HomingSpellMotion2D homingMotion = Asset<HomingSpellMotion2D>(Root + "/Homing Motion.asset");
            Set(homingMotion, data =>
            {
                data.FindProperty("acquisitionRange").floatValue = 10f;
                data.FindProperty("turnDegreesPerSecond").floatValue = 240f;
            });

            DamageSpellEffect damage = Asset<DamageSpellEffect>(Root + "/Damage Effect.asset");
            Set(damage, data => data.FindProperty("multiplier").floatValue = 1f);
            ModifySpellStatEffect boost = Asset<ModifySpellStatEffect>(Root + "/Speed Boost Effect.asset");
            Set(boost, data =>
            {
                data.FindProperty("stat").enumValueIndex = (int)SpellStat.Speed;
                data.FindProperty("flat").floatValue = 0f;
                data.FindProperty("percent").floatValue = 1f;
                data.FindProperty("duration").floatValue = 0f;
            });
            LifetimeSpellCondition halfSecond = Asset<LifetimeSpellCondition>(Root + "/After Half Second.asset");
            Set(halfSecond, data =>
            {
                data.FindProperty("comparison").enumValueIndex = (int)NumericComparison.GreaterOrEqual;
                data.FindProperty("seconds").floatValue = 0.5f;
            });

            SpellStatCondition halfway = Asset<SpellStatCondition>(Root + "/Half Lifetime.asset");
            Set(halfway, data =>
            {
                data.FindProperty("stat").enumValueIndex = (int)SpellStat.Lifetime;
                data.FindProperty("comparison").enumValueIndex = (int)NumericComparison.GreaterOrEqual;
                data.FindProperty("value").floatValue = 0.5f;
            });
            ModifySpellStatEffect growth = Asset<ModifySpellStatEffect>(Root + "/Repeat Radius Growth.asset");
            Set(growth, data =>
            {
                data.FindProperty("stat").enumValueIndex = (int)SpellStat.Radius;
                data.FindProperty("mode").enumValueIndex = (int)SpellModifierMode.Add;
                data.FindProperty("flat").floatValue = 0.1f;
            });
            DamageOverTimeSpellEffect burn = Asset<DamageOverTimeSpellEffect>(Root + "/Burn On Hit.asset");
            Set(burn, data =>
            {
                data.FindProperty("multiplier").floatValue = 0.1f;
                data.FindProperty("duration").floatValue = 3f;
                data.FindProperty("interval").floatValue = 0.25f;
            });

            SpellDefinition area = Asset<SpellDefinition>(Root + "/Burning Area.asset");
            ConfigureSpell(area, SpellForm.Area, prefab, new Color(1f, 0.25f, 0.05f, 0.65f),
                new SpellBaseStats { Damage = 4f, Speed = 0f, Lifetime = 2f, Radius = 1.5f }, null,
                1 << TargetLayer, 0, 0, 0, 0.25f,
                new RuleSpec(SpellTrigger.OnAreaPulse, SpellRuleMode.EveryEvent, 0f, null, damage));

            SpawnSpellEffect spawnArea = Asset<SpawnSpellEffect>(Root + "/Spawn Burning Area Effect.asset");
            Set(spawnArea, data =>
            {
                data.FindProperty("spell").objectReferenceValue = area;
                data.FindProperty("direction").enumValueIndex = (int)SpawnDirection.Forward;
                data.FindProperty("count").intValue = 1;
                data.FindProperty("arc").floatValue = 0f;
            });

            SpellDefinition piercing = Asset<SpellDefinition>(Root + "/Piercing Bolt.asset");
            ConfigureSpell(piercing, SpellForm.Projectile, prefab, new Color(0.1f, 0.85f, 1f),
                new SpellBaseStats { Damage = 10f, Speed = 10f, Lifetime = 2f, Radius = 0.12f }, straightMotion,
                1 << TargetLayer, 1, 2, 0, 0.25f,
                new RuleSpec(SpellTrigger.OnHit, SpellRuleMode.EveryEvent, 0f, null, damage));

            SpellDefinition gravity = Asset<SpellDefinition>(Root + "/Gravity Bounce.asset");
            ConfigureSpell(gravity, SpellForm.Projectile, prefab, new Color(1f, 0.75f, 0.1f),
                new SpellBaseStats { Damage = 15f, Speed = 9f, Lifetime = 4f, Radius = 0.18f }, gravityMotion,
                1 << TargetLayer, 1, 0, 1, 0.25f,
                new RuleSpec(SpellTrigger.OnHit, SpellRuleMode.EveryEvent, 0f, null, damage));

            SpellDefinition homing = Asset<SpellDefinition>(Root + "/Homing Orb.asset");
            ConfigureSpell(homing, SpellForm.Projectile, prefab, new Color(0.85f, 0.2f, 1f),
                new SpellBaseStats { Damage = 12f, Speed = 7f, Lifetime = 4f, Radius = 0.18f }, homingMotion,
                1 << TargetLayer, 1, 0, 0, 0.25f,
                new RuleSpec(SpellTrigger.OnHit, SpellRuleMode.EveryEvent, 0f, null, damage, burn));

            SpellDefinition timed = Asset<SpellDefinition>(Root + "/Timed Area Projectile.asset");
            ConfigureSpell(timed, SpellForm.Projectile, prefab, new Color(0.3f, 1f, 0.3f),
                new SpellBaseStats { Damage = 8f, Speed = 5f, Lifetime = 1.5f, Radius = 0.15f }, straightMotion,
                1 << TargetLayer, 1, 0, 0, 0.25f,
                new RuleSpec(SpellTrigger.OnTick, SpellRuleMode.Once, 0f, new SpellCondition[] { halfSecond }, boost),
                new RuleSpec(SpellTrigger.OnTick, SpellRuleMode.RepeatOnInterval, 0.25f, new SpellCondition[] { halfway }, growth),
                new RuleSpec(SpellTrigger.OnHit, SpellRuleMode.EveryEvent, 0f, null, damage),
                new RuleSpec(SpellTrigger.OnExpire, SpellRuleMode.Once, 0f, null, spawnArea));

            SpellDefinition[] spells = { piercing, gravity, homing, timed };
            CreateScene(Root + "/TopDownSpellDemo.unity", false, spells, lineMaterial, damageNumberPrefab);
            CreateScene(Root + "/PlatformerSpellDemo.unity", true, spells, lineMaterial, damageNumberPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Spell demos created. Open TopDownSpellDemo or PlatformerSpellDemo and press Play.");
        }

        public static void CreateFromCommandLine()
        {
            try { Create(); }
            catch (Exception exception) { Debug.LogException(exception); throw; }
        }

        private static void CreateScene(string path, bool platformer, SpellDefinition[] spells, Material lineMaterial,
            DamageNumberView2D damageNumberPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            SpellWorld2D world = new GameObject("Spell World").AddComponent<SpellWorld2D>();
            DamageNumberPool2D damageNumberPool = new GameObject("Damage Number Pool").AddComponent<DamageNumberPool2D>();
            Set(damageNumberPool, data =>
                data.FindProperty("prefab").objectReferenceValue = damageNumberPrefab);
            GameObject casterObject = Visual("Caster", new Vector2(-6f, platformer ? -2.1f : 0f),
                new Vector2(0.7f, 0.7f), new Color(0.2f, 0.85f, 0.35f));
            casterObject.AddComponent<CircleCollider2D>().radius = 0.5f;
            casterObject.AddComponent<EntityStats>();
            SpellCaster2D caster = casterObject.AddComponent<SpellCaster2D>();
            LineRenderer line = casterObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.06f;
            line.positionCount = 0;
            line.material = lineMaterial;
            line.startColor = line.endColor = new Color(0.2f, 0.9f, 1f, 0.8f);
            line.sortingOrder = 20;
            SpellTrajectoryPreview2D preview = casterObject.AddComponent<SpellTrajectoryPreview2D>();
            SpellDemoInput2D input = casterObject.AddComponent<SpellDemoInput2D>();
            Set(caster, data => data.FindProperty("world").objectReferenceValue = world);
            Set(preview, data => data.FindProperty("line").objectReferenceValue = line);
            Set(input, data =>
            {
                data.FindProperty("worldCamera").objectReferenceValue = camera;
                data.FindProperty("caster").objectReferenceValue = caster;
                data.FindProperty("preview").objectReferenceValue = preview;
                SerializedProperty list = data.FindProperty("spells");
                list.arraySize = spells.Length;
                for (int i = 0; i < spells.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = spells[i];
            });

            if (platformer)
            {
                Obstacle("Floor", new Vector2(0f, -3.1f), new Vector2(15f, 0.5f));
                Obstacle("Platform A", new Vector2(-1f, -0.7f), new Vector2(3f, 0.35f));
                Obstacle("Platform B", new Vector2(4f, 1.2f), new Vector2(3f, 0.35f));
                Target("Target Low", new Vector2(-0.5f, -2.35f), damageNumberPool);
                Target("Target Middle", new Vector2(2f, 0f), damageNumberPool);
                Target("Target High", new Vector2(5f, 1.9f), damageNumberPool);
            }
            else
            {
                Obstacle("Wall Upper", new Vector2(0f, 2.4f), new Vector2(0.5f, 3f));
                Obstacle("Wall Lower", new Vector2(0f, -2.4f), new Vector2(0.5f, 3f));
                Target("Target North", new Vector2(3f, 2.3f), damageNumberPool);
                Target("Target Center", new Vector2(4f, 0f), damageNumberPool);
                Target("Target South", new Vector2(3f, -2.3f), damageNumberPool);
            }

            EditorSceneManager.SaveScene(scene, path);
        }

        private static GameObject Visual(string name, Vector2 position, Vector2 scale, Color color)
        {
            GameObject value = new GameObject(name);
            value.transform.position = position;
            value.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = value.AddComponent<SpriteRenderer>();
            renderer.sprite = BuiltinSprite;
            renderer.color = color;
            return value;
        }

        private static void Obstacle(string name, Vector2 position, Vector2 scale)
        {
            GameObject value = Visual(name, position, scale, new Color(0.25f, 0.28f, 0.35f));
            value.AddComponent<BoxCollider2D>();
        }

        private static void Target(string name, Vector2 position, DamageNumberPool2D damageNumberPool)
        {
            GameObject value = Visual(name, position, new Vector2(0.8f, 0.8f), new Color(0.95f, 0.2f, 0.2f));
            value.layer = TargetLayer;
            BoxCollider2D body = value.AddComponent<BoxCollider2D>();
            Health health = value.AddComponent<Health>();
            Set(health, data =>
            {
                data.FindProperty("maximum").intValue = 20;
                data.FindProperty("current").intValue = 20;
            });
            SpellDemoTarget2D target = value.AddComponent<SpellDemoTarget2D>();
            EnemyHitFeedback2D feedback = value.AddComponent<EnemyHitFeedback2D>();
            Set(target, data =>
            {
                data.FindProperty("visual").objectReferenceValue = value.GetComponent<SpriteRenderer>();
                data.FindProperty("body").objectReferenceValue = body;
            });
            Set(feedback, data =>
            {
                data.FindProperty("health").objectReferenceValue = health;
                data.FindProperty("damageNumberPool").objectReferenceValue = damageNumberPool;
            });
            CreateHealthBar(value.transform, health);
        }

        private static void CreateHealthBar(Transform parent, Health health)
        {
            GameObject canvasObject = new GameObject("Health Bar", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.localPosition = new Vector3(0f, 0.8f, 0f);
            canvasRect.sizeDelta = new Vector2(100f, 12f);
            canvasRect.localScale = Vector3.one * 0.012f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 30;

            Image background = HealthBarImage("Background", canvasRect, new Color(0.08f, 0.08f, 0.1f, 0.9f));
            Image yellow = HealthBarImage("Delayed Health", canvasRect, new Color(1f, 0.75f, 0.05f));
            Image red = HealthBarImage("Current Health", canvasRect, new Color(0.9f, 0.08f, 0.08f));
            background.transform.SetAsFirstSibling();
            yellow.transform.SetSiblingIndex(1);
            red.transform.SetAsLastSibling();

            HealthVisual visual = canvasObject.AddComponent<HealthVisual>();
            visual.Configure(health, red, yellow);
        }

        private static Image HealthBarImage(string name, RectTransform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.sprite = BuiltinSprite;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            return image;
        }

        private static SpellInstance2D EnsureRuntimePrefab()
        {
            const string path = Root + "/Spell Instance.prefab";
            SpellInstance2D existing = AssetDatabase.LoadAssetAtPath<SpellInstance2D>(path);
            if (existing != null) return existing;
            GameObject value = Visual("Spell Instance", Vector2.zero, Vector2.one, Color.white);
            try
            {
                SpriteRenderer renderer = value.GetComponent<SpriteRenderer>();
                renderer.sortingOrder = 10;
                SpellInstance2D instance = value.AddComponent<SpellInstance2D>();
                Set(instance, data => data.FindProperty("visual").objectReferenceValue = renderer);
                return PrefabUtility.SaveAsPrefabAsset(value, path).GetComponent<SpellInstance2D>();
            }
            finally { UnityEngine.Object.DestroyImmediate(value); }
        }

        private static DamageNumberView2D EnsureDamageNumberPrefab()
        {
            const string path = Root + "/Damage Number.prefab";
            DamageNumberView2D existing = AssetDatabase.LoadAssetAtPath<DamageNumberView2D>(path);
            if (existing != null) return existing;
            GameObject value = new GameObject("Damage Number", typeof(RectTransform), typeof(TextMeshPro));
            try
            {
                TextMeshPro label = value.GetComponent<TextMeshPro>();
                label.text = string.Empty;
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.fontSize = 3f;
                label.sortingOrder = 40;
                RectTransform rect = value.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(4f, 1.5f);
                DamageNumberView2D view = value.AddComponent<DamageNumberView2D>();
                Set(view, data => data.FindProperty("label").objectReferenceValue = label);
                return PrefabUtility.SaveAsPrefabAsset(value, path).GetComponent<DamageNumberView2D>();
            }
            finally { UnityEngine.Object.DestroyImmediate(value); }
        }

        private static Material EnsureLineMaterial()
        {
            const string path = Root + "/Trajectory Material.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureSpell(SpellDefinition spell, SpellForm form, SpellInstance2D prefab,
            Color color, SpellBaseStats stats, SpellMotion2D motion, int targets, int obstacles, int pierces,
            int bounces, float pulseInterval, params RuleSpec[] ruleSpecs)
        {
            Set(spell, data =>
            {
                data.FindProperty("form").enumValueIndex = (int)form;
                data.FindProperty("runtimePrefab").objectReferenceValue = prefab;
                data.FindProperty("color").colorValue = color;
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
                data.FindProperty("areaPulseInterval").floatValue = pulseInterval;
                SerializedProperty rules = data.FindProperty("rules");
                rules.arraySize = ruleSpecs.Length;
                for (int i = 0; i < ruleSpecs.Length; i++) ConfigureRule(rules.GetArrayElementAtIndex(i), ruleSpecs[i]);
            });
        }

        private static void ConfigureRule(SerializedProperty property, RuleSpec rule)
        {
            property.FindPropertyRelative("trigger").enumValueIndex = (int)rule.Trigger;
            property.FindPropertyRelative("conditionMode").enumValueIndex = (int)ConditionMode.All;
            property.FindPropertyRelative("mode").enumValueIndex = (int)rule.Mode;
            property.FindPropertyRelative("repeatInterval").floatValue = rule.Interval;
            SerializedProperty conditions = property.FindPropertyRelative("conditions");
            conditions.arraySize = rule.Conditions?.Length ?? 0;
            for (int i = 0; i < conditions.arraySize; i++)
                conditions.GetArrayElementAtIndex(i).objectReferenceValue = rule.Conditions[i];
            SerializedProperty effects = property.FindPropertyRelative("effects");
            effects.arraySize = rule.Effects.Length;
            for (int i = 0; i < effects.arraySize; i++) effects.GetArrayElementAtIndex(i).objectReferenceValue = rule.Effects[i];
        }

        private static T Asset<T>(string path) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null) return value;
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        private static void Set(UnityEngine.Object target, Action<SerializedObject> edit)
        {
            SerializedObject data = new SerializedObject(target);
            data.Update();
            edit(data);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolders()
        {
            string[] parts = Root.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private readonly struct RuleSpec
        {
            public SpellTrigger Trigger { get; }
            public SpellRuleMode Mode { get; }
            public float Interval { get; }
            public SpellCondition[] Conditions { get; }
            public SpellEffect[] Effects { get; }

            public RuleSpec(SpellTrigger trigger, SpellRuleMode mode, float interval,
                SpellCondition[] conditions, params SpellEffect[] effects)
            {
                Trigger = trigger;
                Mode = mode;
                Interval = interval;
                Conditions = conditions;
                Effects = effects ?? Array.Empty<SpellEffect>();
            }
        }
    }
}
