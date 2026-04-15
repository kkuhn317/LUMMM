#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlagSlide))]
public class FlagSlideEditor : Editor
{
    SerializedProperty cutsceneMario;
    SerializedProperty optCutsceneBigMario;
    SerializedProperty marioSlideSpeed;
    SerializedProperty flagMoveSpeed;
    SerializedProperty multiplayerWaitTime;
    SerializedProperty starParticlePrefab;

    void OnEnable()
    {
        cutsceneMario       = serializedObject.FindProperty("cutsceneMario");
        optCutsceneBigMario = serializedObject.FindProperty("optCutsceneBigMario");
        marioSlideSpeed     = serializedObject.FindProperty("marioSlideSpeed");
        flagMoveSpeed       = serializedObject.FindProperty("flagMoveSpeed");
        multiplayerWaitTime = serializedObject.FindProperty("multiplayerWaitTime");
        starParticlePrefab  = serializedObject.FindProperty("starParticlePrefab");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        // Cutscene Puppets
        EditorGUILayout.LabelField("Cutscene Puppets", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cutsceneMario);
        EditorGUILayout.PropertyField(optCutsceneBigMario);

        // Slide Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Slide Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(marioSlideSpeed);
        EditorGUILayout.PropertyField(flagMoveSpeed);

        // Multiplayer Wait
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Multiplayer Wait", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(multiplayerWaitTime);

        if (multiplayerWaitTime.floatValue <= 0f)
            EditorGUILayout.HelpBox("Wait time is 0 — players will slide immediately on grab with no wait for others.", MessageType.Info);

        // Particles
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Particles", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(starParticlePrefab);

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