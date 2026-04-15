#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for FlagPoleScoring.
/// Uses the default array drawer so it behaves exactly like a normal Unity array,
/// while the ScoreBandDrawer controls how each element is displayed.
/// </summary>
[CustomEditor(typeof(FlagPoleScoring))]
public class FlagPoleScoringEditor : Editor
{
    private SerializedProperty _poleBottom;
    private SerializedProperty _poleTop;
    private SerializedProperty _useCollider;
    private SerializedProperty _scoreBands;

    private void OnEnable()
    {
        _poleBottom  = serializedObject.FindProperty("poleBottom");
        _poleTop     = serializedObject.FindProperty("poleTop");
        _useCollider = serializedObject.FindProperty("useColliderIfNoTransforms");
        _scoreBands  = serializedObject.FindProperty("scoreBands");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Pole Range", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_poleBottom);
        EditorGUILayout.PropertyField(_poleTop);
        EditorGUILayout.PropertyField(_useCollider);

        EditorGUILayout.Space();

        // Pass the full array so ScoreBandDrawer can access neighbours
        ScoreBandDrawer.ScoreBandsProperty = _scoreBands;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_scoreBands, new GUIContent("Score Bands"), includeChildren: true);
        bool changed = EditorGUI.EndChangeCheck();

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            SortBandsByMinT();
            serializedObject.ApplyModifiedProperties();
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }

        // Re-lock first band's minT to 0 always
        if (_scoreBands.arraySize > 0)
        {
            var first = _scoreBands.GetArrayElementAtIndex(0).FindPropertyRelative("minT");
            if (!Mathf.Approximately(first.floatValue, 0f))
            {
                first.floatValue = 0f;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    private void SortBandsByMinT()
    {
        // Bubble sort by minT to keep bands in order after drag reorder
        int n = _scoreBands.arraySize;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                var a = _scoreBands.GetArrayElementAtIndex(j);
                var b = _scoreBands.GetArrayElementAtIndex(j + 1);
                float minA = a.FindPropertyRelative("minT").floatValue;
                float minB = b.FindPropertyRelative("minT").floatValue;
                if (minA > minB)
                {
                    // Swap minT and points
                    int pointsA = a.FindPropertyRelative("points").intValue;
                    int pointsB = b.FindPropertyRelative("points").intValue;
                    a.FindPropertyRelative("minT").floatValue    = minB;
                    a.FindPropertyRelative("points").intValue    = pointsB;
                    b.FindPropertyRelative("minT").floatValue    = minA;
                    b.FindPropertyRelative("points").intValue    = pointsA;
                }
            }
        }
    }
}

/// <summary>
/// Draws a single ScoreBand element with a From slider and Points field.
/// Derives the range from neighbouring bands via the static ScoreBandsProperty.
/// </summary>
[CustomPropertyDrawer(typeof(FlagPoleScoring.ScoreBand))]
public class ScoreBandDrawer : PropertyDrawer
{
    // Set by FlagPoleScoringEditor before drawing so we can access neighbours
    public static SerializedProperty ScoreBandsProperty;

    private const float LineH = 18f;
    private const float Pad   = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => LineH * 3 + Pad * 4;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var minT   = property.FindPropertyRelative("minT");
        var points = property.FindPropertyRelative("points");

        // Find index and neighbours
        int   index    = GetIndex(property);
        bool  isFirst  = index == 0;
        bool  isLast   = ScoreBandsProperty == null || index == ScoreBandsProperty.arraySize - 1;

        float prevMin  = isFirst || ScoreBandsProperty == null ? 0f
            : ScoreBandsProperty.GetArrayElementAtIndex(index - 1).FindPropertyRelative("minT").floatValue;
        float derivedMax = isLast || ScoreBandsProperty == null ? 1f
            : ScoreBandsProperty.GetArrayElementAtIndex(index + 1).FindPropertyRelative("minT").floatValue;

        EditorGUI.BeginProperty(position, label, property);

        float y = position.y + Pad;
        Rect row = new Rect(position.x, y, position.width, LineH);

        // From slider
        if (isFirst)
        {
            minT.floatValue = 0f;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.Slider(row, "From", 0f, 0f, 1f);
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            float newMin = EditorGUI.Slider(row, "From", minT.floatValue,
                prevMin + 0.01f, Mathf.Max(prevMin + 0.01f, derivedMax - 0.01f));
            minT.floatValue = newMin;
        }

        // Points
        y   += LineH + Pad;
        row  = new Rect(position.x, y, position.width, LineH);
        points.intValue = EditorGUI.IntField(row, "Points", points.intValue);

        // Range read-only
        y   += LineH + Pad;
        row  = new Rect(position.x, y, position.width, LineH);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.LabelField(row, "Range",
            $"{minT.floatValue:F2}  →  {derivedMax:F2}", EditorStyles.miniLabel);
        EditorGUI.EndDisabledGroup();

        EditorGUI.EndProperty();
    }

    /// <summary>Extracts the array index from the property path (e.g. "scoreBands.Array.data[2]").</summary>
    private int GetIndex(SerializedProperty property)
    {
        string path = property.propertyPath;
        int start   = path.LastIndexOf('[') + 1;
        int end     = path.LastIndexOf(']');
        if (start > 0 && end > start && int.TryParse(path.Substring(start, end - start), out int idx))
            return idx;
        return 0;
    }
}
#endif