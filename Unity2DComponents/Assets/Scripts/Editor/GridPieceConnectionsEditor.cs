using ModularGrid;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridPieceConnections))]
public sealed class GridPieceConnectionsEditor : Editor
{
    private SerializedProperty setupMode;
    private SerializedProperty entrances;
    private SerializedProperty exits;
    private SerializedProperty openings;
    private SerializedProperty legacyPaths;
    private GridFootprint footprint;
    private int selectedIndex = -1;
    private int selectedListId = -1;

    private void OnEnable()
    {
        setupMode = serializedObject.FindProperty("setupMode");
        entrances = serializedObject.FindProperty("entrances");
        exits = serializedObject.FindProperty("exits");
        openings = serializedObject.FindProperty("openings");
        legacyPaths = serializedObject.FindProperty("paths");
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

        if (legacyPaths.arraySize > 0)
        {
            EditorGUILayout.HelpBox("This asset contains old paired paths. Migration keeps their endpoint cells and facings, and merges duplicate shared openings.", MessageType.Info);
            if (GUILayout.Button("Migrate Legacy Paths"))
            {
                Undo.RecordObject(target, "Migrate Grid Connections");
                ((GridPieceConnections)target).MigrateLegacyPaths();
                EditorUtility.SetDirty(target);
                serializedObject.Update();
                setupMode = serializedObject.FindProperty("setupMode");
                entrances = serializedObject.FindProperty("entrances");
                exits = serializedObject.FindProperty("exits");
                openings = serializedObject.FindProperty("openings");
                legacyPaths = serializedObject.FindProperty("paths");
                selectedIndex = -1;
                selectedListId = -1;
            }
        }

        EditorGUILayout.PropertyField(setupMode);
        GridConnectionSetupMode mode = (GridConnectionSetupMode)setupMode.enumValueIndex;
        if (mode == GridConnectionSetupMode.EntrancesAndExits)
        {
            DrawEndpointList("Entrances", entrances, GridConnectionFlowRole.Entrance, 0);
            DrawEndpointList("Exits", exits, GridConnectionFlowRole.Exit, 1);
        }
        else
            DrawEndpointList("Openings", openings, GridConnectionFlowRole.Either, 2);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Footprint connection grid", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(mode == GridConnectionSetupMode.EntrancesAndExits
            ? "Choose an endpoint in a list, then click an occupied cell. Unidirectional connections require an Entrance-to-Exit match; Bidirectional accepts either role. Endpoints must be adjacent and face each other."
            : "Each opening is independent and can connect as an Entrance, Exit, or Either. Choose an opening, then click an occupied cell. Bidirectional ignores flow roles; Unidirectional requires opposite roles unless an opening is Either.", MessageType.Info);
        DrawGrid(mode);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEndpointList(string label, SerializedProperty list, GridConnectionFlowRole defaultRole, int listId)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        if (GUILayout.Button("Add", GUILayout.Width(54)))
        {
            int index = list.arraySize++;
            SerializedProperty endpoint = list.GetArrayElementAtIndex(index);
            SetEndpointToFirstOccupiedCell(endpoint);
            endpoint.FindPropertyRelative("Facing").enumValueIndex = 0;
            SerializedProperty role = endpoint.FindPropertyRelative("FlowRole");
            if (role != null)
                role.enumValueIndex = (int)defaultRole;
            selectedIndex = index;
            selectedListId = listId;
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < list.arraySize;)
        {
            SerializedProperty endpoint = list.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedListId == listId && selectedIndex == i, $"#{i + 1}", "Button", GUILayout.Width(40)))
            {
                selectedListId = listId;
                selectedIndex = i;
            }

            SerializedProperty cell = endpoint.FindPropertyRelative("Cell");
            EditorGUILayout.LabelField($"Cell {cell.FindPropertyRelative("X").intValue},{cell.FindPropertyRelative("Y").intValue}", GUILayout.Width(82));
            EditorGUILayout.PropertyField(endpoint.FindPropertyRelative("Facing"), GUIContent.none, GUILayout.MinWidth(74));
            SerializedProperty flowRole = endpoint.FindPropertyRelative("FlowRole");
            if (label == "Openings")
                EditorGUILayout.PropertyField(flowRole, GUIContent.none, GUILayout.MinWidth(90));

            if (GUILayout.Button("×", GUILayout.Width(24)))
            {
                list.DeleteArrayElementAtIndex(i);
                if (selectedListId == listId && selectedIndex >= list.arraySize)
                    selectedIndex = list.arraySize - 1;
            }
            else
                i++;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawGrid(GridConnectionSetupMode mode)
    {
        for (int y = footprint.EditorGridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = 0; x < footprint.EditorGridWidth; x++)
            {
                string label = footprint.IsEditorCellOccupied(x, y) ? "■" : "·";
                if (mode == GridConnectionSetupMode.EntrancesAndExits)
                {
                    label = AddCellLabels(label, entrances, x, y, "I");
                    label = AddCellLabels(label, exits, x, y, "O");
                }
                else
                    label = AddCellLabels(label, openings, x, y, "A");

                bool wasEnabled = GUI.enabled;
                SerializedProperty selectedList = GetSelectedList();
                GUI.enabled = wasEnabled && selectedList != null && selectedIndex >= 0 &&
                              selectedIndex < selectedList.arraySize && footprint.IsEditorCellOccupied(x, y);
                if (GUILayout.Button(label, GUILayout.MinWidth(44), GUILayout.Height(34)))
                {
                    SerializedProperty endpoint = selectedList.GetArrayElementAtIndex(selectedIndex);
                    SetEndpoint(endpoint, x, y);
                }
                GUI.enabled = wasEnabled;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private SerializedProperty GetSelectedList()
    {
        switch (selectedListId)
        {
            case 0: return entrances;
            case 1: return exits;
            case 2: return openings;
            default: return null;
        }
    }

    private static string AddCellLabels(string label, SerializedProperty list, int x, int y, string prefix)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty cell = list.GetArrayElementAtIndex(i).FindPropertyRelative("Cell");
            if (cell.FindPropertyRelative("X").intValue == x && cell.FindPropertyRelative("Y").intValue == y)
                label += $" {prefix}{i + 1}";
        }
        return label;
    }

    private void SetEndpointToFirstOccupiedCell(SerializedProperty endpoint)
    {
        for (int y = 0; y < footprint.EditorGridHeight; y++)
            for (int x = 0; x < footprint.EditorGridWidth; x++)
                if (footprint.IsEditorCellOccupied(x, y))
                {
                    SetEndpoint(endpoint, x, y);
                    return;
                }
        SetEndpoint(endpoint, 0, 0);
    }

    private static void SetEndpoint(SerializedProperty endpoint, int x, int y)
    {
        SerializedProperty cell = endpoint.FindPropertyRelative("Cell");
        cell.FindPropertyRelative("X").intValue = x;
        cell.FindPropertyRelative("Y").intValue = y;
    }
}
