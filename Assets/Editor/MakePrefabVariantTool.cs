using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor Tool: Create a Prefab Variant of another Prefab.
///
/// Given:
///   - Source Prefab  (Prefab A) – the prefab whose component values you want to carry over
///   - Base Prefab    (Prefab B) – the prefab that will become the parent of the new variant
///
/// Result:
///   A new Prefab Variant asset whose base is Prefab B, with Prefab A's component values
///   applied as overrides. Neither source nor base prefab is modified.
///
/// Usage:
///   Tools > Prefab > Make Variant from Another Prefab
/// </summary>
public class MakePrefabVariantTool : EditorWindow
{
    // ── Fields ──────────────────────────────────────────────────────────────
    private GameObject sourcePrefab;   // Prefab A  (values to copy)
    private GameObject basePrefab;     // Prefab B  (new parent)
    private string     variantName  = "";
    private string     outputFolder = "Assets";
    private Vector2    scroll;

    // ── Menu ────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Prefab/Make Variant from Another Prefab")]
    public static void ShowWindow()
    {
        var w = GetWindow<MakePrefabVariantTool>("Make Prefab Variant");
        w.minSize = new Vector2(420, 320);
        w.Show();
    }

    // Auto-fill Source when user selects a prefab in the Project window
    private void OnSelectionChange()
    {
        if (Selection.activeObject is GameObject go &&
            PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
        {
            sourcePrefab = go;
            if (string.IsNullOrEmpty(variantName))
                variantName = go.name + "_Variant";
            outputFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(go));
            Repaint();
        }
    }

    // ── GUI ─────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Make Prefab Variant from Another Prefab", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates a new Prefab Variant whose BASE is Prefab B, " +
            "with component values copied from Prefab A as overrides.\n" +
            "Neither Prefab A nor Prefab B is modified.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // Source (A)
        EditorGUILayout.LabelField("Step 1 – Source Prefab  (Prefab A — values to carry over)", EditorStyles.miniBoldLabel);
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Source Prefab", sourcePrefab, typeof(GameObject), false);
        DrawPrefabValidation(sourcePrefab, allowVariant: true);

        EditorGUILayout.Space(6);

        // Base (B)
        EditorGUILayout.LabelField("Step 2 – Base Prefab  (Prefab B — new parent of the variant)", EditorStyles.miniBoldLabel);
        basePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Base Prefab", basePrefab, typeof(GameObject), false);
        DrawPrefabValidation(basePrefab, allowVariant: false);

        EditorGUILayout.Space(8);

        // Output
        EditorGUILayout.LabelField("Step 3 – Output", EditorStyles.miniBoldLabel);
        variantName  = EditorGUILayout.TextField("Variant Name",   variantName);

