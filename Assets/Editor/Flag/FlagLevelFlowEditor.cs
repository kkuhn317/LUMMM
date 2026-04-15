#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlagLevelFlow))]
public class FlagLevelFlowEditor : Editor
{
    SerializedProperty triggerMode;
    SerializedProperty useCutscene;
    SerializedProperty cutsceneSelector;
    SerializedProperty cutsceneTime;
    SerializedProperty cutsceneDelay;
    SerializedProperty slideSound;
    SerializedProperty endingMusic;

    void OnEnable()
    {
        triggerMode      = serializedObject.FindProperty("triggerMode");
        useCutscene      = serializedObject.FindProperty("useCutscene");
        cutsceneSelector = serializedObject.FindProperty("cutsceneSelector");
        cutsceneTime     = serializedObject.FindProperty("cutsceneTime");
        cutsceneDelay    = serializedObject.FindProperty("cutsceneDelay");
        slideSound       = serializedObject.FindProperty("slideSound");
        endingMusic      = serializedObject.FindProperty("endingMusic");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        // Cutscene
        EditorGUILayout.LabelField("Cutscene", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(triggerMode);
        EditorGUILayout.PropertyField(useCutscene);

        if (useCutscene.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cutsceneSelector);
            EditorGUILayout.PropertyField(cutsceneTime);
            EditorGUILayout.PropertyField(cutsceneDelay);
            EditorGUI.indentLevel--;
        }

        // Audio
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(slideSound);

        using (new EditorGUI.DisabledScope(useCutscene.boolValue))
            EditorGUILayout.PropertyField(endingMusic);

        if (useCutscene.boolValue)
            EditorGUILayout.HelpBox("Ending Music is unused when Use Cutscene is enabled — the cutscene handles its own audio.", MessageType.Info);

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