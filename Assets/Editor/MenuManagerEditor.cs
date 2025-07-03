using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MenuManager))]
public class MenuManagerEditor : Editor
{
    private GUIStyle headerStyle;

    private void OnEnable()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MenuManager manager = (MenuManager)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Menu Hierarchy Overview", headerStyle);

        if (manager.menus != null)
        {
            foreach (var menu in manager.menus)
            {
                if (menu != null && string.IsNullOrEmpty(menu.ParentMenuId))
                {
                    DrawMenuTree(menu, 0, manager, new HashSet<string>());
                }
            }
        }
    }

    private void DrawMenuTree(MenuBase menu, int depth, MenuManager manager, HashSet<string> visited = null)
    {
        if (menu == null) return;
        
        visited ??= new HashSet<string>();

        if (visited.Contains(menu.MenuId))
        {
            EditorGUILayout.LabelField($"{new string('—', depth)} {menu.MenuId} (circular ref!)", EditorStyles.boldLabel);
            return;
        }

        visited.Add(menu.MenuId);

        string indent = new string('—', depth);
        EditorGUILayout.LabelField($"{indent} {menu.MenuId}", EditorStyles.label);

        foreach (var child in manager.menus)
        {
            if (child != null && child.ParentMenuId == menu.MenuId)
            {
                DrawMenuTree(child, depth + 1, manager, visited);
            }
        }
    }
}