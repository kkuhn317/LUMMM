using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GUIManager : MonoBehaviour
{
    public static GUIManager Instance { get; private set; }

    [Header("Menu Hierarchy")]
    [Tooltip("All menus in the project. Set parentMenuName to define hierarchy. Leave parentMenuName empty for the root menu.")]
    public List<MenuData> menus = new List<MenuData>();
    [SerializeField] private Button rootBackButton;

    private Stack<GameObject> menuHistory = new Stack<GameObject>();
    private Stack<Selectable> selectableHistory = new Stack<Selectable>();

    private GameObject currentActiveMenu;
    private Dictionary<string, GameObject> menuLookup = new Dictionary<string, GameObject>();
    private Dictionary<string, string> parentLookup = new Dictionary<string, string>();
    private Dictionary<string, Selectable> defaultSelectable = new Dictionary<string, Selectable>();
    private Dictionary<GameObject, string> reverseLookup = new Dictionary<GameObject, string>();
    private string rootMenuName;
    private bool isTransitioning = false;
    private bool pendingBack = false;

    /// <summary>
    /// Registers the player that opened the menus as the menu owner.
    /// Only this player's input will be accepted for navigation and cancel.
    /// 
    /// INTEGRATION: Call this from PauseMenuController.PauseGameInternal and ResumeGame:
    ///
    ///   // In PauseGameInternal, after cancelRouter.SetInputSource(owner):
    ///   if (GUIManager.Instance != null) GUIManager.Instance.SetOwner(owner);
    ///
    ///   // In ResumeGame, after cancelRouter.SetInputSource(null):
    ///   if (GUIManager.Instance != null) GUIManager.Instance.SetOwner(null);
    /// </summary>
    public void SetOwner(UnityEngine.InputSystem.PlayerInput owner)
    {
        if (owner != null)
            MenuOwnership.Claim(owner);
        else
            MenuOwnership.Release();

        // Inject ownership into all MenuButtons so their click guards work
        // regardless of where they sit in the hierarchy.
        var buttons = GetComponentsInChildren<MenuButton>(includeInactive: true);
        foreach (var btn in buttons)
            btn.SetPlayerInput(owner);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!BuildMenuLookup())
            return;

        InitializeMenus();
    }

    public Button GetRootBackButton()
    {
        return rootBackButton;
    }

    private bool BuildMenuLookup()
    {
        menuLookup.Clear();
        parentLookup.Clear();
        defaultSelectable.Clear();
        rootMenuName = null;

        foreach (var menu in menus)
        {
            if (string.IsNullOrEmpty(menu.menuName))
            {
                Debug.LogError("GUIManager: A menu entry has an empty menuName. Please fill in all menu names.");
                return false;
            }

            if (menu.menuPanel == null)
            {
                Debug.LogError($"GUIManager: Menu '{menu.menuName}' has no menuPanel assigned.");
                return false;
            }

            if (menuLookup.ContainsKey(menu.menuName))
            {
                Debug.LogError($"GUIManager: Duplicate menu name '{menu.menuName}' found. All menu names must be unique.");
                return false;
            }

            menuLookup[menu.menuName]        = menu.menuPanel;
            parentLookup[menu.menuName]      = menu.parentMenuName;
            defaultSelectable[menu.menuName] = menu.defaultSelected;
            reverseLookup[menu.menuPanel]    = menu.menuName;

            if (string.IsNullOrEmpty(menu.parentMenuName))
            {
                if (rootMenuName != null)
                {
                    Debug.LogError($"GUIManager: Multiple root menus found ('{rootMenuName}' and '{menu.menuName}'). Only one menu can have an empty parentMenuName.");
                    return false;
                }
                rootMenuName = menu.menuName;
            }
        }

        if (rootMenuName == null)
        {
            Debug.LogError("GUIManager: No root menu found. One menu must have an empty parentMenuName.");
            return false;
        }

        foreach (var menu in menus)
        {
            if (!string.IsNullOrEmpty(menu.parentMenuName) && !menuLookup.ContainsKey(menu.parentMenuName))
            {
                Debug.LogError($"GUIManager: Menu '{menu.menuName}' has parentMenuName '{menu.parentMenuName}', but no menu with that name exists.");
                return false;
            }
        }

        foreach (var menu in menus)
        {
            if (HasCycle(menu.menuName))
                return false;
        }

        return true;
    }

    private bool HasCycle(string startMenuName)
    {
        var visited = new HashSet<string>();
        string current = startMenuName;

        while (!string.IsNullOrEmpty(current))
        {
            if (visited.Contains(current))
            {
                Debug.LogError($"GUIManager: Cycle detected in menu hierarchy involving '{current}'.");
                return true;
            }
            visited.Add(current);
            parentLookup.TryGetValue(current, out current);
        }

        return false;
    }

    private void InitializeMenus()
    {
        // Hide all panels immediately (no transition on init)
        foreach (var kvp in menuLookup)
        {
            if (kvp.Value != null)
                kvp.Value.SetActive(false);
        }

        if (menuLookup.TryGetValue(rootMenuName, out GameObject rootPanel))
        {
            rootPanel.SetActive(true);
            currentActiveMenu = rootPanel;
            menuHistory.Clear();
            selectableHistory.Clear();
            menuHistory.Push(rootPanel);

            SelectDefault(rootMenuName);
        }
    }

    private void FinishTransition()
    {
        isTransitioning = false;
        if (pendingBack)
        {
            pendingBack = false;
            Back();
        }
    }

    public bool CanGoBack()
    {
        return !isTransitioning && menuHistory != null && menuHistory.Count > 1;
    }

    /// <summary>
    /// True if Back() will actually do something — either there's history to pop,
    /// or the root back button exists, is active in hierarchy, and is interactable.
    /// Use this before consuming cancel input so inactive root buttons don't
    /// silently swallow the press.
    /// </summary>
    public bool CanGoBackOrExit()
    {
        if (CanGoBack()) return true;
        return rootBackButton != null &&
               rootBackButton.gameObject.activeInHierarchy &&
               rootBackButton.interactable;
    }

    public GameObject GetTopMenuObject()
    {
        if (menuHistory == null || menuHistory.Count == 0)
            return null;

        return menuHistory.Peek();
    }

    /// <summary>
    /// Opens a menu, hiding the previous one. The returnTo selectable will be
    /// focused when Back() is called. If null, the destination's defaultSelected is used.
    /// </summary>
    public void OpenMenu(string menuName, Selectable returnTo = null)
    {
        OpenMenu(menuName, hidePrevious: true, returnTo: returnTo);
    }

    public void OpenMenu(string menuName, bool hidePrevious, Selectable returnTo = null)
    {
        if (isTransitioning) return;

        if (string.IsNullOrEmpty(menuName))
        {
            Debug.LogError("GUIManager: Menu name cannot be null or empty!");
            return;
        }

        if (!menuLookup.TryGetValue(menuName, out GameObject menuToOpen))
        {
            Debug.LogError($"GUIManager: Menu '{menuName}' not found! Available menus: {string.Join(", ", menuLookup.Keys)}");
            return;
        }

        GameObject topMenu = GetTopMenuObject();
        if (topMenu == menuToOpen)
        {
            currentActiveMenu = menuToOpen;

            var cg = GetCanvasGroup(menuToOpen);
            cg.interactable = true;
            cg.blocksRaycasts = true;

            if (returnTo != null)
                SelectImmediate(returnTo);
            else
                SelectDefault(menuName);

            return;
        }

        isTransitioning = true;

        menuHistory.Push(menuToOpen);
        selectableHistory.Push(returnTo);

        void OnHideComplete()
        {
            ShowPanel(menuToOpen, onComplete: () =>
            {
                currentActiveMenu = menuToOpen;
                SelectDefault(menuName);
                FinishTransition();
                GlobalEventHandler.TriggerMenuOpened(menuName);
            });
        }

        if (currentActiveMenu != null && hidePrevious && currentActiveMenu != menuToOpen)
            HidePanel(currentActiveMenu, OnHideComplete);
        else
            OnHideComplete();
    }

    public void OpenSubMenu(string subMenuName, Selectable returnTo = null)
    {
        OpenMenu(subMenuName, hidePrevious: true, returnTo: returnTo);
    }

    public void OpenMenuAsOverlay(string menuName, Selectable returnTo = null)
    {
        if (isTransitioning) return;

        if (string.IsNullOrEmpty(menuName))
        {
            Debug.LogError("GUIManager: Menu name cannot be null or empty!");
            return;
        }

        if (!menuLookup.TryGetValue(menuName, out GameObject menuToOpen))
        {
            Debug.LogError($"GUIManager: Overlay '{menuName}' not found! Available menus: {string.Join(", ", menuLookup.Keys)}");
            return;
        }

        isTransitioning = true;

        // Block all panels currently in the stack before pushing the new one
        foreach (var menu in menuHistory)
        {
            var cg = GetCanvasGroup(menu);
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        menuHistory.Push(menuToOpen);
        selectableHistory.Push(returnTo);

        ShowPanel(menuToOpen, onComplete: () =>
        {
            currentActiveMenu = menuToOpen;

            // Ensure the overlay panel itself is interactable before selecting.
            // Previously-closed panels may have interactable=false from Back() cleanup.
            var cg = GetCanvasGroup(menuToOpen);
            cg.interactable = true;
            cg.blocksRaycasts = true;

            SelectDefault(menuName);
            FinishTransition();
            GlobalEventHandler.TriggerMenuOpened(menuName);
        });
    }

    public void Back()
    {
        if (menuHistory.Count <= 1)
        {
            if (rootBackButton != null && rootBackButton.gameObject.activeInHierarchy && rootBackButton.interactable)
            {
                rootBackButton.Select();
                rootBackButton.onClick.Invoke();
            }
            return;
        }

        if (isTransitioning)
        {
            pendingBack = true;
            TryInterruptTransition(currentActiveMenu);
            return;
        }

        isTransitioning = true;
        GameObject menuToHide = menuHistory.Pop();
        GameObject previousMenu = menuHistory.Peek();
        Selectable returnTarget = selectableHistory.Count > 0 ? selectableHistory.Pop() : null;

        HidePanel(menuToHide, () => {
            ShowPanel(previousMenu, () => {
                currentActiveMenu = previousMenu;
                var cg = GetCanvasGroup(previousMenu);
                cg.interactable = true;
                cg.blocksRaycasts = true;
                if (returnTarget != null) SelectImmediate(returnTarget);
                else SelectDefaultForPanel(previousMenu);
                FinishTransition();
                GlobalEventHandler.TriggerGUIPop();
            });
        });
    }

    public void BackKeepOverlay()
    {
        if (isTransitioning || menuHistory.Count <= 1) return;

        isTransitioning = true;

        menuHistory.Pop();
        GameObject previousMenu = menuHistory.Peek();
        Selectable returnTarget = selectableHistory.Count > 0 ? selectableHistory.Pop() : null;

        // Previous menu stays visible — just unblock it
        var cg = GetCanvasGroup(previousMenu);
        cg.interactable = true;
        cg.blocksRaycasts = true;

        currentActiveMenu = previousMenu;

        if (returnTarget != null)
            SelectImmediate(returnTarget);
        else
            SelectDefaultForPanel(previousMenu);

        FinishTransition();
        GlobalEventHandler.TriggerGUIPop();
    }

    public void CloseAllMenus()
    {
        if (isTransitioning) return;

        isTransitioning = true;

        // Deselect immediately so the button that triggered close (e.g. gamepad B)
        // doesn't fire a phantom click on the newly-visible root panel next frame.
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        // Collect all menus except root to hide them
        var toClose = new List<GameObject>();
        var historyArray = menuHistory.ToArray(); // top → bottom order

        for (int i = 0; i < historyArray.Length - 1; i++)
        {
            if (historyArray[i] != null)
                toClose.Add(historyArray[i]);
        }

        selectableHistory.Clear();

        // Rebuild history with only root
        menuHistory.Clear();
        GameObject root = historyArray[historyArray.Length - 1];
        menuHistory.Push(root);

        // Hide all non-root panels immediately (skip animations for bulk close)
        foreach (var panel in toClose)
            panel.SetActive(false);

        ShowPanel(root, onComplete: () =>
        {
            currentActiveMenu = root;

            var cg = GetCanvasGroup(root);

            // Briefly block interaction for one frame so the input that triggered
            // CloseAllMenus (e.g. gamepad B / keyboard Escape) cannot immediately
            // click a button on the root panel before the EventSystem clears it.
            cg.interactable = false;
            cg.blocksRaycasts = false;

            StartCoroutine(RestoreRootInteractionNextFrame(cg));

            MenuOwnership.Release();
            var buttons = GetComponentsInChildren<MenuButton>(includeInactive: true);
            foreach (var btn in buttons)
                btn.SetPlayerInput(null);
            FinishTransition();
            GlobalEventHandler.TriggerMenuClosed();
        });
    }

    public void CloseOverlay(string overlayName)
    {
        if (isTransitioning) return;

        if (string.IsNullOrEmpty(overlayName))
        {
            Debug.LogError("GUIManager: Overlay name cannot be null or empty!");
            return;
        }

        if (!menuLookup.TryGetValue(overlayName, out GameObject overlay))
        {
            Debug.LogError($"GUIManager: Overlay '{overlayName}' not found!");
            return;
        }

        if (!overlay.activeSelf) return;

        isTransitioning = true;

        HidePanel(overlay, onComplete: () =>
        {
            if (menuHistory.Count > 0 && menuHistory.Peek() == overlay)
            {
                menuHistory.Pop();

                // restore focus when overlay is dismissed
                Selectable returnTarget = selectableHistory.Count > 0 ? selectableHistory.Pop() : null;

                currentActiveMenu = menuHistory.Count > 0 ? menuHistory.Peek() : null;

                if (currentActiveMenu != null)
                {
                    var cg = GetCanvasGroup(currentActiveMenu);
                    cg.interactable = true;
                    cg.blocksRaycasts = true;

                    if (returnTarget != null)
                        SelectImmediate(returnTarget);
                    else
                        SelectDefaultForPanel(currentActiveMenu);
                }
            }

            FinishTransition();
        });
    }

    public void Exit()
    {
        GlobalEventHandler.TriggerExitRequested();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Plays the hide transition on a panel. Calls SetActive(false) and onComplete
    /// after the transition finishes. Falls back to instant hide if no IMenuTransition found.
    /// </summary>
    private void HidePanel(GameObject panel, System.Action onComplete = null)
    {
        var transition = panel.GetComponent<IMenuTransition>();
        if (transition != null)
        {
            transition.OnHide(() =>
            {
                panel.SetActive(false);
                onComplete?.Invoke();
            });
        }
        else
        {
            panel.SetActive(false);
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// If a transition is in progress on this panel, snap it to completion immediately.
    /// This allows quick inputs to feel responsive even during animations.
    /// </summary>
    private void TryInterruptTransition(GameObject panel)
    {
        var transition = panel?.GetComponent<IMenuTransition>();
        transition?.Interrupt();
    }

    /// <summary>
    /// Activates a panel and plays its show transition.
    /// Falls back to instant show if no IMenuTransition found.
    /// </summary>
    private void ShowPanel(GameObject panel, System.Action onComplete = null)
    {
        panel.SetActive(true);
        var transition = panel.GetComponent<IMenuTransition>();
        if (transition != null)
            transition.OnShow(onComplete);
        else
            onComplete?.Invoke();
    }

    private CanvasGroup GetCanvasGroup(GameObject panel)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        return cg;
    }

    private void SelectDefault(string menuName)
    {
        if (defaultSelectable.TryGetValue(menuName, out Selectable sel) && sel != null)
            SelectImmediate(sel);
    }

    private void SelectImmediate(Selectable target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;

        // Don't auto-focus input fields — they require explicit user interaction,
        // auto-selecting them pops the keyboard on mobile and confuses the EventSystem.
        if (target is TMPro.TMP_InputField) return;
        if (target is UnityEngine.UI.InputField) return;

        target.Select();
    }

    private void SelectDefaultForPanel(GameObject panel)
    {
        foreach (var kvp in menuLookup)
        {
            if (kvp.Value == panel)
            {
                SelectDefault(kvp.Key);
                return;
            }
        }
    }

    private Selectable GetDefaultForPanel(GameObject panel)
    {
        foreach (var kvp in menuLookup)
            if (kvp.Value == panel && defaultSelectable.TryGetValue(kvp.Key, out Selectable sel))
                return sel;
        return null;
    }

    /// <summary>
    /// Defers re-enabling interaction on the root panel by one frame after CloseAllMenus.
    /// Prevents the input that closed the menu (e.g. gamepad B) from immediately
    /// clicking a button on the root panel via EventSystem's leftover event queue.
    /// </summary>
    private System.Collections.IEnumerator RestoreRootInteractionNextFrame(CanvasGroup cg)
    {
        yield return null;
        if (cg != null)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        SelectDefaultForPanel(currentActiveMenu);
    }

    public GameObject[] GetActiveMenus()
    {
        var activeMenus = new List<GameObject>();
        foreach (var menu in menuHistory)
        {
            if (menu != null && menu.activeSelf)
                activeMenus.Add(menu);
        }
        return activeMenus.ToArray();
    }

    public int MenuHistoryDepth => menuHistory.Count;

    /// <summary>
    /// Returns the registered menu name for a given panel GameObject, or null if not found.
    /// Use this to compare top-of-stack against a known menu ID without relying on GameObject.name.
    /// </summary>
    public string GetMenuName(GameObject panel)
    {
        if (panel == null) return null;
        return reverseLookup.TryGetValue(panel, out string name) ? name : null;
    }

    public bool IsMenuActive(string menuName)
    {
        if (string.IsNullOrEmpty(menuName)) return false;
        if (menuLookup.TryGetValue(menuName, out GameObject menu))
            return menu.activeSelf;
        return false;
    }

    /// <summary>
    /// True while a show or hide transition is in progress.
    /// Use this to disable input or show a loading indicator if needed.
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}