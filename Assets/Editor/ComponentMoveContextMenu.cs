using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class ComponentMoveContextMenu
{
    private const int MENU_PRIORITY = 2100;

    [MenuItem("CONTEXT/Component/Move To Top", false, MENU_PRIORITY)]
    private static void MoveToTop(MenuCommand command)
    {
        var comp = command.context as Component;
        if (comp == null || comp is Transform) return;

        var go = comp.gameObject;

        Undo.SetCurrentGroupName("Move Component To Top");
        int group = Undo.GetCurrentGroup();
        int guard = go.GetComponents<Component>().Length + 5;

        while (guard-- > 0)
        {
            var list = go.GetComponents<Component>();
            int idx = Array.IndexOf(list, comp);

            if (idx <= 1) break; // it'll be under Transform

            Undo.RegisterCompleteObjectUndo(go, "Move Component");
            bool moved = ComponentUtility.MoveComponentUp(comp);

            // if Unity can't move it, break to avoid infinite loop
            if (!moved) break;
        }

        Undo.CollapseUndoOperations(group);
        EditorApplication.RepaintHierarchyWindow();
        SceneView.RepaintAll();
    }

    [MenuItem("CONTEXT/Component/Move To Bottom", false, MENU_PRIORITY + 1)]
    private static void MoveToBottom(MenuCommand command)
    {
        var comp = command.context as Component;
        if (comp == null || comp is Transform) return;

        var go = comp.gameObject;

        Undo.SetCurrentGroupName("Move Component To Bottom");
        int group = Undo.GetCurrentGroup();

        int guard = go.GetComponents<Component>().Length + 5;

        while (guard-- > 0)
        {
            var list = go.GetComponents<Component>();
            int idx = Array.IndexOf(list, comp);

            if (idx < 0 || idx >= list.Length - 1) break;

            Undo.RegisterCompleteObjectUndo(go, "Move Component");
            bool moved = ComponentUtility.MoveComponentDown(comp);

            if (!moved) break;
        }

        Undo.CollapseUndoOperations(group);
        EditorApplication.RepaintHierarchyWindow();
        SceneView.RepaintAll();
    }

    [MenuItem("CONTEXT/Component/Move To Top", true)]
    private static bool ValidateMoveToTop(MenuCommand command)
    {
        if (command.context is not Component comp || comp is Transform) return false;

        var comps = comp.gameObject.GetComponents<Component>();
        var index = Array.IndexOf(comps, comp);

        // index 0 is always Transform, so "top" means index == 1
        return index > 1;
    }

    [MenuItem("CONTEXT/Component/Move To Bottom", true)]
    private static bool ValidateMoveToBottom(MenuCommand command)
    {
        if (command.context is not Component comp || comp is Transform) return false;

        var comps = comp.gameObject.GetComponents<Component>();
        var index = Array.IndexOf(comps, comp);

        // bottom means it's the last component
        return index >= 0 && index < comps.Length - 1;
    }
}