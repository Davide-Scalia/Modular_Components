using UnityEditor;
using UnityEngine;

namespace RpgToolkit.Editor
{
    internal static class RpgPrefabFactory
    {
        public static GameObject Create(RpgPreset preset, string folder, string characterName, bool replaceDemo = false)
        {
            GameObject root = new GameObject(characterName);
            try
            {
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.freezeRotation = true;
                root.AddComponent<CircleCollider2D>();
                CharacterIntent intent = root.AddComponent<CharacterIntent>();
                CharacterInput2D input = null;
                if (preset.Mode != ExplorationMode.NodeMap)
                {
                    input = root.AddComponent<CharacterInput2D>();
                    input.Configure(preset.InputActions, intent, preset.Mode);
                }
                if (preset.Mode == ExplorationMode.Free)
                    root.AddComponent<FreeMover2D>().Configure(preset.MovementSpeed);
                else if (preset.Mode == ExplorationMode.PointAndClick)
                    root.AddComponent<PointClickMover2D>().Configure(preset.MovementSpeed);

                GridMover2D gridMover = null;
                if (preset.Mode == ExplorationMode.Grid || preset.TacticalCombat)
                {
                    gridMover = root.AddComponent<GridMover2D>();
                    gridMover.Configure(preset.MovementSpeed, preset.MaxMovement);
                    gridMover.enabled = preset.Mode == ExplorationMode.Grid;
                }
                if (preset.TacticalCombat)
                {
                    EntityStats stats = root.AddComponent<EntityStats>();
                    stats.SetMaxMovement(preset.MaxMovement);
                    root.AddComponent<Health>();
                    root.AddComponent<CombatActor>().Configure(gridMover, input, preset.Mode, preset.MaxMovement);
                    root.AddComponent<AttackExecutor>();
                }

                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                visual.AddComponent<SpriteRenderer>().sprite = preset.CharacterSprite;

                string path = folder + "/" + characterName + ".prefab";
                if (!replaceDemo) path = AssetDatabase.GenerateUniqueAssetPath(path);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null) throw new System.InvalidOperationException("Prefab generation failed.");
                return saved;
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
