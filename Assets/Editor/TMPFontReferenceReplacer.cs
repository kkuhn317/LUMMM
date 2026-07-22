#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TMPFontReferenceReplacer : EditorWindow
{
    private const string UnderlaySuffix = "_underlay";

    [SerializeField] private TMP_FontAsset oldFont;
    [SerializeField] private TMP_FontAsset newFont;
    [SerializeField] private Material oldNormalMaterial;
    [SerializeField] private Material newNormalMaterial;
    [SerializeField] private Material oldUnderlayMaterial;
    [SerializeField] private Material newUnderlayMaterial;
    [SerializeField] private bool processPrefabs = true;
    [SerializeField] private bool processScenes = true;

    private int replacedReferenceCount;
    private int changedFileCount;

    [MenuItem("Tools/TextMeshPro/Replace Font References")]
    private static void OpenWindow()
    {
        GetWindow<TMPFontReferenceReplacer>("Replace TMP Font");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Font replacement", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Replaces the old TMP font and its normal / _underlay material references " +
            "throughout every prefab and scene under Assets. Serialized localization " +
            "variants stored in GameObjectLocalizer components are included.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();

        oldFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "Old Font Asset", oldFont, typeof(TMP_FontAsset), false);

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "New Font Asset", newFont, typeof(TMP_FontAsset), false);

        if (EditorGUI.EndChangeCheck())
            AutoDetectMaterials();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Material mapping", EditorStyles.boldLabel);

        oldNormalMaterial = (Material)EditorGUILayout.ObjectField(
            "Old Normal", oldNormalMaterial, typeof(Material), false);

        newNormalMaterial = (Material)EditorGUILayout.ObjectField(
            "New Normal", newNormalMaterial, typeof(Material), false);

        oldUnderlayMaterial = (Material)EditorGUILayout.ObjectField(
            "Old _underlay", oldUnderlayMaterial, typeof(Material), false);

        newUnderlayMaterial = (Material)EditorGUILayout.ObjectField(
            "New _underlay", newUnderlayMaterial, typeof(Material), false);

        if (GUILayout.Button("Auto-detect materials"))
            AutoDetectMaterials();

        EditorGUILayout.Space();
        processPrefabs = EditorGUILayout.ToggleLeft("Process all prefabs", processPrefabs);
        processScenes = EditorGUILayout.ToggleLeft("Process all scenes", processScenes);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!CanRun()))
        {
            if (GUILayout.Button("Replace all references", GUILayout.Height(34)))
                ReplaceAllReferences();
        }

        if (replacedReferenceCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"Last run: {replacedReferenceCount} references replaced in " +
                $"{changedFileCount} files.",
                MessageType.None);
        }
    }

    private bool CanRun()
    {
        return oldFont != null
            && newFont != null
            && oldFont != newFont
            && oldNormalMaterial != null
            && newNormalMaterial != null
            && oldUnderlayMaterial != null
            && newUnderlayMaterial != null
            && (processPrefabs || processScenes);
    }

    private void AutoDetectMaterials()
    {
        oldNormalMaterial = oldFont != null ? oldFont.material : null;
        newNormalMaterial = newFont != null ? newFont.material : null;
        oldUnderlayMaterial = FindUnderlayMaterial(oldFont);
        newUnderlayMaterial = FindUnderlayMaterial(newFont);
    }

    private static Material FindUnderlayMaterial(TMP_FontAsset font)
    {
        if (font == null || font.atlasTexture == null)
            return null;

        Material firstCompatible = null;
        string preferredNameA = font.name + UnderlaySuffix;
        string preferredNameB = font.name + " Material" + UnderlaySuffix;

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null
                || !material.name.EndsWith(UnderlaySuffix, StringComparison.OrdinalIgnoreCase)
                || material.mainTexture != font.atlasTexture)
            {
                continue;
            }

            if (string.Equals(material.name, preferredNameA, StringComparison.OrdinalIgnoreCase)
                || string.Equals(material.name, preferredNameB, StringComparison.OrdinalIgnoreCase))
            {
                return material;
            }

            if (firstCompatible == null)
                firstCompatible = material;
        }

        return firstCompatible;
    }

    private void ReplaceAllReferences()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Cannot run",
                "Exit Play Mode before replacing project references.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Replace TMP references?",
                "This will modify every selected prefab and scene under Assets.\n\n" +
                "Commit or back up the project first.",
                "Replace",
                "Cancel"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        replacedReferenceCount = 0;
        changedFileCount = 0;

        SceneSetup[] originalSceneSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            if (processPrefabs)
                ProcessAllPrefabs();

            if (processScenes)
                ProcessAllScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"TMP font replacement complete: {replacedReferenceCount} references " +
                $"replaced in {changedFileCount} files.");

            EditorUtility.DisplayDialog(
                "Replacement complete",
                $"{replacedReferenceCount} references were replaced in " +
                $"{changedFileCount} files.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "Replacement stopped",
                "An error occurred. See the Console for details.",
                "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            EditorSceneManager.RestoreSceneManagerSetup(originalSceneSetup);
        }
    }

    private void ProcessAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        var prefabs = new List<PrefabRecord>();

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (asset == null)
                continue;

            PrefabAssetType type = PrefabUtility.GetPrefabAssetType(asset);

            if (type == PrefabAssetType.Regular || type == PrefabAssetType.Variant)
                prefabs.Add(new PrefabRecord(path, type));
        }

        prefabs.Sort((a, b) => PrefabSortOrder(a.Type).CompareTo(PrefabSortOrder(b.Type)));

        for (int index = 0; index < prefabs.Count; index++)
        {
            PrefabRecord record = prefabs[index];

            EditorUtility.DisplayProgressBar(
                "Replacing TMP references in prefabs",
                record.Path,
                prefabs.Count == 0 ? 1f : (float)index / prefabs.Count);

            GameObject root = PrefabUtility.LoadPrefabContents(record.Path);

            try
            {
                int replacements = ReplaceInHierarchy(
                    root,
                    skipNestedPrefabRoots: true,
                    currentObjectIsRoot: true);

                if (replacements <= 0)
                    continue;

                PrefabUtility.SaveAsPrefabAsset(root, record.Path);
                replacedReferenceCount += replacements;
                changedFileCount++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private void ProcessAllScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

        for (int index = 0; index < sceneGuids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(sceneGuids[index]);

            EditorUtility.DisplayProgressBar(
                "Replacing TMP references in scenes",
                path,
                sceneGuids.Length == 0 ? 1f : (float)index / sceneGuids.Length);

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int sceneReplacements = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                sceneReplacements += ReplaceInHierarchy(
                    root,
                    skipNestedPrefabRoots: false,
                    currentObjectIsRoot: true);
            }

            if (sceneReplacements <= 0)
                continue;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            replacedReferenceCount += sceneReplacements;
            changedFileCount++;
        }
    }

    private int ReplaceInHierarchy(
        GameObject gameObject,
        bool skipNestedPrefabRoots,
        bool currentObjectIsRoot)
    {
        if (!currentObjectIsRoot
            && skipNestedPrefabRoots
            && PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
        {
            return 0;
        }

        int replacements = 0;

        foreach (Component component in gameObject.GetComponents<Component>())
        {
            if (component != null)
                replacements += ReplaceSerializedReferences(component);
        }

        Transform transform = gameObject.transform;

        for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
        {
            replacements += ReplaceInHierarchy(
                transform.GetChild(childIndex).gameObject,
                skipNestedPrefabRoots,
                false);
        }

        return replacements;
    }

    private int ReplaceSerializedReferences(UnityEngine.Object target)
    {
        int replacements = 0;
        var serializedObject = new SerializedObject(target);
        serializedObject.UpdateIfRequiredOrScript();

        SerializedProperty property = serializedObject.GetIterator();

        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            UnityEngine.Object currentValue = property.objectReferenceValue;
            UnityEngine.Object replacement = GetReplacement(currentValue);

            if (replacement == null || replacement == currentValue)
                continue;

            property.objectReferenceValue = replacement;
            replacements++;
        }

        if (replacements > 0)
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

        return replacements;
    }

    private UnityEngine.Object GetReplacement(UnityEngine.Object currentValue)
    {
        if (currentValue == oldFont)
            return newFont;

        if (currentValue == oldNormalMaterial)
            return newNormalMaterial;

        if (currentValue == oldUnderlayMaterial)
            return newUnderlayMaterial;

        return null;
    }

    private static int PrefabSortOrder(PrefabAssetType type)
    {
        return type == PrefabAssetType.Regular ? 0 : 1;
    }

    private struct PrefabRecord
    {
        public PrefabRecord(string path, PrefabAssetType type)
        {
            Path = path;
            Type = type;
        }

        public string Path;
        public PrefabAssetType Type;
    }
}

#endif
