using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ConveyorBelt))]
[CanEditMultipleObjects]
public class ConveyorBeltEditor : Editor
{
    SerializedProperty length;

    void OnEnable()
    {
        length = serializedObject.FindProperty(nameof(ConveyorBelt.length));
    }

    public override void OnInspectorGUI()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            return;
        }
        
        ConveyorBelt myScript = (ConveyorBelt)target;

        serializedObject.Update();

        DrawDefaultInspector();

        length.intValue = EditorGUILayout.IntField("Length", length.intValue);

        if (EditorGUI.EndChangeCheck())
        {
            myScript.ChangeLength(length.intValue);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
