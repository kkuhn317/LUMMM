// Place this file in any Editor/ folder in your project.
// Window → Sprite Library Merger

using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;
using System.Collections.Generic;

public class SpriteLibraryMerger : EditorWindow
{
    private SpriteLibraryAsset _source;
    private SpriteLibraryAsset _target;

    private List<(string category, string label, Sprite sprite)> _missingEntries = new();
    private Vector2 _scroll;
    private bool _scanned = false;

    [MenuItem("Window/Sprite Library Merger")]
    public static void ShowWindow() => GetWindow<SpriteLibraryMerger>("Sprite Library Merger");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Sprite Library Merger", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Copies missing categories and labels from Source into Target.\n" +
            "Existing entries in Target are never overwritten.",
            MessageType.Info);

        EditorGUILayout.Space();

        var newSource = (SpriteLibraryAsset)EditorGUILayout.ObjectField("Source (has the sprites)", _source, typeof(SpriteLibraryAsset), false);
        var newTarget = (SpriteLibraryAsset)EditorGUILayout.ObjectField("Target (missing entries)",  _target, typeof(SpriteLibraryAsset), false);

        if (newSource != _source || newTarget != _target)
        {
            _source = newSource;
            _target = newTarget;
            _scanned = false;
            _missingEntries.Clear();
        }

        EditorGUILayout.Space();

        GUI.enabled = _source != null && _target != null;
        if (GUILayout.Button("Scan for Missing Entries"))
            Scan();
        GUI.enabled = true;

        if (_scanned)
            DrawScanResults();
    }

    private void Scan()
    {
        _missingEntries.Clear();

        foreach (string cat in _source.GetCategoryNames())
        {
            var targetLabels = new HashSet<string>();
            foreach (string l in _target.GetCategoryLabelNames(cat) ?? new List<string>())
                targetLabels.Add(l);

            foreach (string label in _source.GetCategoryLabelNames(cat) ?? new List<string>())
            {
                if (!targetLabels.Contains(label))
                {
                    Sprite sprite = _source.GetSprite(cat, label);
                    _missingEntries.Add((cat, label, sprite));
                }
            }
        }

        _scanned = true;
    }

    private void DrawScanResults()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scan Results", EditorStyles.boldLabel);

        if (_missingEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("No missing entries — Target is up to date!", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Missing entries ({_missingEntries.Count}):", EditorStyles.miniBoldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(300));
        foreach (var (cat, label, _) in _missingEntries)
            EditorGUILayout.LabelField($"  + [{cat}] {label}", EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        GUI.color = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button($"Copy {_missingEntries.Count} Missing Entries into Target"))
            CopyMissing();
        GUI.color = Color.white;
    }

    private void CopyMissing()
    {
        var so = new SerializedObject(_target);
        so.Update();

        // Actual structure (confirmed via debug):
        // m_Labels (array) → m_Name (category name), m_CategoryList (array) → m_Name (label), m_Sprite
        var categoriesArray = so.FindProperty("m_Labels");
        if (categoriesArray == null)
        {
            Debug.LogError("[SpriteLibraryMerger] Could not find m_Labels on target.");
            return;
        }

        int copied = 0;
        foreach (var (cat, label, sprite) in _missingEntries)
        {
            if (sprite == null) continue;

            // Find or create the category in m_Labels
            int catIndex = -1;
            for (int i = 0; i < categoriesArray.arraySize; i++)
            {
                var entry = categoriesArray.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("m_Name").stringValue == cat)
                {
                    catIndex = i;
                    break;
                }
            }

            if (catIndex == -1)
            {
                categoriesArray.arraySize++;
                catIndex = categoriesArray.arraySize - 1;
                var newCat = categoriesArray.GetArrayElementAtIndex(catIndex);
                newCat.FindPropertyRelative("m_Name").stringValue = cat;
                newCat.FindPropertyRelative("m_CategoryList").arraySize = 0;
            }

            var categoryProp = categoriesArray.GetArrayElementAtIndex(catIndex);
            var labelsArray  = categoryProp.FindPropertyRelative("m_CategoryList");

            labelsArray.arraySize++;
            var newLabel = labelsArray.GetArrayElementAtIndex(labelsArray.arraySize - 1);
            newLabel.FindPropertyRelative("m_Name").stringValue = label;
            newLabel.FindPropertyRelative("m_Sprite").objectReferenceValue = sprite;
            copied++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _scanned = false;
        _missingEntries.Clear();
        Debug.Log($"[SpriteLibraryMerger] Copied {copied} entries into {_target.name}.");
        EditorUtility.DisplayDialog("Done", $"Copied {copied} entries into {_target.name}.", "OK");
    }

    [MenuItem("Window/Debug Sprite Library Properties")]
    public static void DebugProperties()
    {
        var selected = Selection.activeObject as SpriteLibraryAsset;
        if (selected == null) { Debug.LogError("Select a SpriteLibraryAsset first."); return; }

        var so = new SerializedObject(selected);
        var prop = so.GetIterator();
        prop.Next(true);
        while (prop.NextVisible(true))
            Debug.Log($"path={prop.propertyPath} type={prop.propertyType} name={prop.name}");
    }
}