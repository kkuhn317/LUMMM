using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI; // Added for GraphicRaycaster

public class UIInputLock : MonoBehaviour
{
    [Header("What to disable")]
    public InputSystemUIInputModule inputModule;
    public bool disableSendNavigationEvents = true;
    
    [Header("Disable mouse/touch clicks")]
    public bool disableGraphicRaycaster = true;
    public Canvas[] canvasesToDisable; // If empty, will find all

    private int lockCount;
    private bool isUnlocking;

    private bool savedInputModuleEnabled;
    private bool savedSendNavEvents;
    private GameObject savedSelected;
    
    // Track which actions were enabled before locking
    private bool wasPointEnabled;
    private bool wasMoveEnabled;
    private bool wasSubmitEnabled;
    private bool wasCancelEnabled;
    private bool wasClickEnabled;
    private bool wasScrollWheelEnabled;
    private bool wasMiddleClickEnabled;
    private bool wasRightClickEnabled;
    private bool wasTrackedDevicePositionEnabled;
    private bool wasTrackedDeviceOrientationEnabled;
    
    // Track GraphicRaycaster states
    private GraphicRaycaster[] graphicRaycasters;
    private bool[] raycasterEnabledStates;

    private void Awake()
    {
        if (inputModule == null)
            inputModule = FindObjectOfType<InputSystemUIInputModule>();

        if (EventSystem.current != null)
            savedSendNavEvents = EventSystem.current.sendNavigationEvents;

        // Find all GraphicRaycasters if we need to disable them
        if (disableGraphicRaycaster)
        {
            if (canvasesToDisable != null && canvasesToDisable.Length > 0)
            {
                System.Collections.Generic.List<GraphicRaycaster> raycasters = new System.Collections.Generic.List<GraphicRaycaster>();
                foreach (var canvas in canvasesToDisable)
                {
                    if (canvas != null)
                    {
                        var raycaster = canvas.GetComponent<GraphicRaycaster>();
                        if (raycaster != null) raycasters.Add(raycaster);
                    }
                }
                graphicRaycasters = raycasters.ToArray();
            }
            else
            {
                // Find all GraphicRaycasters in the scene
                graphicRaycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            }
            
            raycasterEnabledStates = new bool[graphicRaycasters.Length];
        }

        Debug.Log($"UIInputLock Awake: InputModule found: {inputModule != null}, GraphicRaycasters: {graphicRaycasters?.Length ?? 0}");
    }

    public void Lock(bool rememberSelection = true)
    {
        if (ConfirmPopup.IsAnyPopupOpen)
        {
            Debug.Log("[UILOCK] Skipped lock because popup is open");
            return;
        }

        lockCount++;

        if (lockCount == 1)
        {
            if (inputModule != null)
                savedInputModuleEnabled = inputModule.enabled;

            if (EventSystem.current != null)
            {
                savedSendNavEvents = EventSystem.current.sendNavigationEvents;
                savedSelected = rememberSelection
                    ? EventSystem.current.currentSelectedGameObject
                    : null;
            }
            
            // Save GraphicRaycaster states
            if (disableGraphicRaycaster && graphicRaycasters != null)
            {
                for (int i = 0; i < graphicRaycasters.Length; i++)
                {
                    if (graphicRaycasters[i] != null)
                    {
                        raycasterEnabledStates[i] = graphicRaycasters[i].enabled;
                    }
                }
            }
            
            // Save the enabled state of each action BEFORE disabling them
            SaveActionsEnabledState();
            
            // Disable individual actions instead of the entire module
            DisableInputActions();
        }

        // Disable GraphicRaycasters to block mouse/touch clicks
        if (disableGraphicRaycaster && graphicRaycasters != null)
        {
            for (int i = 0; i < graphicRaycasters.Length; i++)
            {
                if (graphicRaycasters[i] != null)
                {
                    graphicRaycasters[i].enabled = false;
                }
            }
        }

        if (disableSendNavigationEvents && EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        Debug.Log($"[UILOCK] LOCK frame={Time.frameCount} " +
                  $"current={EventSystem.current?.currentSelectedGameObject?.name ?? "NULL"} " +
                  $"popupOpen={ConfirmPopup.IsAnyPopupOpen} " +
                  $"raycastersDisabled={disableGraphicRaycaster}");
    }

    public void Unlock(bool restoreSelection = true)
    {
        if (isUnlocking) return;
        
        isUnlocking = true;

        try
        {
            lockCount--;
            
            if (lockCount > 0)
                return;

            // Re-enable GraphicRaycasters
            if (disableGraphicRaycaster && graphicRaycasters != null)
            {
                for (int i = 0; i < graphicRaycasters.Length; i++)
                {
                    if (graphicRaycasters[i] != null && raycasterEnabledStates[i])
                    {
                        graphicRaycasters[i].enabled = true;
                    }
                }
            }

            // Re-enable actions based on their saved state
            RestoreInputActions();

            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = savedSendNavEvents;

                // Never restore popup buttons
                if (restoreSelection &&
                    savedSelected != null &&
                    savedSelected.activeInHierarchy &&
                    !ConfirmPopup.IsAnyPopupOpen)
                {
                    EventSystem.current.SetSelectedGameObject(savedSelected);
                }
            }
        }
        finally
        {
            isUnlocking = false;
        }

        Debug.Log($"[UILOCK] UNLOCK frame={Time.frameCount} " +
                $"restoreSelection={restoreSelection} " +
                $"lockCount={lockCount} " +
                $"raycastersRestored={disableGraphicRaycaster} " +
                $"savedSelected={(savedSelected ? savedSelected.name : "NULL")} " +
                $"current={EventSystem.current?.currentSelectedGameObject?.name ?? "NULL"}");
    }

