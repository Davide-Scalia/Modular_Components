using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RpgToolkit.Editor
{
    [CustomEditor(typeof(EncounterDefinition))]
    public sealed class EncounterDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("kind"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lootPool"));

            SerializedProperty path = serializedObject.FindProperty("scenePath");
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
            SceneAsset selected = (SceneAsset)EditorGUILayout.ObjectField(
                "Destination Scene", scene, typeof(SceneAsset), false);
            if (selected != scene)
                path.stringValue = selected == null ? string.Empty : AssetDatabase.GetAssetPath(selected);

            if (!string.IsNullOrEmpty(path.stringValue) &&
                !EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == path.stringValue))
                EditorGUILayout.HelpBox("Add the destination scene to Build Settings before playing.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
