using ModularGrid;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridPieceConnections))]
public sealed class GridPieceConnectionsEditor : Editor
{
    private SerializedProperty paths;
    private GridFootprint footprint;
    private int selectedPath;
    private bool selectedStart = true;

    private void OnEnable()
    {
        paths = serializedObject.FindProperty("paths");
        footprint = ((GridPieceConnections)target).GetComponent<GridFootprint>();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (footprint == null || !footprint.UsesEditorGrid)
        {
            EditorGUILayout.HelpBox("Enable Use Footprint Grid on the required GridFootprint component before authoring connection paths.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.LabelField("Connection paths", EditorStyles.boldLabel);
        for (int i = 0; i < paths.arraySize;)
        {
            if (!DrawPath(i))
                i++;
        }

        if (GUILayout.Button("Add Path"))
        {
            int index = paths.arraySize++;
            SerializedProperty path = paths.GetArrayElementAtIndex(index);
            SetEndpoint(path.FindPropertyRelative("Start"), 0, 0);
            SetEndpoint(path.FindPropertyRelative("End"), 0, 0);
            selectedPath = index;
            selectedStart = true;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Footprint connection grid", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select Start or End for a path, then click its occupied cell. Both ends may use the same cell; set different facings to represent separate sides. Connections work in either direction when endpoint cells are adjacent and their facings point toward each other. Use cardinal directions on square grids and the six axial directions on hex grids.", MessageType.Info);
        DrawGrid();
        serializedObject.ApplyModifiedProperties();
    }

    private bool DrawPath(int index)
    {
        SerializedProperty path = paths.GetArrayElementAtIndex(index);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Path {index + 1}", EditorStyles.boldLabel);
        if (GUILayout.Button("Remove", GUILayout.Width(64)))
        {
            paths.DeleteArrayElementAtIndex(index);
            if (selectedPath >= paths.arraySize)
                selectedPath = paths.arraySize - 1;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        EditorGUILayout.EndHorizontal();

        SerializedProperty start = path.FindPropertyRelative("Start");
        SerializedProperty end = path.FindPropertyRelative("End");
        DrawEndRow("Start", start, index, true);
        DrawEndRow("End", end, index, false);
        EditorGUILayout.EndVertical();
        return false;
    }

    private void DrawEndRow(string label, SerializedProperty endpoint, int pathIndex, bool isStart)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(selectedPath == pathIndex && selectedStart == isStart, $"Set {label}", "Button", GUILayout.Width(76)))
        {
            selectedPath = pathIndex;
            selectedStart = isStart;
        }
        SerializedProperty cell = endpoint.FindPropertyRelative("Cell");
        EditorGUILayout.LabelField($"Cell {cell.FindPropertyRelative("X").intValue},{cell.FindPropertyRelative("Y").intValue}", GUILayout.Width(74));
        EditorGUILayout.PropertyField(endpoint.FindPropertyRelative("Facing"), GUIContent.none);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {
        for (int y = footprint.EditorGridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = 0; x < footprint.EditorGridWidth; x++)
            {
                string label = footprint.IsEditorCellOccupied(x, y) ? "■" : "·";
                for (int i = 0; i < paths.arraySize; i++)
                {
                    SerializedProperty path = paths.GetArrayElementAtIndex(i);
                    if (EndpointAt(path.FindPropertyRelative("Start"), x, y)) label += $" S{i + 1}";
                    if (EndpointAt(path.FindPropertyRelative("End"), x, y)) label += $" E{i + 1}";
                }

                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && selectedPath >= 0 && selectedPath < paths.arraySize && footprint.IsEditorCellOccupied(x, y);
                if (GUILayout.Button(label, GUILayout.MinWidth(44), GUILayout.Height(34)))
                {
                    SerializedProperty endpoint = paths.GetArrayElementAtIndex(selectedPath)
                        .FindPropertyRelative(selectedStart ? "Start" : "End");
                    SetEndpoint(endpoint, x, y);
                }
                GUI.enabled = wasEnabled;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private static bool EndpointAt(SerializedProperty endpoint, int x, int y)
    {
        SerializedProperty cell = endpoint.FindPropertyRelative("Cell");
        return cell.FindPropertyRelative("X").intValue == x && cell.FindPropertyRelative("Y").intValue == y;
    }

    private static void SetEndpoint(SerializedProperty endpoint, int x, int y)
    {
        SerializedProperty cell = endpoint.FindPropertyRelative("Cell");
        cell.FindPropertyRelative("X").intValue = x;
        cell.FindPropertyRelative("Y").intValue = y;
    }
}
