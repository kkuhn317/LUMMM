using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(Flag))]
[CanEditMultipleObjects]
public class FlagEditor : Editor
{

    SerializedProperty height;

    void OnEnable()
    {
        height = serializedObject.FindProperty("height");
    }

    public override void OnInspectorGUI()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            return;
        }

        serializedObject.Update();

        DrawDefaultInspector();

        Flag myScript = (Flag)target;

        EditorGUI.BeginChangeCheck();

        height.floatValue = EditorGUILayout.FloatField("Height", height.floatValue);

        if (EditorGUI.EndChangeCheck())
        {
            myScript.ChangeHeight(height.floatValue);
        }

        serializedObject.ApplyModifiedProperties();

    }
}
