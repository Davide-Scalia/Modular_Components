using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RpgToolkit.Editor
{
    // Creates isolated demo content; never edits the user's open scene.
    internal static class RpgDemoCreator
    {
        private const string Folder = "Assets/RpgToolkit/Demo";

        [MenuItem("Window/RPG Toolkit/Create Demo Content")]
        private static void Create()
        {
            string arenaPath = Folder + "/RpgArenaDemo.unity";
            string freePath = Folder + "/RpgFreeDemo.unity";
            string mapPath = Folder + "/RpgMapDemo.unity";
            var openPaths = new System.Collections.Generic.HashSet<string>();
            foreach (string path in new[] { arenaPath })
            {
                Scene open = SceneManager.GetSceneByPath(path);
                if (open.IsValid() && open.isLoaded && open.isDirty)
                    throw new System.InvalidOperationException("Save or discard changes to the open " + open.name + " scene before regenerating it.");
                if (open.IsValid() && open.isLoaded) openPaths.Add(path);
            }
            Scene original = SceneManager.GetActiveScene();
            try
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/RpgToolkit", "Demo");
                Sprite sprite = EnsureSprite();
                Tile floor = EnsureTile("Floor", sprite, new Color(0.45f, 0.65f, 0.55f));
                Tile mud = EnsureTile("Mud", sprite, new Color(0.6f, 0.4f, 0.2f));
                Tile wall = EnsureTile("Wall", sprite, new Color(0.16f, 0.18f, 0.23f));
                TopDownTileRules rules = EnsureRules(floor, mud, wall);
                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/Settings/InputSystem_Actions.inputactions");
                RpgPreset preset = EnsurePreset(actions, sprite);
                GameObject prefab = RpgPrefabFactory.Create(preset, Folder, "DemoCharacter", true);
                AttackDefinition demoAttack = EnsureAttack();
                AssignDemoAttacks(prefab, sprite, new[]
                {
                    demoAttack,
                    EnsureVariant("DemoSpell", DamageKind.Magical, 150, 0, 1, 15, 25, demoAttack.CompletionPattern),
                    EnsureVariant("DemoQuickStrike", DamageKind.Physical, 60, 0, 2, 5, 10, demoAttack.CompletionPattern),
                    EnsureVariant("DemoHeavyStrike", DamageKind.Physical, 220, 0, 1, 25, 40, demoAttack.CompletionPattern)
                });
                RpgPreset freePreset = Object.Instantiate(preset);
                SerializedObject freeData = new SerializedObject(freePreset);
                freeData.FindProperty("explorationMode").enumValueIndex = (int)ExplorationMode.PointAndClick;
                freeData.ApplyModifiedPropertiesWithoutUndo();
                GameObject freePrefab = RpgPrefabFactory.Create(freePreset, Folder, "DemoClickCharacter", true);
                Object.DestroyImmediate(freePreset);

                CreateArena(floor, mud, wall, rules, prefab, sprite, openPaths.Contains(arenaPath));
                CreateFreeScene(sprite, freePrefab);

                var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                if (!scenes.Exists(s => s.path == arenaPath)) scenes.Add(new EditorBuildSettingsScene(arenaPath, true));
                if (!scenes.Exists(s => s.path == freePath)) scenes.Add(new EditorBuildSettingsScene(freePath, true));
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mapPath) != null && !scenes.Exists(s => s.path == mapPath))
                    scenes.Add(new EditorBuildSettingsScene(mapPath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
                Debug.Log("RPG Toolkit demo content created. Open RpgArenaDemo or RpgFreeDemo and enter Play Mode.");
            }
            finally
            {
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        private static Sprite EnsureSprite()
        {
            string path = Folder + "/DemoPixel.png";
            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[256];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Tile EnsureTile(string name, Sprite sprite, Color color)
        {
            string path = Folder + "/" + name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static TopDownTileRules EnsureRules(Tile floor, Tile mud, Tile wall)
        {
            string path = Folder + "/TopDownRules.asset";
            TopDownTileRules rules = AssetDatabase.LoadAssetAtPath<TopDownTileRules>(path);
            if (rules != null) return rules;
            rules = ScriptableObject.CreateInstance<TopDownTileRules>();
            AssetDatabase.CreateAsset(rules, path);
            SerializedObject data = new SerializedObject(rules);
            SerializedProperty entries = data.FindProperty("entries");
            entries.arraySize = 3;
            SetRule(entries.GetArrayElementAtIndex(0), floor, true, 10, false);
            SetRule(entries.GetArrayElementAtIndex(1), mud, true, 20, false);
            SetRule(entries.GetArrayElementAtIndex(2), wall, false, 1, true);
            data.ApplyModifiedPropertiesWithoutUndo();
            return rules;
        }

        private static void SetRule(SerializedProperty element, Tile tile, bool walkable, int cost, bool blocksSight)
        {
            element.FindPropertyRelative("Tile").objectReferenceValue = tile;
            element.FindPropertyRelative("Walkable").boolValue = walkable;
            element.FindPropertyRelative("EntryCost").intValue = cost;
            element.FindPropertyRelative("OpenEdges").intValue = (int)CardinalEdge.All;
            element.FindPropertyRelative("BlocksSight").boolValue = blocksSight;
        }

        private static RpgPreset EnsurePreset(InputActionAsset actions, Sprite sprite)
        {
            string path = Folder + "/TopDownPreset.asset";
            RpgPreset preset = AssetDatabase.LoadAssetAtPath<RpgPreset>(path);
            if (preset != null) return preset;
            preset = ScriptableObject.CreateInstance<RpgPreset>();
            AssetDatabase.CreateAsset(preset, path);
            SerializedObject data = new SerializedObject(preset);
            data.FindProperty("description").stringValue = "2D top-down tactical character";
            data.FindProperty("preview").objectReferenceValue = sprite;
            data.FindProperty("characterSprite").objectReferenceValue = sprite;
            data.FindProperty("explorationMode").enumValueIndex = (int)ExplorationMode.Grid;
            data.FindProperty("tacticalCombat").boolValue = true;
            data.FindProperty("inputActions").objectReferenceValue = actions;
            data.ApplyModifiedPropertiesWithoutUndo();
            return preset;
        }

        private static AttackDefinition EnsureAttack()
        {
            string patternPath = Folder + "/DemoAttackPattern.asset";
            AttackPattern pattern = AssetDatabase.LoadAssetAtPath<AttackPattern>(patternPath);
            if (pattern == null)
            {
                pattern = ScriptableObject.CreateInstance<AttackPattern>();
                AssetDatabase.CreateAsset(pattern, patternPath);
                SerializedObject patternData = new SerializedObject(pattern);
                SerializedProperty cells = patternData.FindProperty("cells");
                cells.GetArrayElementAtIndex(4).boolValue = true;
                cells.GetArrayElementAtIndex(7).boolValue = true;
                patternData.ApplyModifiedPropertiesWithoutUndo();
            }
            string attackPath = Folder + "/DemoAttack.asset";
            AttackDefinition attack = AssetDatabase.LoadAssetAtPath<AttackDefinition>(attackPath);
            if (attack != null) return attack;
            attack = ScriptableObject.CreateInstance<AttackDefinition>();
            AssetDatabase.CreateAsset(attack, attackPath);
            SerializedObject data = new SerializedObject(attack);
            data.FindProperty("preparationPattern").objectReferenceValue = pattern;
            data.FindProperty("preparationHits").intValue = 1;
            data.FindProperty("completionPattern").objectReferenceValue = pattern;
            data.FindProperty("completionHits").intValue = 2;
            data.ApplyModifiedPropertiesWithoutUndo();
            return attack;
        }

        private static AttackDefinition EnsureVariant(string name, DamageKind kind, int scaling,
            int preparationHits, int completionHits, int preparationCost, int completionCost, AttackPattern pattern)
        {
            string path = Folder + "/" + name + ".asset";
            AttackDefinition attack = AssetDatabase.LoadAssetAtPath<AttackDefinition>(path);
            if (attack != null) return attack;
            attack = ScriptableObject.CreateInstance<AttackDefinition>();
            AssetDatabase.CreateAsset(attack, path);
            SerializedObject data = new SerializedObject(attack);
            data.FindProperty("damageKind").enumValueIndex = (int)kind;
            data.FindProperty("scalingPercent").intValue = scaling;
            data.FindProperty("preparationCost").intValue = preparationCost;
            data.FindProperty("completionCost").intValue = completionCost;
            data.FindProperty("preparationPattern").objectReferenceValue = pattern;
            data.FindProperty("preparationHits").intValue = preparationHits;
            data.FindProperty("completionPattern").objectReferenceValue = pattern;
            data.FindProperty("completionHits").intValue = completionHits;
            data.ApplyModifiedPropertiesWithoutUndo();
            return attack;
        }

        private static void AssignDemoAttacks(GameObject prefab, Sprite icon, AttackDefinition[] definitions)
        {
            foreach (AttackDefinition definition in definitions)
                if (definition != null && definition.Icon == null)
                {
                    SerializedObject iconData = new SerializedObject(definition);
                    iconData.FindProperty("icon").objectReferenceValue = icon;
                    iconData.ApplyModifiedPropertiesWithoutUndo();
                }
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SerializedObject data = new SerializedObject(root.GetComponent<CombatActor>());
                SerializedProperty attacks = data.FindProperty("attacks");
                attacks.arraySize = 4;
                for (int i = 0; i < 4; i++)
                    attacks.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Scene NewScene(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            }
            else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject camera = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(camera, scene);
            Camera component = camera.AddComponent<Camera>();
            component.orthographic = true;
            component.orthographicSize = 6;
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            return scene;
        }

        private static void SaveAndClose(Scene scene, string path, bool keepOpen = false)
        {
            EditorSceneManager.SaveScene(scene, path);
            if (!keepOpen) EditorSceneManager.CloseScene(scene, true);
        }

        private static void SetObject(Object target, string field, Object value)
        {
            SerializedObject data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, params Object[] values)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty array = data.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateArena(Tile floor, Tile mud, Tile wall, TopDownTileRules rules, GameObject prefab,
            Sprite sprite, bool keepOpen)
        {
            string path = Folder + "/RpgArenaDemo.unity";
            Scene scene = NewScene(path);
            GameObject gridObject = new GameObject("Tactical Grid", typeof(Grid));
            SceneManager.MoveGameObjectToScene(gridObject, scene);
            GameObject tiles = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer), typeof(GameplayGrid2D));
            tiles.transform.SetParent(gridObject.transform);
            Tilemap map = tiles.GetComponent<Tilemap>();
            for (int x = -4; x <= 4; x++)
                for (int y = -3; y <= 3; y++)
                    map.SetTile(new Vector3Int(x, y), y == 1 && x > -3 && x < 3 ? mud : floor);
            map.SetTile(new Vector3Int(0, 0), wall);
            GameplayGrid2D world = tiles.GetComponent<GameplayGrid2D>();
            SetObject(world, "ground", map);
            SetObject(world, "rules", rules);
            GameObject a = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            a.name = "Player";
            b.name = "Enemy";
            SerializedObject enemyData = new SerializedObject(b.GetComponent<CombatActor>());
            enemyData.FindProperty("team").enumValueIndex = (int)CombatTeam.Enemy;
            enemyData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject enemyHealth = new SerializedObject(b.GetComponent<Health>());
            enemyHealth.FindProperty("maximum").intValue = 30;
            enemyHealth.FindProperty("current").intValue = 30;
            enemyHealth.ApplyModifiedPropertiesWithoutUndo();
            a.transform.position = map.GetCellCenterWorld(new Vector3Int(-2, 0));
            b.transform.position = map.GetCellCenterWorld(new Vector3Int(2, 0));
            GameObject setup = new GameObject("Arena Setup", typeof(CombatTimeline), typeof(ArenaSetup), typeof(ArenaDemoControls));
            SceneManager.MoveGameObjectToScene(setup, scene);
            CombatTimeline timeline = setup.GetComponent<CombatTimeline>();
            SetArray(setup.GetComponent<ArenaSetup>(), "gridActors", a.GetComponent<GridMover2D>(), b.GetComponent<GridMover2D>());
            SetArray(setup.GetComponent<ArenaSetup>(), "combatActors", a.GetComponent<CombatActor>(), b.GetComponent<CombatActor>());
            SetObject(setup.GetComponent<ArenaSetup>(), "grid", world);
            SetObject(setup.GetComponent<ArenaSetup>(), "timeline", timeline);
            SetObject(setup.GetComponent<ArenaDemoControls>(), "timeline", timeline);
            SetObject(setup.GetComponent<ArenaDemoControls>(), "grid", world);
            SetObject(setup.GetComponent<ArenaDemoControls>(), "uiSprite", sprite);
            SetArray(setup.GetComponent<ArenaDemoControls>(), "actors", a.GetComponent<CombatActor>(), b.GetComponent<CombatActor>());
            SaveAndClose(scene, path, keepOpen);
        }

        private static void CreateFreeScene(Sprite sprite, GameObject prefab)
        {
            string path = Folder + "/RpgFreeDemo.unity";
            if (File.Exists(path)) return;
            Scene scene = NewScene(path);
            GameObject actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            actor.transform.position = new Vector3(-3, 0, 0);
            AddWall(scene, sprite, new Vector2(0, -1), new Vector2(0.3f, 4f));
            AddWall(scene, sprite, new Vector2(1.5f, 1), new Vector2(3f, 0.3f));
            AddWall(scene, sprite, new Vector2(3, -1), new Vector2(0.3f, 4f));
            SaveAndClose(scene, path);
        }

        private static void AddWall(Scene scene, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject wall = new GameObject("Obstacle", typeof(SpriteRenderer), typeof(BoxCollider2D));
            SceneManager.MoveGameObjectToScene(wall, scene);
            wall.transform.position = position;
            wall.transform.localScale = size;
            SpriteRenderer renderer = wall.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.3f, 0.3f, 0.4f);
            wall.GetComponent<BoxCollider2D>().size = Vector2.one;
        }
    }
}
