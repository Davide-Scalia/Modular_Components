using UnityEditor;
using UnityEngine;

namespace RpgToolkit.Editor
{
    [CustomEditor(typeof(AttackPattern))]
    public sealed class AttackPatternEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty width = serializedObject.FindProperty("width");
            SerializedProperty height = serializedObject.FindProperty("height");
            SerializedProperty cells = serializedObject.FindProperty("cells");
            int requestedWidth = Mathf.Clamp(EditorGUILayout.IntField("Width (odd)", width.intValue), 1, 21);
            width.intValue = requestedWidth % 2 == 0 ? Mathf.Min(21, requestedWidth + 1) : requestedWidth;
            height.intValue = Mathf.Clamp(EditorGUILayout.IntField("Height", height.intValue), 1, 21);
            cells.arraySize = width.intValue * height.intValue;
            EditorGUILayout.HelpBox("The O square is the actor. Select target squares above or beside it. The pattern rotates with facing.", MessageType.Info);
            for (int y = height.intValue - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < width.intValue; x++)
                {
                    bool origin = x == width.intValue / 2 && y == 0;
                    SerializedProperty cell = cells.GetArrayElementAtIndex(y * width.intValue + x);
                    if (origin) cell.boolValue = false;
                    using (new EditorGUI.DisabledScope(origin))
                        cell.boolValue = GUILayout.Toggle(cell.boolValue, origin ? "O" : "X", "Button", GUILayout.Width(32), GUILayout.Height(32));
                }
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
