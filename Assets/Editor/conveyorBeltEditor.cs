using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ConveyorBelt))]
[CanEditMultipleObjects]
public class ConveyorBeltEditor : Editor
{
    private SerializedProperty _length;

    private void OnEnable()
    {
        _length = serializedObject.FindProperty(nameof(ConveyorBelt.length));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        bool isPlaying = EditorApplication.isPlaying;

        using (new EditorGUI.DisabledScope(isPlaying))
        {
            EditorGUI.BeginChangeCheck();

            int newLength = EditorGUILayout.IntField("Length", _length.intValue);

            if (EditorGUI.EndChangeCheck())
            {
                _length.intValue = newLength;
                serializedObject.ApplyModifiedProperties();

                foreach (Object t in targets)
                {
                    ConveyorBelt belt = (ConveyorBelt)t;
                    Undo.RecordObject(belt, "Change Conveyor Length");
                    belt.ChangeLength(newLength);
                    EditorUtility.SetDirty(belt);
                }
            }
        }

        if (isPlaying)
            EditorGUILayout.HelpBox("Length cannot be changed in Play Mode.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}