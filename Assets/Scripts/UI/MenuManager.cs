using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    public List<MenuBase> menus;

    private Dictionary<string, MenuBase> menuById = new();
    private Stack<string> menuStack = new();
    private string currentMenuId;

    void Awake()
    {
        foreach (var menu in menus)
        {
            if (!string.IsNullOrEmpty(menu.MenuId))
                menuById[menu.MenuId] = menu;
        }
    }

    private void Start()
    {
        // Try to open the first menu that is already active in the scene
        foreach (var menu in menus)
        {
            if (menu.gameObject.activeSelf)
            {
                OpenMenu(menu.MenuId);
                return;
            }
        }

        // Fallback: open the menu that has no parent (assumed to be root)
        foreach (var menu in menus)
        {
            if (string.IsNullOrEmpty(menu.ParentMenuId))
            {
                OpenMenu(menu.MenuId);
                return;
            }
        }

        Debug.LogWarning("[MenuManager] No suitable menu found to open at Start.");
    }


    public void OpenMenu(string menuId)
    {
        if (!string.IsNullOrEmpty(currentMenuId) && currentMenuId != menuId)
            menuStack.Push(currentMenuId);

        // Disable input on all menus in the stack except the current one
        foreach (var menu in menuStack)
        {
            if (menuById.TryGetValue(menu, out var m))
                m.GetComponent<CanvasGroup>().interactable = false;
        }
        // Enable input on the menu being opened
        if (menuById.TryGetValue(menuId, out var menuToOpen))
            menuToOpen.GetComponent<CanvasGroup>().interactable = true;

        currentMenuId = menuId;

        // Close all other menus and open the selected one
        foreach (var menu in menus)
        {
            if (menu.MenuId == menuId)
                menu.Open();
            else
                menu.Close();
        }
    }

    public void GoBack()
    {
        if (menuStack.Count > 0)
        {
            string previous = menuStack.Pop();
            OpenMenu(previous);
        }
    }
}