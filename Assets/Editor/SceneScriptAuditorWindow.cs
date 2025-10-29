using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneScriptAuditorWindow : EditorWindow
{
    private class Entry
    {
        public GameObject go;
        public Component component;       // may be null if missing script
        public string scriptName;         // "[Missing Script]" or component type name
        public bool goActive;
        public bool componentEnabled;     // only meaningful for Behaviour
    }

    // UI state
    private Vector2 _scroll;
    private string _search = string.Empty;
    private bool _includeInactive = true;
    private bool _includeDisabledComponents = true;
    private bool _showMissingScripts = true;

    // Data
    private readonly List<Entry> _entries = new List<Entry>();
    private readonly Dictionary<string, List<Entry>> _byScript = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);

    [MenuItem("Tools/Fullscreen Detective/Scene Script Auditor")]
    public static void ShowWindow()
    {
        var win = GetWindow<SceneScriptAuditorWindow>("Scene Script Auditor");
        win.minSize = new Vector2(600, 320);
        win.Refresh();
    }

    private void OnEnable()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        Undo.undoRedoPerformed += Refresh;
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        Undo.undoRedoPerformed -= Refresh;
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode) => Refresh();

    private void OnFocus() => Repaint();

    private GUIStyle SmallGrayLabel
    {
        get
        {
            var s = new GUIStyle(EditorStyles.label);
            s.fontSize = 10;
            s.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            return s;
        }
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);

        if (_entries.Count == 0)
        {
            EditorGUILayout.HelpBox("No data yet. Click Refresh to scan the active scene.", MessageType.Info);
        }

        // Header
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("GameObject", GUILayout.Width(260));
            GUILayout.Label("Script", GUILayout.Width(240));
            GUILayout.Label("Active", GUILayout.Width(48));
            GUILayout.Label("Enabled", GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                // noop: header button just a hint
            }
        }

        // List
        try
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var e in FilteredEntries())
            {
                DrawRow(e);
            }
        }
        catch (Exception ex)
        {
            // IMGUI can throw on layout mismatches; keep the window resilient.
            Debug.LogWarning($"SceneScriptAuditor IMGUI glitch: {ex.Message}");
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Total: {_entries.Count} | Shown: {FilteredEntries().Count()}", SmallGrayLabel);
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            // Basic search field (works in all LTS)
            GUILayout.Label("Search", GUILayout.Width(48));
            var newSearch = GUILayout.TextField(_search ?? string.Empty, EditorStyles.toolbarTextField, GUILayout.MinWidth(140));
            if (!string.Equals(newSearch, _search, StringComparison.Ordinal))
                _search = newSearch;

            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(24)))
                _search = string.Empty;

            GUILayout.Space(8);

            _includeInactive = GUILayout.Toggle(_includeInactive, "Include Inactive GOs", EditorStyles.toolbarButton);
            _includeDisabledComponents = GUILayout.Toggle(_includeDisabledComponents, "Include Disabled Components", EditorStyles.toolbarButton);
            _showMissingScripts = GUILayout.Toggle(_showMissingScripts, "Show Missing Scripts", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                Refresh();

            if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(90)))
                ExportCsv();

            if (GUILayout.Button("Find 'Screen.fullScreen' in Project", EditorStyles.toolbarButton, GUILayout.Width(240)))
                ProjectSearch_FindFullscreenUsages();
        }
    }

    private IEnumerable<IGrouping<string, Entry>> FilteredGroups()
        => FilteredEntries().GroupBy(e => e.scriptName).OrderBy(g => g.Key);

    private IEnumerable<Entry> FilteredEntries()
    {
        IEnumerable<Entry> q = _entries;

        if (!_includeInactive)
            q = q.Where(e => e.goActive);

        if (!_includeDisabledComponents)
            q = q.Where(e => e.component == null || e.componentEnabled);

        if (!_showMissingScripts)
            q = q.Where(e => e.component != null);

        if (!string.IsNullOrEmpty(_search))
        {
            var s = _search.Trim();
            q = q.Where(e =>
                (e.go && e.go.name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(e.scriptName) && e.scriptName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        return q;
    }

    private void DrawRow(Entry e)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // GameObject field (ping on click)
            EditorGUILayout.ObjectField(e.go, typeof(GameObject), true, GUILayout.Width(260));

            // Script name
            var label = e.scriptName ?? "(unknown)";
            if (e.component == null)
                label = $"[Missing] {label}";
            EditorGUILayout.LabelField(label, GUILayout.Width(240));

            // Active state
            GUILayout.Label(e.goActive ? "Yes" : "No", GUILayout.Width(48));

            // Enabled state (only for Behaviour)
            GUILayout.Label(e.componentEnabled ? "Yes" : "No", GUILayout.Width(60));

            // Ping button
            if (GUILayout.Button("Ping", GUILayout.Width(60)))
            {
                Selection.activeObject = e.go;
                EditorGUIUtility.PingObject(e.go);
            }
        }
    }

    private void Refresh()
    {
        _entries.Clear();
        _byScript.Clear();

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var c in components)
            {
                if (c == null)
                {
                    // Missing script
                    if (_showMissingScripts)
                    {
                        _entries.Add(new Entry
                        {
                            go = GetOwnerGameObjectFromMissing(components, root), // fallback to object in loop
                            component = null,
                            scriptName = "[Missing Script]",
                            goActive = root.activeInHierarchy,
                            componentEnabled = false
                        });
                    }
                    continue;
                }

                // We only care about scripts/Behaviours (ignore Transform/Renderer/etc. if you prefer)
                if (!(c is MonoBehaviour) && !(c is Behaviour))
                    continue;

                var beh = c as Behaviour;
                var e = new Entry
                {
                    go = c.gameObject,
                    component = c,
                    scriptName = c.GetType().Name,
                    goActive = c.gameObject.activeInHierarchy,
                    componentEnabled = beh ? beh.enabled : true
                };
                _entries.Add(e);
            }
        }

        foreach (var e in _entries)
        {
            var key = e.scriptName ?? "(unknown)";
            if (!_byScript.TryGetValue(key, out var list))
            {
                list = new List<Entry>();
                _byScript[key] = list;
            }
            list.Add(e);
        }

        Repaint();
    }

    private GameObject GetOwnerGameObjectFromMissing(Component[] context, GameObject fallback)
    {
        // When a Component is null in an array it still belongs to the current GO; try to find a close owner.
        return fallback != null ? fallback : (context != null && context.Length > 0 ? context[0].gameObject : null);
    }

    private void ExportCsv()
    {
        var path = EditorUtility.SaveFilePanel("Export Scene Script List", Application.dataPath, "SceneScriptAudit.csv", "csv");
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            using (var sw = new StreamWriter(path))
            {
                sw.WriteLine("GameObject,Script,Active,Enabled,Path");
                foreach (var e in FilteredEntries())
                {
                    var goPath = GetHierarchyPath(e.go);
                    sw.WriteLine($"{Escape(e.go?.name)},{Escape(e.scriptName)},{(e.goActive ? "true" : "false")},{(e.componentEnabled ? "true" : "false")},{Escape(goPath)}");
                }
            }
            EditorUtility.RevealInFinder(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"CSV export failed: {ex.Message}");
        }
    }

    private string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\""))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private string GetHierarchyPath(GameObject go)
    {
        if (go == null) return "";
        var stack = new Stack<string>();
        var t = go.transform;
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }

    [MenuItem("Tools/Fullscreen Detective/Find 'Screen.fullScreen' Usages")]
    private static void ProjectSearch_FindFullscreenUsages()
    {
        try
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var hits = new List<string>();
            foreach (var p in assetPaths)
            {
                var text = File.ReadAllText(p);
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Contains("Screen.fullScreen") || line.Contains("SetFullscreen("))
                    {
                        hits.Add($"{p}:{i + 1}  {line.Trim()}");
                    }
                }
            }

            var log = "===== Screen.fullScreen / SetFullscreen usages =====\n";
            if (hits.Count == 0) log += "(none found)\n";
            else log += string.Join("\n", hits) + "\n";
            Debug.Log(log);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Project search failed: {ex.Message}");
        }
    }
}