using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(conveyorBelt))]
public class conveyorBeltEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        conveyorBelt myScript = (conveyorBelt)target;

        EditorGUI.BeginChangeCheck();

        myScript.length = EditorGUILayout.IntField("Length", myScript.length);

        if (EditorGUI.EndChangeCheck())
        {
            myScript.ChangeLength(myScript.length);
        }

    }
}
