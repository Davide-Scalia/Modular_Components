using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RpgToolkit.Editor
{
    public sealed class RpgSetupWindow : EditorWindow
    {
        private RpgPreset source;
        private RpgPreset draft;
        private string characterName = "RpgCharacter";
        private string outputFolder = "Assets";
        private string presetFolder = "Assets";

        [MenuItem("Window/RPG Toolkit/Setup")]
        private static void Open() => GetWindow<RpgSetupWindow>("RPG Toolkit Setup");

        private void OnEnable() => NewDraft();
        private void OnDisable()
        {
            if (draft != null) DestroyImmediate(draft);
        }

        private void NewDraft(RpgPreset preset = null)
        {
            if (draft != null) DestroyImmediate(draft);
            source = preset;
            draft = preset == null ? CreateInstance<RpgPreset>() : Instantiate(preset);
            draft.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnGUI()
        {
            if (draft == null) NewDraft();
            EditorGUI.BeginChangeCheck();
            RpgPreset chosen = (RpgPreset)EditorGUILayout.ObjectField("Saved preset", source, typeof(RpgPreset), false);
            if (EditorGUI.EndChangeCheck()) NewDraft(chosen);

            if (draft.Preview != null)
                GUILayout.Label(AssetPreview.GetAssetPreview(draft.Preview) ?? draft.Preview.texture,
                    GUILayout.Height(120), GUILayout.ExpandWidth(false));

            SerializedObject serialized = new SerializedObject(draft);
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("description"));
            EditorGUILayout.PropertyField(serialized.FindProperty("preview"));
            EditorGUILayout.PropertyField(serialized.FindProperty("explorationMode"));
            EditorGUILayout.PropertyField(serialized.FindProperty("tacticalCombat"));
            EditorGUILayout.PropertyField(serialized.FindProperty("movementSpeed"));
            EditorGUILayout.PropertyField(serialized.FindProperty("maxMovement"));
            EditorGUILayout.PropertyField(serialized.FindProperty("characterSprite"));
            EditorGUILayout.PropertyField(serialized.FindProperty("inputActions"));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorGUILayout.Space();
            presetFolder = EditorGUILayout.TextField("Preset folder", presetFolder);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(source == null))
                if (GUILayout.Button("Save")) SavePreset(false);
            if (GUILayout.Button("Save As New")) SavePreset(true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            characterName = EditorGUILayout.TextField("Character name", characterName);
            outputFolder = EditorGUILayout.TextField("Prefab folder", outputFolder);
            string error = Validate();
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
            else EditorGUILayout.HelpBox("Creates a new Top Down 2D character prefab. World binding happens in its scene.", MessageType.Info);
            using (new EditorGUI.DisabledScope(error != null))
                if (GUILayout.Button("Create Prefab")) CreatePrefab();
        }

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(characterName) || characterName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
                return "Enter a valid character name.";
            if (!AssetDatabase.IsValidFolder(outputFolder)) return "Choose an existing prefab folder under Assets.";
            if (draft.Mode != ExplorationMode.NodeMap && draft.InputActions == null)
                return "Assign an Input Actions asset with Player/Move for this movement mode.";
            if (draft.InputActions != null && draft.InputActions.FindAction("Player/Move", false) == null)
                return "Input Actions asset needs Player/Move.";
            return null;
        }

        private void SavePreset(bool asNew)
        {
            if (asNew)
            {
                if (!AssetDatabase.IsValidFolder(presetFolder))
                {
                    EditorUtility.DisplayDialog("RPG Toolkit", "Choose an existing preset folder under Assets.", "OK");
                    return;
                }
                RpgPreset saved = Instantiate(draft);
                saved.hideFlags = HideFlags.None;
                string path = AssetDatabase.GenerateUniqueAssetPath(presetFolder + "/RpgPreset.asset");
                AssetDatabase.CreateAsset(saved, path);
                AssetDatabase.SaveAssets();
                NewDraft(saved);
            }
            else if (source != null)
            {
                Undo.RecordObject(source, "Save RPG preset");
                EditorUtility.CopySerialized(draft, source);
                EditorUtility.SetDirty(source);
                AssetDatabase.SaveAssets();
            }
        }

        private void CreatePrefab()
        {
            Selection.activeObject = RpgPrefabFactory.Create(draft, outputFolder, characterName);
        }
    }

    internal static class RpgSettings
    {
        [SettingsProvider]
        private static SettingsProvider Create() => new SettingsProvider("Project/RPG Toolkit", SettingsScope.Project)
        {
            label = "RPG Toolkit",
            guiHandler = _ =>
            {
                EditorGUILayout.HelpBox("Character presets are project assets. Export a preset with its referenced preview and input assets.", MessageType.Info);
                foreach (string guid in AssetDatabase.FindAssets("t:RpgPreset"))
                {
                    RpgPreset preset = AssetDatabase.LoadAssetAtPath<RpgPreset>(AssetDatabase.GUIDToAssetPath(guid));
                    if (preset != null) EditorGUILayout.ObjectField(preset.name, preset, typeof(RpgPreset), false);
                }
            }
        };
    }
}
