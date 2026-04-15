#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlagArrival))]
public class FlagArrivalEditor : Editor
{
    SerializedProperty postSlideTarget;
    SerializedProperty arrivalMode;
    SerializedProperty playerSpacing;
    SerializedProperty walkSpeed;
    SerializedProperty jumpHeight;
    SerializedProperty jumpDuration;
    SerializedProperty hopDistance;
    SerializedProperty gravity;
    SerializedProperty hidePuppetOnArrival;

    void OnEnable()
    {
        postSlideTarget     = serializedObject.FindProperty("postSlideTarget");
        arrivalMode         = serializedObject.FindProperty("arrivalMode");
        playerSpacing       = serializedObject.FindProperty("playerSpacing");
        walkSpeed           = serializedObject.FindProperty("walkSpeed");
        jumpHeight          = serializedObject.FindProperty("jumpHeight");
        jumpDuration        = serializedObject.FindProperty("jumpDuration");
        hopDistance         = serializedObject.FindProperty("hopDistance");
        gravity             = serializedObject.FindProperty("gravity");
        hidePuppetOnArrival = serializedObject.FindProperty("hidePuppetOnArrival");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        // Post-Slide Target
        EditorGUILayout.LabelField("Post-Slide Target", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(postSlideTarget);
        EditorGUILayout.PropertyField(arrivalMode);

        var mode = (FlagArrival.ArrivalMode)arrivalMode.enumValueIndex;

        if (mode != FlagArrival.ArrivalMode.None)
        {
            EditorGUILayout.PropertyField(playerSpacing);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(walkSpeed);
            EditorGUILayout.PropertyField(gravity);

            bool needsJump = mode == FlagArrival.ArrivalMode.Jump ||
                             mode == FlagArrival.ArrivalMode.HopThenWalk;
            if (needsJump)
            {
                EditorGUILayout.PropertyField(jumpHeight);
                EditorGUILayout.PropertyField(jumpDuration);
            }

            if (mode == FlagArrival.ArrivalMode.HopThenWalk)
                EditorGUILayout.PropertyField(hopDistance);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Puppet", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hidePuppetOnArrival);

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