using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DemoScreenEffects))]
public class DemoScreenEffectsEditor : Editor
{
    SerializedProperty demoStandard_colProp;
    SerializedProperty demoStandard_iNumRowsProp;
    SerializedProperty demoStandard_bFillColumnsFirstProp;
    SerializedProperty demoStandard_fDelayProp;
    SerializedProperty demoStandard_bIsLeftProp;
    SerializedProperty demoStandard_bIsBottomProp;

    SerializedProperty demoDiag_colProp;
    SerializedProperty demoDiag_iNumRowsProp;
    SerializedProperty demoDiag_fDelayProp;
    SerializedProperty demoDiag_bIsLeftProp;
    SerializedProperty demoDiag_bIsBottomProp;

    SerializedProperty demoHorBan_colProp;
    SerializedProperty demoHorBan_iNumBanProp;
    SerializedProperty demoHorBan_fDelayProp;
    SerializedProperty demoHorBan_fAcrossTimeProp;
    SerializedProperty demoHorBan_bIsLeftProp;
    SerializedProperty demoHorBan_bIsEnteringProp;
    SerializedProperty demoHorBan_bIsTopProp;
    SerializedProperty demoHorBan_animCurveProp;

    SerializedProperty demoVerBan_colProp;
    SerializedProperty demoVerBan_iNumBanProp;
    SerializedProperty demoVerBan_fDelayProp;
    SerializedProperty demoVerBan_fAcrossTimeProp;
    SerializedProperty demoVerBan_bIsLeftProp;
    SerializedProperty demoVerBan_bIsEnteringProp;
    SerializedProperty demoVerBan_bIsTopProp;
    SerializedProperty demoVerBan_animCurveProp;

    SerializedProperty demoBar_colProp;
    SerializedProperty demoBar_fTimeProp;
    SerializedProperty demoBar_fProportionProp;
    SerializedProperty demoBar_bIsHorizontalProp;
    SerializedProperty demoBar_bIsEnteringProp;
    SerializedProperty demoBar_animCurveProp;

    SerializedProperty demoKey_keyholeProp;
    SerializedProperty demoKey_ColProp;
    SerializedProperty demoKey_AnimProp;
    SerializedProperty demoKey_fTimeProp;
    SerializedProperty demoKey_targetProp;
    SerializedProperty demoKey_bIsEnteringProp;


    public void OnEnable()
    {
        demoStandard_colProp = serializedObject.FindProperty("demoStandard_col");
        demoStandard_iNumRowsProp = serializedObject.FindProperty("demoStandard_iNumRows");
        demoStandard_bFillColumnsFirstProp = serializedObject.FindProperty("demoStandard_bFillColumnsFirst");
        demoStandard_fDelayProp = serializedObject.FindProperty("demoStandard_fDelay");
        demoStandard_bIsLeftProp = serializedObject.FindProperty("demoStandard_bIsLeft");
        demoStandard_bIsBottomProp = serializedObject.FindProperty("demoStandard_bIsBottom");

        demoDiag_colProp = serializedObject.FindProperty("demoDiag_col");
        demoDiag_iNumRowsProp = serializedObject.FindProperty("demoDiag_iNumRows");
        demoDiag_fDelayProp = serializedObject.FindProperty("demoDiag_fDelay");
        demoDiag_bIsLeftProp = serializedObject.FindProperty("demoDiag_bIsLeft");
        demoDiag_bIsBottomProp = serializedObject.FindProperty("demoDiag_bIsBottom");
        
        demoHorBan_colProp = serializedObject.FindProperty("demoHorBan_col");
        demoHorBan_iNumBanProp = serializedObject.FindProperty("demoHorBan_iNumBan");
        demoHorBan_fDelayProp = serializedObject.FindProperty("demoHorBan_fDelay");
        demoHorBan_fAcrossTimeProp = serializedObject.FindProperty("demoHorBan_fAcrossTime");
        demoHorBan_bIsLeftProp = serializedObject.FindProperty("demoHorBan_bIsLeft");
        demoHorBan_bIsEnteringProp = serializedObject.FindProperty("demoHorBan_bIsEntering");
        demoHorBan_bIsTopProp = serializedObject.FindProperty("demoHorBan_bIsTop");
        demoHorBan_animCurveProp = serializedObject.FindProperty("demoHorBan_animCurve");

        demoVerBan_colProp = serializedObject.FindProperty("demoVerBan_col");
        demoVerBan_iNumBanProp = serializedObject.FindProperty("demoVerBan_iNumBan");
        demoVerBan_fDelayProp = serializedObject.FindProperty("demoVerBan_fDelay");
        demoVerBan_fAcrossTimeProp = serializedObject.FindProperty("demoVerBan_fAcrossTime");
        demoVerBan_bIsLeftProp = serializedObject.FindProperty("demoVerBan_bIsLeft");
        demoVerBan_bIsEnteringProp = serializedObject.FindProperty("demoVerBan_bIsEntering");
        demoVerBan_bIsTopProp = serializedObject.FindProperty("demoVerBan_bIsTop");
        demoVerBan_animCurveProp = serializedObject.FindProperty("demoVerBan_animCurve");

        demoBar_colProp = serializedObject.FindProperty("demoBar_col");
        demoBar_fTimeProp = serializedObject.FindProperty("demoBar_fTime");
        demoBar_fProportionProp = serializedObject.FindProperty("demoBar_fProportion");
        demoBar_bIsHorizontalProp = serializedObject.FindProperty("demoBar_bIsHorizontal");
        demoBar_bIsEnteringProp = serializedObject.FindProperty("demoBar_bIsEntering");
        demoBar_animCurveProp = serializedObject.FindProperty("demoBar_animCurve");

        demoKey_keyholeProp = serializedObject.FindProperty("demoKey_keyhole");
        demoKey_AnimProp = serializedObject.FindProperty("demoKey_anim");
        demoKey_ColProp = serializedObject.FindProperty("demoKey_Col");
        demoKey_fTimeProp = serializedObject.FindProperty("demoKey_fTime");
        demoKey_targetProp = serializedObject.FindProperty("demoKey_target");
        demoKey_bIsEnteringProp = serializedObject.FindProperty("demoKey_bIsEntering");
    }

