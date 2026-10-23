using ModularGrid;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridFootprint))]
public sealed class GridFootprintEditor : Editor
{
    private SerializedProperty width;
    private SerializedProperty height;
    private SerializedProperty occupied;
    private SerializedProperty useEditorGrid;
    private SerializedProperty legacyCells;

    private void OnEnable()
    {
        width = serializedObject.FindProperty("editorGridWidth");
        height = serializedObject.FindProperty("editorGridHeight");
        occupied = serializedObject.FindProperty("editorOccupiedCells");
        useEditorGrid = serializedObject.FindProperty("useEditorGrid");
        legacyCells = serializedObject.FindProperty("cells");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(useEditorGrid, new GUIContent("Use Footprint Grid"));
        if (!useEditorGrid.boolValue)
        {
            EditorGUILayout.HelpBox("Legacy offset footprint. Enable the grid editor to author occupied cells visually.", MessageType.Info);
            EditorGUILayout.PropertyField(legacyCells, true);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.PropertyField(width, new GUIContent("Grid Width"));
        EditorGUILayout.PropertyField(height, new GUIContent("Grid Height"));
        width.intValue = Mathf.Max(1, width.intValue);
        height.intValue = Mathf.Max(1, height.intValue);
        RemoveOutOfBoundsCells();

        EditorGUILayout.LabelField("Occupied cells (click to toggle)", EditorStyles.boldLabel);
        int centerX = (width.intValue - 1) / 2;
        int centerY = (height.intValue - 1) / 2;
        for (int y = height.intValue - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < width.intValue; x++)
            {
                bool isOccupied = FindCell(x, y) >= 0;
                GUI.backgroundColor = isOccupied ? new Color(0.35f, 0.8f, 0.45f) : Color.white;
                string label = x == centerX && y == centerY ? (isOccupied ? "●" : "○") : (isOccupied ? "■" : "□");
                if (GUILayout.Button(label, GUILayout.MinWidth(28), GUILayout.Height(28)))
                    ToggleCell(x, y);
            }
            EditorGUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.HelpBox("The object pivot is the exact center of this editor grid. The marked ○/● cell shows the reference cell used for integer grid occupancy; even-sized grids place the object pivot between cells.", MessageType.None);
        serializedObject.ApplyModifiedProperties();
    }

    private int FindCell(int x, int y)
    {
        for (int i = 0; i < occupied.arraySize; i++)
        {
            SerializedProperty cell = occupied.GetArrayElementAtIndex(i);
            if (cell.FindPropertyRelative("X").intValue == x && cell.FindPropertyRelative("Y").intValue == y)
                return i;
        }
        return -1;
    }

    private void ToggleCell(int x, int y)
    {
        int index = FindCell(x, y);
        if (index >= 0)
        {
            occupied.DeleteArrayElementAtIndex(index);
            return;
        }
        int size = occupied.arraySize++;
        SerializedProperty cell = occupied.GetArrayElementAtIndex(size);
        cell.FindPropertyRelative("X").intValue = x;
        cell.FindPropertyRelative("Y").intValue = y;
    }

    private void RemoveOutOfBoundsCells()
    {
        for (int i = occupied.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty cell = occupied.GetArrayElementAtIndex(i);
            int x = cell.FindPropertyRelative("X").intValue;
            int y = cell.FindPropertyRelative("Y").intValue;
            if (x < 0 || x >= width.intValue || y < 0 || y >= height.intValue)
                occupied.DeleteArrayElementAtIndex(i);
        }
    }
}