        EditorGUILayout.BeginHorizontal();
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        if (GUILayout.Button("Browse…", GUILayout.Width(70)))
        {
            string chosen = EditorUtility.OpenFolderPanel("Select Output Folder", outputFolder, "");
            if (!string.IsNullOrEmpty(chosen) && chosen.StartsWith(Application.dataPath))
                outputFolder = "Assets" + chosen.Substring(Application.dataPath.Length);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Convert button
        bool canConvert = IsValidPrefab(sourcePrefab, true) &&
                          IsValidPrefab(basePrefab,   false) &&
                          sourcePrefab != basePrefab &&
                          !string.IsNullOrWhiteSpace(variantName) &&
                          !string.IsNullOrWhiteSpace(outputFolder);

        GUI.enabled = canConvert;
        if (GUILayout.Button("Create Prefab Variant", GUILayout.Height(36)))
            CreateVariant();
        GUI.enabled = true;

        if (sourcePrefab != null && basePrefab != null && sourcePrefab == basePrefab)
            EditorGUILayout.HelpBox("Source and Base must be different prefabs.", MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    // ── Validation helpers ───────────────────────────────────────────────────
    private static bool IsValidPrefab(GameObject go, bool allowVariant)
    {
        if (go == null) return false;
        var t = PrefabUtility.GetPrefabAssetType(go);
        if (t == PrefabAssetType.NotAPrefab) return false;
        if (!allowVariant && t == PrefabAssetType.Variant) return false;
        return true;
    }

    private static void DrawPrefabValidation(GameObject go, bool allowVariant)
    {
        if (go == null) return;
        var t = PrefabUtility.GetPrefabAssetType(go);
        if (t == PrefabAssetType.NotAPrefab)
            EditorGUILayout.HelpBox("Not a prefab asset.", MessageType.Error);
        else if (!allowVariant && t == PrefabAssetType.Variant)
            EditorGUILayout.HelpBox("Base Prefab cannot itself be a Prefab Variant (use a regular prefab).", MessageType.Warning);
        else
            EditorGUILayout.HelpBox("✓ OK", MessageType.None);
    }

    // ── Core logic ───────────────────────────────────────────────────────────
    private void CreateVariant()
    {
        // Ensure output folder exists
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            bool create = EditorUtility.DisplayDialog("Folder Not Found",
                $"The folder \"{outputFolder}\" does not exist. Create it?", "Create", "Cancel");
            if (!create) return;
            Directory.CreateDirectory(Path.GetFullPath(outputFolder));
            AssetDatabase.Refresh();
        }

        string safeName    = variantName.Trim();
        string variantPath = $"{outputFolder}/{safeName}.prefab";

        if (File.Exists(Path.GetFullPath(variantPath)))
        {
            bool overwrite = EditorUtility.DisplayDialog("File Exists",
                $"\"{variantPath}\" already exists. Overwrite?", "Overwrite", "Cancel");
            if (!overwrite) return;
        }

        // 1. Load both prefab roots
        GameObject sourceRoot = sourcePrefab;
        GameObject baseRoot   = basePrefab;

        // 2. Instantiate the BASE prefab — this becomes our variant instance
        GameObject variantInstance = (GameObject)PrefabUtility.InstantiatePrefab(baseRoot);
        variantInstance.name = safeName;

        try
        {
            // 3. Copy component values from Source → variant instance
            CopyComponentOverrides(sourceRoot, variantInstance);

            // 4. Copy child component values (matched by name + sibling index)
            CopyChildComponentOverrides(sourceRoot.transform, variantInstance.transform);

            // 5. Save as a Prefab Variant (keeps the link to baseRoot)
            GameObject savedVariant = PrefabUtility.SaveAsPrefabAsset(variantInstance, variantPath);

            if (savedVariant != null)
            {
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(savedVariant);
                Selection.activeObject = savedVariant;
                Debug.Log($"[MakePrefabVariantTool] Created variant '{variantPath}' " +
                          $"(base: {AssetDatabase.GetAssetPath(baseRoot)}, " +
                          $"source: {AssetDatabase.GetAssetPath(sourceRoot)})");
                EditorUtility.DisplayDialog("Done",
                    $"Prefab Variant created at:\n{variantPath}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error",
                    "SaveAsPrefabAsset returned null. Check the Console.", "OK");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Exception", ex.Message, "OK");
        }
        finally
        {
            DestroyImmediate(variantInstance);
        }
    }

    // ── Component copying ─────────────────────────────────────────────────────

    /// <summary>
    /// For each component on sourceGO, find the matching component on targetGO
    /// (by type + index) and copy serialized field values.
    /// Components that don't exist on the target are added.
    /// </summary>
    private static void CopyComponentOverrides(GameObject sourceGO, GameObject targetGO)
    {
        var sourceComponents = sourceGO.GetComponents<Component>();
        var targetComponents = targetGO.GetComponents<Component>();

        // Track which target components have already been matched
        var matched = new HashSet<int>();

        foreach (Component srcComp in sourceComponents)
        {
            if (srcComp == null) continue;
            System.Type type = srcComp.GetType();

            // Skip Transform — position/rotation/scale are handled separately if desired
            if (type == typeof(Transform)) continue;

            // Find first unmatched component of the same type on target
            Component dstComp = null;
            for (int i = 0; i < targetComponents.Length; i++)
            {
                if (!matched.Contains(i) &&
                    targetComponents[i] != null &&
                    targetComponents[i].GetType() == type)
                {
                    dstComp = targetComponents[i];
                    matched.Add(i);
                    break;
                }
            }

            // If target doesn't have this component, add it
            if (dstComp == null)
                dstComp = targetGO.AddComponent(type);

            CopySerializedProperties(srcComp, dstComp);
        }
    }

    /// <summary>
    /// Recursively walk the source child hierarchy and copy component values
    /// to matching children on the target (matched by sibling index, then name).
    /// </summary>
    private static void CopyChildComponentOverrides(Transform srcParent, Transform dstParent)
    {
        int count = Mathf.Min(srcParent.childCount, dstParent.childCount);
        for (int i = 0; i < count; i++)
        {
            Transform srcChild = srcParent.GetChild(i);
            Transform dstChild = dstParent.GetChild(i);

            // Also try to match by name if indices diverge in complex hierarchies
            if (srcChild.name != dstChild.name)
            {
                Transform named = FindChildByName(dstParent, srcChild.name);
                if (named != null) dstChild = named;
            }

            CopyComponentOverrides(srcChild.gameObject, dstChild.gameObject);
            CopyChildComponentOverrides(srcChild, dstChild);
        }
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    /// <summary>
    /// Copies all serialized property values from src to dst using SerializedObject,
    /// which respects Unity's serialization system and works correctly with prefab overrides.
    /// </summary>
    private static void CopySerializedProperties(Component src, Component dst)
    {
        var srcSO = new SerializedObject(src);
        var dstSO = new SerializedObject(dst);

        SerializedProperty prop = srcSO.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Skip Unity internal bookkeeping properties
            if (prop.propertyPath == "m_Script") continue;

            SerializedProperty dstProp = dstSO.FindProperty(prop.propertyPath);
            if (dstProp != null && !dstProp.isArray || (dstProp != null && prop.isArray))
            {
                try { dstSO.CopyFromSerializedProperty(prop); }
                catch { /* Ignore properties that can't be copied (e.g. read-only) */ }
            }
        }

        dstSO.ApplyModifiedPropertiesWithoutUndo();
    }
}
