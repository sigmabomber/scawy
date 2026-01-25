using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KeypadSystem))]
public class KeypadSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        KeypadSystem keypad = (KeypadSystem)target;

        EditorGUILayout.LabelField("Keypad Layout", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Row 1: Keys 1, 2, 3
        EditorGUILayout.BeginHorizontal();
        keypad.key1 = (GameObject)EditorGUILayout.ObjectField(keypad.key1, typeof(GameObject), true, GUILayout.Width(80));
        keypad.key2 = (GameObject)EditorGUILayout.ObjectField(keypad.key2, typeof(GameObject), true, GUILayout.Width(80));
        keypad.key3 = (GameObject)EditorGUILayout.ObjectField(keypad.key3, typeof(GameObject), true, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // Row 2: Keys 4, 5, 6
        EditorGUILayout.BeginHorizontal();
        keypad.key4 = (GameObject)EditorGUILayout.ObjectField(keypad.key4, typeof(GameObject), true, GUILayout.Width(80));
        keypad.key5 = (GameObject)EditorGUILayout.ObjectField(keypad.key5, typeof(GameObject), true, GUILayout.Width(80));
        keypad.key6 = (GameObject)EditorGUILayout.ObjectField(keypad.key6, typeof(GameObject), true, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // Row 3: Keys 7, 8, 9
        EditorGUILayout.BeginHorizontal();
        keypad.key7 = (GameObject)EditorGUILayout.ObjectField(keypad.key7, typeof(GameObject), true, GUILayout.Width(80));
        keypad.key8 = (GameObject)EditorGUILayout.ObjectField(keypad.key8, typeof(GameObject), true, GUILayout.Width(80));
        keypad.key9 = (GameObject)EditorGUILayout.ObjectField(keypad.key9, typeof(GameObject), true, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        DrawPropertiesExcluding(serializedObject, "m_Script", "key1", "key2", "key3", "key4", "key5", "key6", "key7", "key8", "key9");
        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(keypad);
        }
    }
}