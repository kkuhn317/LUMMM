#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Flag))]
public class FlagEditor : Editor
{
    SerializedProperty flag;
    SerializedProperty pole;
    SerializedProperty flagOnRight;
    SerializedProperty playerSide;
    SerializedProperty height;

    void OnEnable()
    {
        flag        = serializedObject.FindProperty("flag");
        pole        = serializedObject.FindProperty("pole");
        flagOnRight = serializedObject.FindProperty("flagOnRight");
        playerSide  = serializedObject.FindProperty("playerSide");
        height      = serializedObject.FindProperty("height");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        // References
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flag);
        EditorGUILayout.PropertyField(pole);

        if (flag.objectReferenceValue == null || pole.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign both Flag and Pole objects to configure the pole height.", MessageType.Warning);

        // Layout
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(flagOnRight);
        EditorGUILayout.PropertyField(playerSide);
        EditorGUILayout.PropertyField(height);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            ((Flag)target).ChangeHeight(height.floatValue);
            return;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Script",
                MonoScript.FromMonoBehaviour((MonoBehaviour)target),
                typeof(MonoScript), false);
        EditorGUILayout.Space();
    }
}
#endif