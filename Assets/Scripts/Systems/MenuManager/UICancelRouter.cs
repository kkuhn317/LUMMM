using System.Collections.Generic;
using System.Security;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UICancelRouter : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private GUIManager guiManager;
    [SerializeField] private PauseMenuController pauseController;

    [Header("Input Source (set to PauseOwner while paused)")]
    [SerializeField] private PlayerInput inputSource;

    [Header("Input Settings")]
    [SerializeField] private string cancelActionName = "Cancel";

    [Header("Audio")]
    [SerializeField] private AudioClip cancelSfx;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private InputAction cancelAction;
    private int cancelLockCount = 0;
    public bool IsCancelLocked => cancelLockCount > 0;

    public void LockCancel()   => cancelLockCount++;
    public void UnlockCancel() { if (cancelLockCount > 0) cancelLockCount--; }

    private readonly List<ICancelHandler> handlerBuffer = new();

    public void SetInputSource(PlayerInput source)
    {
        Unhook();
        inputSource = source;
        RebindCancelAction();
        if (gameObject.activeInHierarchy && enabled)
            Hook();
    }

    private void RebindCancelAction()
    {
        cancelAction = null;
        if (inputSource == null || inputSource.actions == null) return;
        cancelAction = inputSource.actions.FindAction(cancelActionName, throwIfNotFound: false);
        if (cancelAction == null && verboseLogs)
            Debug.LogWarning($"[UICancelRouter] Cancel action '{cancelActionName}' not found on '{inputSource.gameObject.name}'.");
    }

    private void Awake()
    {
        if (guiManager == null) guiManager = FindObjectOfType<GUIManager>(true);
        if (pauseController == null) pauseController  = FindObjectOfType<PauseMenuController>(true);
        if (inputSource == null) inputSource = FindObjectOfType<PlayerInput>(true);

        RebindCancelAction();
        if (verboseLogs) Debug.Log($"[UICancelRouter] Awake - inputSource={inputSource?.gameObject.name ?? "null"}");
    }

    private void OnEnable()
    {
        Hook();
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void Hook()
    {
        if (cancelAction != null)
            cancelAction.performed += OnCancel;

        if (verboseLogs)
            Debug.Log($"[UICancelRouter] Hook() - cancelAction={cancelAction?.name ?? "null"}, inputSource={inputSource?.gameObject.name ?? "null"}");
    }

    private void Unhook()
    {
        if (cancelAction != null)
            cancelAction.performed -= OnCancel;
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        Debug.Log("Pressed cancel");

        if (!CanProcessCancel(ctx.control?.device)) return;

        if (verboseLogs) Debug.Log("[UICancelRouter] Processing cancel...");

        bool consumed = false;

        // 1. Focused widget handler (input field, slider, custom cancel handler on selected object)
        if (!consumed) consumed = TryCancelFocusedWidget();

        // 2. Active menu handler (ICancelHandler on the top menu's children like a rebind operation in progress)
        if (!consumed) consumed = TryCancelActiveMenu();

        // 3. Back navigation (pops history, or invokes rootBackButton at root)
        if (!consumed) consumed = TryBackNavigation();

        // 4. Global fallback at root in StandaloneOptionsMenu, Esc should toggle pause
        if (!consumed) consumed = TryGlobalFallback();

        if (consumed)
        {
            if (cancelSfx != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(cancelSfx, SoundCategory.SFX);
            }
            pauseController?.MarkCancelConsumedThisFrame();
            GlobalEventHandler.TriggerCancelConsumed();
            if (verboseLogs) Debug.Log("[UICancelRouter] Cancel consumed.");
        }
        else
        {
            if (verboseLogs) Debug.Log("[UICancelRouter] Cancel not consumed.");
        }
    }

    /// <summary>
    /// Checks the currently selected UI object and its parents for an ICancelHandler.
    /// Handles things like sliders that should deselect on cancel, or input fields.
    /// </summary>
    private bool TryCancelFocusedWidget()
    {
        if (EventSystem.current == null) return false;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;

        var handler = FindBestHandlerInParents(selected);
        if (handler == null) return false;

        bool consumed = handler.OnCancel();
        if (verboseLogs) Debug.Log($"[UICancelRouter] TryCancelFocusedWidget: handler={handler.GetType().Name}, consumed={consumed}");
        return consumed;
    }

    /// <summary>
    /// Checks the top menu panel's children for an ICancelHandler.
    /// Handles things like an active rebind operation that needs to be cancelled
    /// before allowing back navigation.
    /// Note: Only fires if the handler is NOT also the focused widget handler
    /// (to avoid double-firing). We rely on CancelPriority to break ties.
    /// </summary>
    private bool TryCancelActiveMenu()
    {
        if (guiManager == null) return false;
        var topMenu = guiManager.GetTopMenuObject();
        if (topMenu == null) return false;

        // Skip: TryCancelFocusedWidget already covers selected-object handlers.
        // Here we look for menu-level handlers that are NOT on the selected object's chain.
        var selected = EventSystem.current?.currentSelectedGameObject;

        handlerBuffer.Clear();
        var behaviours = topMenu.GetComponentsInChildren<MonoBehaviour>(includeInactive: false);
        foreach (var mb in behaviours)
        {
            if (mb is ICancelHandler h)
            {
                // Skip if this handler would also be found via the selected-object chain
                if (selected != null && mb.transform.IsChildOf(selected.transform)) continue;
                if (selected != null && selected.transform.IsChildOf(mb.transform)) continue;
                handlerBuffer.Add(h);
            }
        }

        var handler = PickHighestPriority(handlerBuffer);
        if (handler == null) return false;

        bool consumed = handler.OnCancel();
        if (verboseLogs) Debug.Log($"[UICancelRouter] TryCancelActiveMenu: handler={handler.GetType().Name}, consumed={consumed}");
        return consumed;
    }

    private bool TryBackNavigation()
    {
        if (guiManager == null) return false;

        if (guiManager.CanGoBackOrExit())
        {
            if (verboseLogs)
            {
                Debug.Log(
                    $"[UICancelRouter] TryBackNavigation: calling Back() " +
                    $"(CanGoBack={guiManager.CanGoBack()}, CanGoBackOrExit={guiManager.CanGoBackOrExit()})"
                );
            }

            guiManager.Back();
            return true;
        }

        return false;
    }

    private bool TryGlobalFallback()
    {
        return false;
    }

    // decides whether to process this cancel event at all
    private bool CanProcessCancel(InputDevice device)
    {
        if (guiManager == null)
        {
            if (verboseLogs) Debug.Log("[UICancelRouter] CanProcessCancel FAIL: guiManager null");
            return false;
        }

        if (inputSource == null)
        {
            if (verboseLogs) Debug.Log("[UICancelRouter] CanProcessCancel FAIL: inputSource null");
            return false;
        }

        if (IsCancelLocked)
        {
            if (verboseLogs) Debug.Log("[UICancelRouter] CanProcessCancel FAIL: locked");
            return false;
        }

        bool isPaused = pauseController != null && pauseController.IsPaused;
        bool isStandalone = pauseController != null &&
                            pauseController.Mode == PauseMenuController.PauseMenuMode.StandaloneOptionsMenu;
        var map = inputSource.currentActionMap;

        if (verboseLogs) Debug.Log($"[UICancelRouter] CanProcessCancel: isPaused={isPaused}, isStandalone={isStandalone}, map='{map?.name ?? "null"}'");

        // If it's StandaloneOptionsMenu, allow cancel regardless of action map (scene may start without a map).
        if (isStandalone) return true;

        // Require UI action map to be active.
        if (map == null || !string.Equals(map.name, "UI"))
        {
            if (verboseLogs) Debug.Log($"[UICancelRouter] CanProcessCancel FAIL: map='{map?.name}'");
            return false;
        }

        // When paused, only accept input from the owner's device.
        if (isPaused)
        {
            var owner = pauseController.PauseOwner;
            if (owner != null && device != null)
            {
                bool ownerHasDevice = false;
                foreach (var d in owner.devices) if (d == device) { ownerHasDevice = true; break; }
                if (!ownerHasDevice)
                {
                    if (verboseLogs) Debug.Log("[UICancelRouter] CanProcessCancel FAIL: wrong owner device");
                    return false;
                }
            }
        }

        return true;
    }

    private ICancelHandler FindBestHandlerInParents(GameObject start)
    {
        handlerBuffer.Clear();
        var behaviours = start.GetComponentsInParent<MonoBehaviour>(includeInactive: true);
        foreach (var mb in behaviours) if (mb is ICancelHandler h) handlerBuffer.Add(h);
        return PickHighestPriority(handlerBuffer);
    }

    private static ICancelHandler PickHighestPriority(List<ICancelHandler> list)
    {
        ICancelHandler best = null;
        foreach (var h in list) if (best == null || h.CancelPriority > best.CancelPriority) best = h;
        return best;
    }
}