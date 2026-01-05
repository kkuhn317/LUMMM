using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AudioMixerMigrationTool : EditorWindow
{
    [System.Serializable]
    public class MixerGroupMapping
    {
        public AudioMixerGroup oldMixerGroup;
        public AudioMixerGroup newMixerGroup;
        public string description;
    }

    private Vector2 windowScroll;
    private AudioMixer masterMixer;
    private List<MixerGroupMapping> mappings = new List<MixerGroupMapping>();
    private Vector2 scrollPos;
    private bool showUnmappedOnly = false;
    private int totalAudioSources = 0;
    private int modifiedAudioSources = 0;
    
    [MenuItem("Tools/Audio/Audio Mixer Migration Tool")]
    public static void ShowWindow()
    {
        GetWindow<AudioMixerMigrationTool>("Audio Mixer Migration");
    }

    private void OnEnable()
    {
        LoadMappings();
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Audio Mixer Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Map OLD Audio Mixer Groups to NEW Audio Mixer Groups.", MessageType.Info);

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Audio Mixer Group Mappings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Map OLD mixer groups to NEW mixer groups.", MessageType.Info);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

        int removeIndex = -1;

        for (int i = 0; i < mappings.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Mapping {i + 1}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            mappings[i].oldMixerGroup =
                (AudioMixerGroup)EditorGUILayout.ObjectField("Old Mixer Group", mappings[i].oldMixerGroup, typeof(AudioMixerGroup), false);

            mappings[i].newMixerGroup =
                (AudioMixerGroup)EditorGUILayout.ObjectField("New Mixer Group", mappings[i].newMixerGroup, typeof(AudioMixerGroup), false);

            mappings[i].description =
                EditorGUILayout.TextField("Description", mappings[i].description);

            if (EditorGUI.EndChangeCheck())
                SaveMappings();

            if (GUILayout.Button("Remove"))
                removeIndex = i;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        // Apply removal AFTER the loop to keep GUILayout state valid
        if (removeIndex >= 0)
        {
            mappings.RemoveAt(removeIndex);
            SaveMappings();
            GUI.FocusControl(null);
            Repaint();
        }

        if (GUILayout.Button("Add New Mapping"))
        {
            mappings.Add(new MixerGroupMapping());
            SaveMappings();
            GUI.FocusControl(null);
            Repaint();
        }

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Find All Used Mixer Groups in Project"))
            FindAllUsedMixerGroups();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Scan", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Project for AudioSources", GUILayout.Height(30)))
            ScanAudioSources();

        if (GUILayout.Button("Scan Current Scene", GUILayout.Height(30)))
            ScanCurrentScene();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        showUnmappedOnly = EditorGUILayout.Toggle("Show Unmapped AudioSources Only", showUnmappedOnly);
        EditorGUILayout.LabelField($"Found {totalAudioSources} AudioSources ({modifiedAudioSources} will be modified)");

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Migration Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Preview Changes (Dry Run)", GUILayout.Height(30)))
            PreviewChanges();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Apply Migrations to Project", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Confirm Migration",
                "This will modify AudioSources across the entire project. Make sure you have a backup!\n\nContinue?",
                "Yes, Migrate", "Cancel"))
            {
                ApplyMigrations();
            }
        }

        if (GUILayout.Button("Apply to Current Scene Only", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm Migration",
                "This will modify AudioSources in the current scene only.\n\nContinue?",
                "Yes, Migrate", "Cancel"))
            {
                ApplyToCurrentScene();
            }
        }

        if (mappings.Count == 0)
            EditorGUILayout.HelpBox("No mappings defined. Add mappings above.", MessageType.Warning);

        EditorGUILayout.Space(10);

        EditorGUILayout.EndScrollView();
    }
    
    private void FindAllUsedMixerGroups()
    {
        HashSet<AudioMixerGroup> usedGroups = new HashSet<AudioMixerGroup>();
        
        string[] guids = AssetDatabase.FindAssets("t:GameObject");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (obj != null && PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
            {
                AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>(true);
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource.outputAudioMixerGroup != null)
                    {
                        usedGroups.Add(audioSource.outputAudioMixerGroup);
                    }
                }
            }
        }
        
        Debug.Log($"=== USED MIXER GROUPS IN PROJECT ({usedGroups.Count}) ===");
        foreach (var group in usedGroups.OrderBy(g => g.name))
        {
            Debug.Log($"• {group.name}", group);
        }
    }
    
    private void ScanAudioSources()
    {
        totalAudioSources = 0;
        modifiedAudioSources = 0;
        
        string[] guids = AssetDatabase.FindAssets("t:GameObject");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (obj != null && PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
            {
                ScanGameObject(obj, path);
            }
        }
        
        EditorUtility.DisplayDialog("Scan Complete", 
            $"Found {totalAudioSources} AudioSources total.\n{modifiedAudioSources} will be modified based on current mappings.", 
            "OK");
    }
    
    private void ScanCurrentScene()
    {
        totalAudioSources = 0;
        modifiedAudioSources = 0;
        
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            ScanGameObject(obj, "Current Scene: " + obj.name);
        }
        
        EditorUtility.DisplayDialog("Scan Complete", 
            $"Found {totalAudioSources} AudioSources in current scene.\n{modifiedAudioSources} will be modified based on current mappings.", 
            "OK");
    }
    
    private void ScanGameObject(GameObject obj, string source)
    {
        AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>(true);
        
        foreach (AudioSource audioSource in audioSources)
        {
            totalAudioSources++;
            
            if (audioSource.outputAudioMixerGroup != null)
            {
                bool isMapped = false;
                
                foreach (var mapping in mappings)
                {
                    if (mapping.oldMixerGroup == audioSource.outputAudioMixerGroup)
                    {
                        modifiedAudioSources++;
                        isMapped = true;
                        
                        if (!showUnmappedOnly)
                        {
                            Debug.Log($"Will modify: {source} > {GetFullPath(audioSource.gameObject)}\n" +
                                     $"  Old: {audioSource.outputAudioMixerGroup.name}\n" +
                                     $"  New: {mapping.newMixerGroup?.name ?? "None"}", audioSource);
                        }
                        break;
                    }
                }
                
                if (!isMapped && !showUnmappedOnly)
                {
                    Debug.Log($"Unmapped: {source} > {GetFullPath(audioSource.gameObject)}\n" +
                             $"  Group: {audioSource.outputAudioMixerGroup.name}", audioSource);
                }
            }
            else if (!showUnmappedOnly)
            {
                Debug.Log($"No mixer group: {source} > {GetFullPath(audioSource.gameObject)}", audioSource);
            }
        }
    }
    
    private void PreviewChanges()
    {
        Debug.Log("=== PREVIEW CHANGES ===");
        
        int changedCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:GameObject");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (obj != null && PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
            {
                changedCount += PreviewGameObject(obj, path);
            }
        }
        
        Debug.Log($"=== PREVIEW COMPLETE: {changedCount} AudioSources would be modified ===");
        EditorUtility.DisplayDialog("Preview Complete", 
            $"{changedCount} AudioSources would be modified.\nCheck Console for details.", 
            "OK");
    }
    
    private int PreviewGameObject(GameObject obj, string source)
    {
        int changedCount = 0;
        AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>(true);
        bool hasChanges = false;
        string changesLog = "";
        
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource.outputAudioMixerGroup != null)
            {
                foreach (var mapping in mappings)
                {
                    if (mapping.oldMixerGroup == audioSource.outputAudioMixerGroup && mapping.newMixerGroup != null)
                    {
                        changedCount++;
                        hasChanges = true;
                        changesLog += $"  • {audioSource.name}: {audioSource.outputAudioMixerGroup.name} → {mapping.newMixerGroup.name}\n";
                        break;
                    }
                }
            }
        }
        
        if (hasChanges)
        {
            Debug.Log($"Prefab: {source}\n{changesLog}");
        }
        
        return changedCount;
    }
    
    private void ApplyMigrations()
    {
        int totalChanged = 0;
        List<string> modifiedAssets = new List<string>();
        
        string[] guids = AssetDatabase.FindAssets("t:GameObject");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (obj != null && PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
            {
                int changed = MigrateGameObject(obj);
                if (changed > 0)
                {
                    totalChanged += changed;
                    modifiedAssets.Add(path);
                    
                    EditorUtility.SetDirty(obj);
                    AssetDatabase.SaveAssetIfDirty(obj);
                }
            }
        }
        
        Debug.Log($"=== MIGRATION COMPLETE: {totalChanged} AudioSources modified in {modifiedAssets.Count} assets ===");
        
        string summary = $"Modified {totalChanged} AudioSources in {modifiedAssets.Count} assets:\n\n";
        foreach (var asset in modifiedAssets)
        {
            summary += $"• {asset}\n";
        }
        
        EditorUtility.DisplayDialog("Migration Complete", summary, "OK");
        AssetDatabase.Refresh();
    }
    
    private void ApplyToCurrentScene()
    {
        int totalChanged = 0;
        
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>(true);
            
            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource.outputAudioMixerGroup != null)
                {
                    foreach (var mapping in mappings)
                    {
                        if (mapping.oldMixerGroup == audioSource.outputAudioMixerGroup && mapping.newMixerGroup != null)
                        {
                            Undo.RecordObject(audioSource, "Migrate Audio Mixer Group");
                            audioSource.outputAudioMixerGroup = mapping.newMixerGroup;
                            totalChanged++;
                            Debug.Log($"Modified: {GetFullPath(audioSource.gameObject)} - {audioSource.outputAudioMixerGroup.name} → {mapping.newMixerGroup.name}", audioSource);
                            break;
                        }
                    }
                }
            }
        }
        
        EditorUtility.DisplayDialog("Scene Migration Complete", 
            $"Modified {totalChanged} AudioSources in the current scene.", 
            "OK");
    }
    
    private int MigrateGameObject(GameObject obj)
    {
        int changedCount = 0;
        AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>(true);
        
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource.outputAudioMixerGroup != null)
            {
                foreach (var mapping in mappings)
                {
                    if (mapping.oldMixerGroup == audioSource.outputAudioMixerGroup && mapping.newMixerGroup != null)
                    {
                        audioSource.outputAudioMixerGroup = mapping.newMixerGroup;
                        changedCount++;
                        break;
                    }
                }
            }
        }
        
        return changedCount;
    }
    
    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    private void SaveMappings()
    {
        string json = JsonUtility.ToJson(new MappingsWrapper { mappings = mappings.ToArray() });
        EditorPrefs.SetString("AudioMixerMigration_Mappings", json);
    }
    
    private void LoadMappings()
    {
        string json = EditorPrefs.GetString("AudioMixerMigration_Mappings", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var wrapper = JsonUtility.FromJson<MappingsWrapper>(json);
                mappings = new List<MixerGroupMapping>(wrapper.mappings);
            }
            catch
            {
                mappings = new List<MixerGroupMapping>();
            }
        }
    }
    
    [System.Serializable]
    private class MappingsWrapper
    {
        public MixerGroupMapping[] mappings;
    }
}