    public override void OnInspectorGUI()
    {
        DemoScreenEffects s = (DemoScreenEffects)target;

        //DrawDefaultInspector();

        if (Application.isPlaying)
        {
            GUILayout.Space(25);
            if (GUILayout.Button("Clear Effects"))
            {
                ScreenEffects.Clear();
            }
            GUILayout.Space(25);
        }

        //==============================
        //KEYHOLE
        //==============================

        GUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Keyhole", EditorStyles.boldLabel);

        GUILayout.Space(10);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Demo Keyhole"))
            {
                s.DemoKeyhole();
            }
        }

        EditorGUILayout.PropertyField(demoKey_keyholeProp, new GUIContent("Keyhole Prefab:"));
        EditorGUILayout.PropertyField(demoKey_ColProp, new GUIContent("Color:"));
        EditorGUILayout.PropertyField(demoKey_AnimProp, new GUIContent("Animation Curve:"));
        EditorGUILayout.PropertyField(demoKey_fTimeProp, new GUIContent("Time"));
        EditorGUILayout.PropertyField(demoKey_targetProp, new GUIContent("Worldspace Target"));
        EditorGUILayout.PropertyField(demoKey_bIsEnteringProp, new GUIContent("Keyhole entering?"));

        GUILayout.EndVertical();

        GUILayout.Space(25);

        //==============================
        //AXIAL TILING
        //==============================

        GUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Axial Tiling", EditorStyles.boldLabel);

        

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Demo Axial Tiling"))
            {
                s.DemoTileScreen();
            }
        }

        EditorGUILayout.PropertyField(demoStandard_colProp, new GUIContent("Tile color:"));
        EditorGUILayout.PropertyField(demoStandard_iNumRowsProp, new GUIContent("Number of rows:"));
        EditorGUILayout.PropertyField(demoStandard_fDelayProp, new GUIContent("Delay between tiles:"));
        EditorGUILayout.PropertyField(demoStandard_bIsLeftProp, new GUIContent("Start on the left?"));
        EditorGUILayout.PropertyField(demoStandard_bIsBottomProp, new GUIContent("Start at the bottom?"));
        EditorGUILayout.PropertyField(demoStandard_bFillColumnsFirstProp, new GUIContent("Fill vertically?"));

        GUILayout.EndVertical();

        GUILayout.Space(25);

        //==============================
        //DIAGONAL TILING
        //==============================

        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Diagonal Tiling", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Demo Diagonal Tiling"))
            {
                s.DemoTileScreenDiagonally();
            }
        }

        EditorGUILayout.PropertyField(demoDiag_colProp, new GUIContent("Tile color:"));
        EditorGUILayout.PropertyField(demoDiag_iNumRowsProp, new GUIContent("Number of rows:"));
        EditorGUILayout.PropertyField(demoDiag_fDelayProp, new GUIContent("Delay between tiles:"));
        EditorGUILayout.PropertyField(demoDiag_bIsLeftProp, new GUIContent("Start on the left?"));
        EditorGUILayout.PropertyField(demoDiag_bIsBottomProp, new GUIContent("Start at the bottom?"));

        GUILayout.EndVertical();

        GUILayout.Space(25);

        //==============================
        //HORIZONTAL BANNERS
        //==============================

        GUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Horizontal Banners", EditorStyles.boldLabel);

        GUILayout.Space(10);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Demo Horizontal Banners"))
            {
                s.DemoHorizontalBanners();
            }
        }
        
        EditorGUILayout.PropertyField(demoHorBan_colProp, new GUIContent("Banner color:"));
        EditorGUILayout.PropertyField(demoHorBan_animCurveProp, new GUIContent("Animation Curve:"));
        EditorGUILayout.PropertyField(demoHorBan_iNumBanProp, new GUIContent("Number of banners:"));
        EditorGUILayout.PropertyField(demoHorBan_fAcrossTimeProp, new GUIContent("Time for one banner to cross:"));
        EditorGUILayout.PropertyField(demoHorBan_fDelayProp, new GUIContent("Delay between banners:"));
        EditorGUILayout.PropertyField(demoHorBan_bIsLeftProp, new GUIContent("Start on the left?"));
        EditorGUILayout.PropertyField(demoHorBan_bIsTopProp, new GUIContent("Start at the top?"));
        EditorGUILayout.PropertyField(demoHorBan_bIsEnteringProp, new GUIContent("Banners entering?"));

        GUILayout.EndVertical();

        GUILayout.Space(25);

        //==============================
        //VERTICAL BANNERS
        //==============================

        GUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Vertical Banners", EditorStyles.boldLabel);

        GUILayout.Space(10);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Demo Vertical Banners"))
            {
                s.DemoVerticalBanners();
            }
        }
        
        EditorGUILayout.PropertyField(demoVerBan_colProp, new GUIContent("Banner color:"));
        EditorGUILayout.PropertyField(demoVerBan_animCurveProp, new GUIContent("Animation Curve:"));
        EditorGUILayout.PropertyField(demoVerBan_iNumBanProp, new GUIContent("Number of banners:"));
        EditorGUILayout.PropertyField(demoVerBan_fAcrossTimeProp, new GUIContent("Time for one banner to cross:"));
        EditorGUILayout.PropertyField(demoVerBan_fDelayProp, new GUIContent("Delay between banners:"));
        EditorGUILayout.PropertyField(demoVerBan_bIsLeftProp, new GUIContent("Start on the left?"));
        EditorGUILayout.PropertyField(demoVerBan_bIsTopProp, new GUIContent("Start at the top?"));
        EditorGUILayout.PropertyField(demoVerBan_bIsEnteringProp, new GUIContent("Banners entering?"));

        GUILayout.EndVertical();

        GUILayout.Space(25);

        //==============================
        //BARS
        //==============================

        GUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Bars", EditorStyles.boldLabel);

        GUILayout.Space(10);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Demo Bars"))
            {
                s.DemoBars();
            }
        }

        EditorGUILayout.PropertyField(demoBar_colProp, new GUIContent("Bar color:"));
        EditorGUILayout.PropertyField(demoBar_animCurveProp, new GUIContent("Animation Curve:"));
        EditorGUILayout.PropertyField(demoBar_fTimeProp, new GUIContent("Total time:"));
        EditorGUILayout.PropertyField(demoBar_fProportionProp, new GUIContent("Proportion of screen filled by one banner:"));
        EditorGUILayout.PropertyField(demoBar_bIsHorizontalProp, new GUIContent("Horizontal banners?"));
        EditorGUILayout.PropertyField(demoBar_bIsEnteringProp, new GUIContent("Banners entering?"));

        GUILayout.EndVertical();

        GUILayout.Space(25);

        serializedObject.ApplyModifiedProperties();
    }
}