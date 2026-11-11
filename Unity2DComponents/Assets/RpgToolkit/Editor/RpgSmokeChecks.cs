using System;
using ModularGrid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace RpgToolkit.Editor
{
    public static class RpgSmokeChecks
    {
        [MenuItem("Window/RPG Toolkit/Run Arena Regression Checks")]
        public static void RunArenaRegressionChecks()
        {
            GameObject first = null, second = null, visual = null, redObject = null, yellowObject = null;
            try
            {
                first = new GameObject("Arena Check Player");
                second = new GameObject("Arena Check Enemy");
                CombatActor player = first.AddComponent<CombatActor>();
                CombatActor enemy = second.AddComponent<CombatActor>();
                CombatTimeline timeline = first.AddComponent<CombatTimeline>();
                timeline.Begin(new[] { player, enemy });
                Check(timeline.Current == player, "player begins");
                Check(timeline.Submit(player, 25, 0, null) && timeline.Current == enemy,
                    "first action yields to enemy");
                Check(timeline.Submit(enemy, 100, 0, null) && timeline.Current == player,
                    "player becomes ready after enemy action");
                Check(player.TryMarkAttack() && timeline.SpendTime(player, 10), "player can attack and spend time");
                Check(timeline.FinishActivation(player) && timeline.Current == enemy && timeline.Time == 100,
                    "ending activation yields to enemy even when enemy readiness is later");
                Check(timeline.Submit(enemy, 10, 0, null) && timeline.Current == player && !player.HasAttacked,
                    "next player activation restores attack");
                timeline.End();

                Health health = first.AddComponent<Health>();
                visual = new GameObject("Arena Check Health Visual", typeof(HealthVisual));
                redObject = new GameObject("Red", typeof(RectTransform), typeof(Image));
                yellowObject = new GameObject("Yellow", typeof(RectTransform), typeof(Image));
                Image red = redObject.GetComponent<Image>();
                Image yellow = yellowObject.GetComponent<Image>();
                red.type = Image.Type.Filled;
                yellow.type = Image.Type.Filled;
                visual.GetComponent<HealthVisual>().Configure(health, red, yellow);
                health.TakeDamage(10);
                Check(health.Current == 90 && Mathf.Approximately(red.fillAmount, 0.9f) &&
                    Mathf.Approximately(yellow.fillAmount, 1f), "red health fill changes while yellow trails");
                Debug.Log("RPG arena regression checks passed: turn handoff, attack reset, health fill.");
            }
            finally
            {
                if (visual != null) UnityEngine.Object.DestroyImmediate(visual);
                if (redObject != null) UnityEngine.Object.DestroyImmediate(redObject);
                if (yellowObject != null) UnityEngine.Object.DestroyImmediate(yellowObject);
                if (first != null) UnityEngine.Object.DestroyImmediate(first);
                if (second != null) UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [MenuItem("Window/RPG Toolkit/Run Smoke Checks")]
        public static void Run()
        {
            GameObject root = null, firstObject = null, secondObject = null;
            Tile floor = null, mud = null;
            TopDownTileRules rules = null;
            GameObject mapRoot = null, foreignNode = null;
            AttackPattern pattern = null;
            LootPool loot = null;
            LootItem item = null;
            try
            {
                floor = ScriptableObject.CreateInstance<Tile>();
                mud = ScriptableObject.CreateInstance<Tile>();
                rules = ScriptableObject.CreateInstance<TopDownTileRules>();
                SerializedObject data = new SerializedObject(rules);
                SerializedProperty entries = data.FindProperty("entries");
                entries.arraySize = 2;
                SetRule(entries.GetArrayElementAtIndex(0), floor, 10);
                SetRule(entries.GetArrayElementAtIndex(1), mud, 50);
                data.ApplyModifiedPropertiesWithoutUndo();

                root = new GameObject("Check Grid", typeof(Grid));
                GameObject ground = new GameObject("Ground", typeof(Tilemap), typeof(GameplayGrid2D));
                ground.transform.SetParent(root.transform);
                Tilemap tilemap = ground.GetComponent<Tilemap>();
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 2; y++) tilemap.SetTile(new Vector3Int(x, y), floor);
                tilemap.SetTile(new Vector3Int(1, 0), mud);
                GameplayGrid2D world = ground.GetComponent<GameplayGrid2D>();
                data = new SerializedObject(world);
                data.FindProperty("ground").objectReferenceValue = tilemap;
                data.FindProperty("rules").objectReferenceValue = rules;
                data.ApplyModifiedPropertiesWithoutUndo();

                GridCoordinate start = new GridCoordinate(0, 0), goal = new GridCoordinate(2, 0);
                Check(!GridPathfinder.Reachable(world, start, 30).ContainsKey(goal), "budget 30 cannot reach");
                Check(GridPathfinder.Reachable(world, start, 40)[goal] == 40, "cheaper detour cost 40");
                Check(GridPathfinder.Path(world, start, goal, 40).Count == 4, "A* takes detour");
                Check(GridPathfinder.Reachable(world, start, 0).Count == 1, "zero budget");
                Check(!world.TryTraversal(start, new GridCoordinate(1, 1), out _), "no diagonal");
                firstObject = new GameObject("Check Actor A");
                secondObject = new GameObject("Check Actor B");
                CombatActor a = firstObject.AddComponent<CombatActor>();
                CombatActor b = secondObject.AddComponent<CombatActor>();
                Check(world.TryRegister(new GridCoordinate(1, 1), b), "actor registration");
                Check(!world.TryTraversal(new GridCoordinate(0, 1), new GridCoordinate(1, 1), out _), "occupied cell blocks");
                world.Unregister(new GridCoordinate(1, 1), b);

                mapRoot = new GameObject("Check Node Map");
                mapRoot.SetActive(false);
                MapNode origin = new GameObject("Start").AddComponent<MapNode>();
                MapNode branch = new GameObject("Branch").AddComponent<MapNode>();
                MapNode deadEnd = new GameObject("Dead End").AddComponent<MapNode>();
                origin.transform.SetParent(mapRoot.transform);
                branch.transform.SetParent(mapRoot.transform);
                deadEnd.transform.SetParent(mapRoot.transform);
                data = new SerializedObject(origin);
                SerializedProperty links = data.FindProperty("links");
                links.arraySize = 1;
                links.GetArrayElementAtIndex(0).objectReferenceValue = branch;
                data.ApplyModifiedPropertiesWithoutUndo();
                NodeMapNavigator navigator = mapRoot.AddComponent<NodeMapNavigator>();
                data = new SerializedObject(navigator);
                data.FindProperty("startNode").objectReferenceValue = origin;
                SerializedProperty nodes = data.FindProperty("nodes");
                nodes.arraySize = 3;
                nodes.GetArrayElementAtIndex(0).objectReferenceValue = origin;
                nodes.GetArrayElementAtIndex(1).objectReferenceValue = branch;
                nodes.GetArrayElementAtIndex(2).objectReferenceValue = deadEnd;
                data.ApplyModifiedPropertiesWithoutUndo();
                mapRoot.SetActive(true);
                Check(navigator.TrySelect(branch) && navigator.Current == branch, "scene node link");
                navigator.Restore(origin);
                foreignNode = new GameObject("Foreign Node");
                Check(!navigator.TrySelect(foreignNode.AddComponent<MapNode>()), "foreign node rejected");

                CombatTimeline timeline = firstObject.AddComponent<CombatTimeline>();
                timeline.Begin(new[] { a, b });
                Check(timeline.Current == a && timeline.Time == 0, "stable first actor");
                Check(timeline.Submit(a, 25, 0, null) && timeline.Current == b, "item delays actor");
                Check(timeline.Submit(b, 100, 0, null) && timeline.Current == a && timeline.Time == 25,
                    "cheaper item returns actor earlier");
                Check(!timeline.Submit(b, 1, 0, null), "inactive actor cannot act");
                int fired = 0;
                Check(timeline.ScheduleExternal(5, () => fired++), "schedule event");
                Check(timeline.SpendTime(a, 10) && timeline.Time == 35 && fired == 1,
                    "movement time processes due event once");
                Check(a.TryMarkAttack(), "actor can attack during activation");
                Check(timeline.FinishActivation(a) && timeline.Current == b && timeline.Time == 100,
                    "ending activation yields to the next actor");
                Check(timeline.Submit(b, 10, 0, null) && timeline.Current == a && !a.HasAttacked,
                    "next activation restores attack availability");
                timeline.End();

                pattern = ScriptableObject.CreateInstance<AttackPattern>();
                data = new SerializedObject(pattern);
                data.FindProperty("cells").GetArrayElementAtIndex(4).boolValue = true;
                data.ApplyModifiedPropertiesWithoutUndo();
                Check(pattern.Contains(0, 1) && !pattern.Contains(0, 0) && !pattern.Contains(2, 1),
                    "pattern uses bottom center origin");
                foreach (Vector2Int direction in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                {
                    int count = 0;
                    foreach (GridCoordinate cell in pattern.Cells(start, direction))
                    {
                        Check(cell == new GridCoordinate(direction.x, direction.y), "pattern rotates with aim");
                        count++;
                    }
                    Check(count == 1, "pattern iterator matches selected cells");
                }
                item = ScriptableObject.CreateInstance<LootItem>();
                loot = ScriptableObject.CreateInstance<LootPool>();
                data = new SerializedObject(loot);
                data.FindProperty("rolls").intValue = 2;
                SerializedProperty drop = data.FindProperty("entries");
                drop.arraySize = 1;
                drop.GetArrayElementAtIndex(0).FindPropertyRelative("Item").objectReferenceValue = item;
                drop.GetArrayElementAtIndex(0).FindPropertyRelative("Weight").intValue = 1;
                drop.GetArrayElementAtIndex(0).FindPropertyRelative("MinCount").intValue = 1;
                drop.GetArrayElementAtIndex(0).FindPropertyRelative("MaxCount").intValue = 1;
                data.ApplyModifiedPropertiesWithoutUndo();
                Check(loot.Roll(new System.Random(1)).Count == 2, "loot pool rolls configured count");
                Health health = firstObject.AddComponent<Health>();
                health.TakeDamage(10);
                Check(health.Current == 90 && health.IsAlive, "damage reduces current health");

                Debug.Log("RPG Toolkit smoke checks passed: weighted paths, budget, occupancy, map links, timeline, loot, pattern, health.");
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (firstObject != null) UnityEngine.Object.DestroyImmediate(firstObject);
                if (secondObject != null) UnityEngine.Object.DestroyImmediate(secondObject);
                if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
                if (mud != null) UnityEngine.Object.DestroyImmediate(mud);
                if (rules != null) UnityEngine.Object.DestroyImmediate(rules);
                if (mapRoot != null) UnityEngine.Object.DestroyImmediate(mapRoot);
                if (foreignNode != null) UnityEngine.Object.DestroyImmediate(foreignNode);
                if (pattern != null) UnityEngine.Object.DestroyImmediate(pattern);
                if (loot != null) UnityEngine.Object.DestroyImmediate(loot);
                if (item != null) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("RPG check failed: " + message);
        }

        private static void SetRule(SerializedProperty entry, Tile tile, int cost)
        {
            entry.FindPropertyRelative("Tile").objectReferenceValue = tile;
            entry.FindPropertyRelative("Walkable").boolValue = true;
            entry.FindPropertyRelative("EntryCost").intValue = cost;
            entry.FindPropertyRelative("OpenEdges").intValue = (int)CardinalEdge.All;
        }
    }
}
