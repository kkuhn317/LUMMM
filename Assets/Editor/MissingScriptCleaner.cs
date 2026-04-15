using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;

public class MissingScriptCleaner : EditorWindow
{
    private List<GameObject> objectsToClean = new List<GameObject>();
    private Vector2 scrollPos;
    private Vector2 valScrollPos;
    private string searchText = "";
    private StringBuilder sessionLog = new StringBuilder();

    [MenuItem("Tools/Missing Script Cleaner")]
    public static void ShowWindow() => GetWindow<MissingScriptCleaner>("Script Cleaner");

    private void OnGUI()
    {
        // --- HEADER & SEARCH ---
        GUILayout.Label("Missing Script Cleaner + Logger", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(40))) searchText = "";
        EditorGUILayout.EndHorizontal();

        // SECTION 1: OBJECT LIST (THE SPACE FILLER)
        // ExpandHeight(true) forces this to push everything else down
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        List<GameObject> filteredList = GetFilteredList();
        
        for (int i = 0; i < filteredList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(filteredList[i], typeof(GameObject), true);
            if (GUILayout.Button("X", GUILayout.Width(20))) { objectsToClean.Remove(filteredList[i]); break; }
            EditorGUILayout.EndHorizontal();
        }
        if (filteredList.Count == 0) GUILayout.Label("No objects found...", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // SECTION 2: VALIDATION (FIXED HEIGHT)
        GUILayout.Label("Validation Status:", EditorStyles.miniBoldLabel);
        valScrollPos = EditorGUILayout.BeginScrollView(valScrollPos, GUILayout.Height(100));
        EditorGUILayout.BeginVertical(EditorStyles.textArea);
        int dirtyCount = 0;
        foreach (var obj in filteredList)
        {
            if (obj != null && HasMissingScripts(obj))
            {
                GUI.color = new Color(1f, 0.7f, 0.3f);
                if (GUILayout.Button($"[!] {obj.name} (Click to Ping)", EditorStyles.label)) EditorGUIUtility.PingObject(obj);
                dirtyCount++;
            }
        }
        if (dirtyCount == 0 && filteredList.Count > 0)
        {
            GUI.color = Color.green;
            GUILayout.Label("✓ Filtered selection is clean!");
        }
        GUI.color = Color.white;
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // SECTION 3: FOOTER CONTROLS
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add All Scene")) AddAllSceneObjects();
        
        GUI.enabled = !string.IsNullOrEmpty(searchText) && filteredList.Count > 0;
        if (GUILayout.Button("Keep Filtered Only")) objectsToClean = new List<GameObject>(filteredList);
        GUI.enabled = true;

        if (GUILayout.Button("Clear All")) objectsToClean.Clear();
        EditorGUILayout.EndHorizontal();

        // Action Buttons
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("CLEAN PROJECT PREFABS", GUILayout.Height(25))) CleanAllPrefabsInProject();

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        string btnText = string.IsNullOrEmpty(searchText) ? "CLEAN ALL SELECTED" : $"CLEAN {filteredList.Count} FILTERED";
        if (GUILayout.Button(btnText, GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Proceed with cleaning and generate report?", "Yes", "Cancel"))
                CleanSpecificList(filteredList);
        }
        
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Open Last Report")) OpenLogFile();

        // THE STATUS BAR
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label($"Total: {objectsToClean.Count}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (dirtyCount > 0) GUI.color = Color.yellow;
        GUILayout.Label($"Issues in Filter: {dirtyCount}", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private List<GameObject> GetFilteredList() => objectsToClean.FindAll(go => go != null && go.name.ToLower().Contains(searchText.ToLower()));

    private bool HasMissingScripts(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) return true;
        return false;
    }

    private void CleanSpecificList(List<GameObject> list)
    {
        sessionLog.Clear();
        sessionLog.AppendLine($"--- Manual Cleanup: {System.DateTime.Now} ---");
        int total = 0;
        foreach (GameObject obj in list)
        {
            if (obj == null) continue;
            foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            {
                Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove Missing Scripts");
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removed > 0)
                {
                    sessionLog.AppendLine($"- Removed {removed} from: {GetGameObjectPath(t.gameObject)}");
                    total += removed;
                }
            }
        }
        SaveReport();
        EditorUtility.DisplayDialog("Done", $"Removed {total} scripts. Report saved.", "OK");
    }

    private void CleanAllPrefabsInProject()
    {
        sessionLog.Clear();
        sessionLog.AppendLine($"--- Project Prefab Deep Clean: {System.DateTime.Now} ---");
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                int r = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                    r += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (r > 0) { sessionLog.AppendLine($"- Cleaned Prefab: {path} ({r})"); EditorUtility.SetDirty(prefab); count += r; }
            }
        }
        AssetDatabase.SaveAssets();
        SaveReport();
        EditorUtility.DisplayDialog("Project Cleaned", $"Removed {count} scripts. Log saved.", "OK");
    }

    private void SaveReport()
    {
        string dir = Application.dataPath + "/Editor/Logs";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(dir + "/CleanupReport.txt", sessionLog.ToString());
        AssetDatabase.Refresh();
    }

    private void OpenLogFile()
    {
        string path = Application.dataPath + "/Editor/Logs/CleanupReport.txt";
        if (File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null) { obj = obj.transform.parent.gameObject; path = "/" + obj.name + path; }
        return path;
    }

    private void AddAllSceneObjects()
    {
        objectsToClean.Clear();
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects()) objectsToClean.Add(go);
    }
}