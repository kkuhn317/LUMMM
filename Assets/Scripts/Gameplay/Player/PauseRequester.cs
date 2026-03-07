using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PauseRequester : MonoBehaviour
{
    [SerializeField] private PauseMenuController pauseController;
    [SerializeField] private string pauseActionName = "Pause";
    [SerializeField] private string standalonePauseMenuId = "RebindMenu";

    private PlayerInput pi;
    private InputActionAsset lastAsset;
    private readonly List<InputAction> pauseActions = new();

    private void Awake()
    {
        pi = GetComponent<PlayerInput>();

        if (pauseController == null)
            pauseController = FindObjectOfType<PauseMenuController>(true);
    }

    private void OnEnable()
    {
        RefreshPauseBinding();
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void Update()
    {
        if (pi != null && pi.actions != lastAsset)
            RefreshPauseBinding();
    }

    private void RefreshPauseBinding()
    {
        Unhook();

        if (pi == null || pi.actions == null)
            return;

        lastAsset = pi.actions;
        pauseActions.Clear();

        // Hook Pause in ALL action maps so Pause works in Mariomove and UI
        foreach (var map in pi.actions.actionMaps)
        {
            var action = map.FindAction(pauseActionName, throwIfNotFound: false);
            if (action == null) continue;

            action.performed += OnPause;
            pauseActions.Add(action);
        }

        if (pauseActions.Count == 0)
        {
            Debug.LogWarning(
                $"PauseRequester: Could not find action '{pauseActionName}' in any map on '{gameObject.name}'.");
        }
    }

    private void Unhook()
    {
        foreach (var action in pauseActions)
        {
            if (action != null)
                action.performed -= OnPause;
        }

        pauseActions.Clear();
    }

    private void OnPause(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (pauseController == null || pi == null) return;

        if (ctx.action.actionMap != pi.currentActionMap)
            return;

        // If Cancel already consumed this frame, do not let Pause also toggle.
        if (pauseController.WasCancelConsumedThisFrame())
            return;

        // In StandaloneOptionsMenu, only allow Pause toggle from RebindMenu, and allow pause at same time as cancel.
        if (pauseController.Mode == PauseMenuController.PauseMenuMode.StandaloneOptionsMenu)
        {
            if (!pauseController.IsMenuOpen(standalonePauseMenuId))
                return;
        }
        else
        {
            if (pauseController.IsPaused)
            {
                // In normal gameplay pause flow, only allow Pause toggle from the pause root.
                if (!pauseController.IsAtPauseRoot())
                    return;
            }
        }

        pauseController.RequestTogglePause(pi);
    }
}