    public void ForceUnlockAll(bool restoreSelection = true)
    {
        if (lockCount <= 0) 
        {
            // Even if not locked, ensure input is enabled
            EnsureInputActionsEnabled();
            
            // Also ensure GraphicRaycasters are enabled
            if (disableGraphicRaycaster && graphicRaycasters != null)
            {
                for (int i = 0; i < graphicRaycasters.Length; i++)
                {
                    if (graphicRaycasters[i] != null && !graphicRaycasters[i].enabled)
                    {
                        graphicRaycasters[i].enabled = true;
                    }
                }
            }
            return;
        }
        
        lockCount = 1;
        Unlock(restoreSelection);
    }

    public int GetLockCount() => lockCount;

    #region Input Action Handling
    
    private void SaveActionsEnabledState()
    {
        if (inputModule == null) return;
        
        // Get the actions from the input module using reflection
        wasPointEnabled = GetActionEnabled("pointAction");
        wasMoveEnabled = GetActionEnabled("moveAction");
        wasSubmitEnabled = GetActionEnabled("submitAction");
        wasCancelEnabled = GetActionEnabled("cancelAction");
        wasClickEnabled = GetActionEnabled("clickAction");
        wasScrollWheelEnabled = GetActionEnabled("scrollWheelAction");
        wasMiddleClickEnabled = GetActionEnabled("middleClickAction");
        wasRightClickEnabled = GetActionEnabled("rightClickAction");
        wasTrackedDevicePositionEnabled = GetActionEnabled("trackedDevicePositionAction");
        wasTrackedDeviceOrientationEnabled = GetActionEnabled("trackedDeviceOrientationAction");
    }
    
    private bool GetActionEnabled(string actionFieldName)
    {
        if (inputModule == null) return false;
        
        var field = inputModule.GetType().GetField(actionFieldName, 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic);
            
        if (field != null)
        {
            var actionRef = field.GetValue(inputModule) as InputActionReference;
            if (actionRef != null && actionRef.action != null)
            {
                return actionRef.action.enabled;
            }
        }
        
        return false;
    }
    
    private void SetActionEnabled(string actionFieldName, bool enabled)
    {
        if (inputModule == null) return;
        
        var field = inputModule.GetType().GetField(actionFieldName, 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic);
            
        if (field != null)
        {
            var actionRef = field.GetValue(inputModule) as InputActionReference;
            if (actionRef != null && actionRef.action != null)
            {
                if (enabled)
                    actionRef.action.Enable();
                else
                    actionRef.action.Disable();
            }
        }
    }
    
    private void DisableInputActions()
    {
        if (inputModule == null) return;
        
        // Disable all UI input actions individually
        SetActionEnabled("pointAction", false);
        SetActionEnabled("moveAction", false);
        SetActionEnabled("submitAction", false);
        SetActionEnabled("cancelAction", false);
        SetActionEnabled("clickAction", false);
        SetActionEnabled("scrollWheelAction", false);
        SetActionEnabled("middleClickAction", false);
        SetActionEnabled("rightClickAction", false);
        SetActionEnabled("trackedDevicePositionAction", false);
        SetActionEnabled("trackedDeviceOrientationAction", false);
    }
    
    private void RestoreInputActions()
    {
        if (inputModule == null) return;
        
        // Restore each action to its previous enabled state
        SetActionEnabled("pointAction", wasPointEnabled);
        SetActionEnabled("moveAction", wasMoveEnabled);
        SetActionEnabled("submitAction", wasSubmitEnabled);
        SetActionEnabled("cancelAction", wasCancelEnabled);
        SetActionEnabled("clickAction", wasClickEnabled);
        SetActionEnabled("scrollWheelAction", wasScrollWheelEnabled);
        SetActionEnabled("middleClickAction", wasMiddleClickEnabled);
        SetActionEnabled("rightClickAction", wasRightClickEnabled);
        SetActionEnabled("trackedDevicePositionAction", wasTrackedDevicePositionEnabled);
        SetActionEnabled("trackedDeviceOrientationAction", wasTrackedDeviceOrientationEnabled);
    }
    
    private void EnsureInputActionsEnabled()
    {
        if (inputModule == null) return;
        
        // Ensure all critical actions are enabled (fallback)
        SetActionEnabled("moveAction", true);
        SetActionEnabled("submitAction", true);
        SetActionEnabled("cancelAction", true);
        
        // Also ensure the module itself is enabled if it should be
        if (savedInputModuleEnabled && !inputModule.enabled)
            inputModule.enabled = true;
    }
    
    #endregion
}