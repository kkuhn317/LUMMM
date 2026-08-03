using System.Collections.Generic;
using System.Security;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

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
    private readonly List<ICancelHandler> sortedBuffer = new();

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
        if (verboseLogs) Debug.Log("[UICancelRouter] Pressed cancel");

        if (!CanProcessCancel(ctx.control?.device)) return;

        if (verboseLogs) Debug.Log("[UICancelRouter] Processing cancel...");

        bool consumed = false;

        // Modal popups.
        // TMP_Dropdown implements UnityEngine.EventSystems.ICancelHandler, which is a DIFFERENT
        // type from this project's global ICancelHandler despite the identical name. An open
        // dropdown list is therefore invisible to stages 1 and 2, and cancel used to fall
        // through to Back() -> rootBackButton -> scene transition while the list was still up.
        // Also note GUIManager.Back() calls rootBackButton.Select(), which moves selection away
        // from the option Toggle before InputSystemUIInputModule can deliver its own cancel —
        // so the dropdown never closed itself either.
        if (!consumed) consumed = TryCloseOpenDropdown();

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
    /// closes an open TMP_Dropdown list, if any. Prefers the dropdown in the current
    /// selection's parent chain (correct when several are on screen); falls back to a scan for
    /// mouse-driven use where selection may be null.
    /// </summary>
    private bool TryCloseOpenDropdown()
    {
        TMP_Dropdown open = null;

        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null)
        {
            // Valid because TMP parents the runtime "Dropdown List" under the dropdown itself.
            var candidate = selected.GetComponentInParent<TMP_Dropdown>();
            if (candidate != null && candidate.IsExpanded) open = candidate;
        }

        if (open == null)
        {
            var all = FindObjectsOfType<TMP_Dropdown>();
            foreach (var d in all)
            {
                if (d != null && d.IsExpanded) { open = d; break; }
            }
        }

        if (open == null) return false;

        // Hide() also re-selects the dropdown, so focus lands back on the control the player
        // was using rather than on a Toggle that is about to be destroyed.
        open.Hide();

        if (verboseLogs) Debug.Log($"[UICancelRouter] TryCloseOpenDropdown: closed '{open.name}'.");
        return true;
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

        // collect every handler in the parent chain and try them in priority
        // order, instead of committing to the single highest-priority one. A handler that
        // declines (returns false) must not block the ones beneath it.
        CollectHandlersInParents(selected, handlerBuffer);
        return TryConsumeInPriorityOrder(handlerBuffer, "TryCancelFocusedWidget");
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

        // same priority-ordered fallthrough as above.
        return TryConsumeInPriorityOrder(handlerBuffer, "TryCancelActiveMenu");
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

    // returns all candidates rather than one.
    private static void CollectHandlersInParents(GameObject start, List<ICancelHandler> results)
    {
        results.Clear();
        var behaviours = start.GetComponentsInParent<MonoBehaviour>(includeInactive: true);
        foreach (var mb in behaviours) if (mb is ICancelHandler h) results.Add(h);
    }

    /// <summary>
    /// replaces PickHighestPriority(). Tries handlers from highest to lowest
    /// priority and stops at the first one that reports the press as consumed.
    ///
    /// The old behaviour picked exactly one handler and gave up if it declined, so a
    /// high-priority handler that returns false in some states (RebindMenuCancelInterceptor
    /// does this when it isn't the top menu; DropdownCancelHandler does it when its list is
    /// closed) could silently swallow the press for everyone else.
    /// </summary>
    private bool TryConsumeInPriorityOrder(List<ICancelHandler> candidates, string context)
    {
        if (candidates.Count == 0) return false;

        sortedBuffer.Clear();
        sortedBuffer.AddRange(candidates);
        sortedBuffer.Sort((a, b) => b.CancelPriority.CompareTo(a.CancelPriority));

        foreach (var handler in sortedBuffer)
        {
            if (handler == null) continue;
            if (!handler.OnCancel()) continue;

            if (verboseLogs)
                Debug.Log($"[UICancelRouter] {context}: consumed by {handler.GetType().Name} (priority {handler.CancelPriority})");
            return true;
        }

        if (verboseLogs)
            Debug.Log($"[UICancelRouter] {context}: {sortedBuffer.Count} handler(s) declined.");
        return false;
    }
}