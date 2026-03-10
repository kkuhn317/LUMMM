using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioClipReplacerTool : EditorWindow
{
    private AudioClip oldClip;
    private AudioClip newClip;
    private bool includeInactive = true;

    private Vector2 scroll;

    private int foundComponents;
    private int foundReferences;
    private bool hasScanned;

    private string objectFilter = string.Empty;
    private string componentFilter = string.Empty;
    private string propertyFilter = string.Empty;
    private bool showOnlySelected;

    private readonly List<MatchInfo> matches = new();

    private class MatchInfo
    {
        public Component component;
        public GameObject gameObject;
        public string propertyPath;
        public int referencesInComponent;
        public bool selected = true;
    }

    [MenuItem("Tools/Audio/Audio Clip Replacer")]
    public static void ShowWindow()
    {
        GetWindow<AudioClipReplacerTool>("Audio Clip Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Audio Clip Replacer", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Scan the current scene, review matches, filter what you want, and replace only the selected AudioClip references.",
            EditorStyles.wordWrappedMiniLabel);

        GUILayout.Space(10);

        EditorGUI.BeginChangeCheck();

        oldClip = (AudioClip)EditorGUILayout.ObjectField("Old Audio", oldClip, typeof(AudioClip), false);
        newClip = (AudioClip)EditorGUILayout.ObjectField("New Audio", newClip, typeof(AudioClip), false);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        if (EditorGUI.EndChangeCheck())
        {
            ClearScanResults();
        }

        GUILayout.Space(10);

        DrawStatusBox();

        GUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = oldClip != null;
            if (GUILayout.Button("Scan", GUILayout.Height(28)))
            {
                ScanScene();
            }

            GUI.enabled = CanReplace();
            if (GUILayout.Button($"Replace Selected ({GetSelectedReferenceCount()})", GUILayout.Height(28)))
            {
                ReplaceReferences();
            }

            GUI.enabled = true;
        }

        GUILayout.Space(12);

        DrawResults();
    }

    private void DrawStatusBox()
    {
        if (oldClip == null && newClip == null)
        {
            EditorGUILayout.HelpBox("Assign an old AudioClip and a new AudioClip to begin.", MessageType.Info);
            return;
        }

        if (oldClip == null)
        {
            EditorGUILayout.HelpBox("Select the AudioClip you want to replace.", MessageType.Warning);
            return;
        }

        if (newClip == null)
        {
            EditorGUILayout.HelpBox("Select the replacement AudioClip.", MessageType.Info);
        }

        if (oldClip != null && newClip != null && oldClip == newClip)
        {
            EditorGUILayout.HelpBox("Old Audio and New Audio cannot be the same clip.", MessageType.Error);
            return;
        }

        if (hasScanned)
        {
            if (foundReferences > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Scan complete. Found {foundComponents} component(s) with {foundReferences} total reference(s). " +
                    $"Currently selected: {GetSelectedComponentCount()} component(s), {GetSelectedReferenceCount()} reference(s).",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox("Scan complete. No matching references were found in the current scene.", MessageType.Warning);
            }
        }
        else if (oldClip != null)
        {
            EditorGUILayout.HelpBox("Click Scan to preview where this AudioClip is used in the current scene.", MessageType.None);
        }
    }

    private void DrawResults()
    {
        EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

        if (!hasScanned)
        {
            EditorGUILayout.HelpBox("No scan results yet.", MessageType.None);
            return;
        }

        if (matches.Count == 0)
        {
            EditorGUILayout.HelpBox("No matching components found.", MessageType.None);
            return;
        }

        DrawFilterToolbar();

        GUILayout.Space(6);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(260));

        int visibleCount = 0;

        foreach (var match in matches)
        {
            if (!PassesFilters(match))
                continue;

            visibleCount++;

            EditorGUILayout.BeginVertical("box");

            using (new EditorGUILayout.HorizontalScope())
            {
                match.selected = EditorGUILayout.Toggle(match.selected, GUILayout.Width(18));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(match.gameObject, typeof(GameObject), true);
                }

                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(match.gameObject);
                    Selection.activeObject = match.gameObject;
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Component", match.component, typeof(Component), true);
            }

            EditorGUILayout.LabelField("Property", match.propertyPath);
            EditorGUILayout.LabelField("References In Component", match.referencesInComponent.ToString());

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        if (visibleCount == 0)
        {
            EditorGUILayout.HelpBox("No results match the current filters.", MessageType.None);
        }
    }

    private void DrawFilterToolbar()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Review & Filter", EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select All Visible"))
            {
                SetSelectionForVisible(true);
            }

            if (GUILayout.Button("Deselect All Visible"))
            {
                SetSelectionForVisible(false);
            }

            if (GUILayout.Button("Clear Filters"))
            {
                objectFilter = string.Empty;
                componentFilter = string.Empty;
                propertyFilter = string.Empty;
                showOnlySelected = false;
                GUI.FocusControl(null);
            }
        }

        GUILayout.Space(4);

        objectFilter = EditorGUILayout.TextField("GameObject Filter", objectFilter);
        componentFilter = EditorGUILayout.TextField("Component Filter", componentFilter);
        propertyFilter = EditorGUILayout.TextField("Property Filter", propertyFilter);
        showOnlySelected = EditorGUILayout.Toggle("Show Only Selected", showOnlySelected);

        EditorGUILayout.LabelField(
            $"Visible: {GetVisibleMatchCount()}  |  Selected: {GetSelectedVisibleMatchCount()}  |  Selected References: {GetSelectedReferenceCount()}",
            EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void ScanScene()
    {
        ClearScanResults();

        if (oldClip == null)
        {
            EditorUtility.DisplayDialog("Missing Old Audio", "Please assign the AudioClip you want to replace.", "OK");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("No Scene Loaded", "There is no loaded active scene.", "OK");
            return;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            Component[] components = root.GetComponentsInChildren<Component>(includeInactive);

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                SerializedObject serializedObject = new SerializedObject(component);
                SerializedProperty iterator = serializedObject.GetIterator();

                int refsInThisComponent = 0;
                string firstPropertyPath = null;

                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                        iterator.objectReferenceValue == oldClip)
                    {
                        refsInThisComponent++;

                        if (string.IsNullOrEmpty(firstPropertyPath))
                            firstPropertyPath = iterator.propertyPath;
                    }
                }

                if (refsInThisComponent > 0)
                {
                    foundComponents++;
                    foundReferences += refsInThisComponent;

                    matches.Add(new MatchInfo
                    {
                        component = component,
                        gameObject = component.gameObject,
                        propertyPath = firstPropertyPath,
                        referencesInComponent = refsInThisComponent,
                        selected = true
                    });
                }
            }
        }

        hasScanned = true;
        Repaint();
    }

    private void ReplaceReferences()
    {
        if (!hasScanned)
        {
            EditorUtility.DisplayDialog("Scan Required", "Please scan the scene before replacing references.", "OK");
            return;
        }

        if (oldClip == null || newClip == null)
        {
            EditorUtility.DisplayDialog("Missing AudioClip", "Assign both Old Audio and New Audio.", "OK");
            return;
        }

        if (oldClip == newClip)
        {
            EditorUtility.DisplayDialog("Invalid Selection", "Old Audio and New Audio cannot be the same.", "OK");
            return;
        }

        int selectedComponents = GetSelectedComponentCount();
        int selectedReferences = GetSelectedReferenceCount();

        if (selectedReferences == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "There are no selected references to replace.", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Confirm Replacement",
            $"You are about to replace only the selected matches.\n\n" +
            $"{selectedReferences} reference(s)\n" +
            $"across {selectedComponents} component(s)\n\n" +
            $"From: {oldClip.name}\n" +
            $"To: {newClip.name}\n\n" +
            $"Proceed?",
            "Replace Selected",
            "Cancel"
        );

        if (!confirm)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        int replacedCount = 0;
        int changedComponents = 0;

        foreach (var match in matches)
        {
            if (!match.selected || match.component == null)
                continue;

            SerializedObject serializedObject = new SerializedObject(match.component);
            SerializedProperty iterator = serializedObject.GetIterator();

            bool changed = false;

            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                    iterator.objectReferenceValue == oldClip)
                {
                    iterator.objectReferenceValue = newClip;
                    replacedCount++;
                    changed = true;
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(match.component);
                changedComponents++;
            }
        }

        if (changedComponents > 0)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        EditorUtility.DisplayDialog(
            "Replacement Complete",
            $"Done.\n\nReplaced {replacedCount} reference(s) across {changedComponents} component(s).",
            "OK"
        );

        ScanScene();
    }

    private bool CanReplace()
    {
        return hasScanned &&
               oldClip != null &&
               newClip != null &&
               oldClip != newClip &&
               GetSelectedReferenceCount() > 0;
    }

    private bool PassesFilters(MatchInfo match)
    {
        if (match == null)
            return false;

        if (showOnlySelected && !match.selected)
            return false;

        string gameObjectName = match.gameObject != null ? match.gameObject.name : string.Empty;
        string componentName = match.component != null ? match.component.GetType().Name : string.Empty;
        string propertyName = match.propertyPath ?? string.Empty;

        if (!ContainsIgnoreCase(gameObjectName, objectFilter))
            return false;

        if (!ContainsIgnoreCase(componentName, componentFilter))
            return false;

        if (!ContainsIgnoreCase(propertyName, propertyFilter))
            return false;

        return true;
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (string.IsNullOrEmpty(source))
            return false;

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SetSelectionForVisible(bool selected)
    {
        foreach (var match in matches)
        {
            if (PassesFilters(match))
            {
                match.selected = selected;
            }
        }

        Repaint();
    }

    private int GetVisibleMatchCount()
    {
        int count = 0;

        foreach (var match in matches)
        {
            if (PassesFilters(match))
                count++;
        }

        return count;
    }

    private int GetSelectedVisibleMatchCount()
    {
        int count = 0;

        foreach (var match in matches)
        {
            if (PassesFilters(match) && match.selected)
                count++;
        }

        return count;
    }

    private int GetSelectedComponentCount()
    {
        int count = 0;

        foreach (var match in matches)
        {
            if (match.selected)
                count++;
        }

        return count;
    }

    private int GetSelectedReferenceCount()
    {
        int count = 0;

        foreach (var match in matches)
        {
            if (match.selected)
                count += match.referencesInComponent;
        }

        return count;
    }

    private void ClearScanResults()
    {
        foundComponents = 0;
        foundReferences = 0;
        hasScanned = false;
        matches.Clear();

        objectFilter = string.Empty;
        componentFilter = string.Empty;
        propertyFilter = string.Empty;
        showOnlySelected = false;
    }
}