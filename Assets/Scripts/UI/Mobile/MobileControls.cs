using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MobileControls : MonoBehaviour
{
    // TODO: Investigate setting input action values in the script (simulating input instead of directly calling the functions)
    // This would reduce code in MarioMovement.cs and allow MarioAbilities to to handle their own input
    // https://rene-damm.github.io/HowDoI.html#set-an-actions-value-programmatically
    // https://discussions.unity.com/t/trigger-set-value-of-inputaction-through-c-code/819388

    // Also something else I found, we might be able to use actionEvents property of the PlayerInput but by messing with it it doesnt seem that promising
    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.PlayerInput.html

    private MarioCore player1;
    private PlayerRegistry playerRegistry;
    private readonly Dictionary<MobileControlButton, bool> defaultButtonVisibility = new();
    private bool levelEnding;

    private void Awake()
    {
        GetMyButtons();
        foreach (MobileControlButton button in myButtons)
            defaultButtonVisibility[button] = button.gameObject.activeSelf;
    }

    private void OnEnable()
    {
        CheatFlags.OnAllAbilitiesChanged += OnAllAbilitiesChanged;
        GameEvents.OnLevelEnding += OnLevelEnding;
        RefreshAbilityButtons();
    }

    private void OnDisable()
    {
        CheatFlags.OnAllAbilitiesChanged -= OnAllAbilitiesChanged;
        GameEvents.OnLevelEnding -= OnLevelEnding;
        MobileHeldInputState.ResetTransient();
    }

    void Start()
    {
        CacheRegistry();
        gameObject.SetActive(GlobalVariables.OnScreenControls);
    }

    private void OnLevelEnding()
    {
        levelEnding = true;
        MobileHeldInputState.ResetTransient();
        SetTouchControlsEnabled(false);
    }

    private void OnAllAbilitiesChanged(bool enabled)
    {
        // X changes meaning between the normal layout and Ability Freak.
        // Release the action belonging to the old mode so toggling the cheat
        // while X is held cannot leave ShootPressed or SpinHeld latched.
        MarioCore player = getPlayer1();
        if (enabled)
            player?.Input?.OnShootReleased();
        else
            player?.Input?.OnSpinReleased();

        RefreshAbilityButtons();
    }

    /// <summary>
    /// Ability Freak exposes every authored touch button. When it is disabled,
    /// restore the visibility configured by the current level/prefab instance.
    /// </summary>
    private void RefreshAbilityButtons()
    {
        if (levelEnding) return;

        GetMyButtons();
        foreach (MobileControlButton button in myButtons)
        {
            bool visible = CheatFlags.AllAbilities
                || (defaultButtonVisibility.TryGetValue(button, out bool wasVisible) && wasVisible);
            button.gameObject.SetActive(visible);
        }

        if (CheatFlags.AllAbilities)
            UpdateButtonPosScaleOpacity();
    }

    public void SetTouchControlsEnabled(bool enabled)
    {
        GetMyButtons();
        foreach (MobileControlButton button in myButtons)
            button.gameObject.SetActive(enabled);
    }
    
    private void CacheRegistry()
    {
        if (GameManager.Instance != null)
            playerRegistry = GameManager.Instance.GetSystem<PlayerRegistry>();

        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>(true);
    }

    private void CachePlayer1()
    {
        if (playerRegistry == null) CacheRegistry();
        player1 = playerRegistry != null ? playerRegistry.GetPlayer(0) : null;
    }

    private MarioCore getPlayer1()
    {
        /*if (player1 == null || player1.gameObject == null)
        {
            player1 = GameManager.Instance.GetPlayer(0);
        }
        return player1;*/
        
        if (player1 == null || player1.gameObject == null)
        {
            CachePlayer1();
        }
        return player1;
    }

    private List<MobileControlButton> myButtons = new List<MobileControlButton>();

    private void GetMyButtons()
    {
        if (myButtons.Count > 0) return;
        foreach (MobileControlButton button in GetComponentsInChildren<MobileControlButton>(includeInactive: true))
        {
            myButtons.Add(button);
        }
    }

    public void UpdateButtonPosScaleOpacity() {
        GetMyButtons();
        foreach (MobileControlButton button in myButtons)
        {
            button.UpdatePosScaleOpacity();
        }
    }

    public void UpdateButtonOpacity(float buttonPressedOpacity, float buttonUnpressedOpacity) {
        GetMyButtons();
        foreach (MobileControlButton button in myButtons)
        {
            button.UpdateButtonOpacity(buttonPressedOpacity, buttonUnpressedOpacity);
        }
    }

    public void onLeftPress()
    {
        getPlayer1().Input.OnMobileLeftPressed();
    }

    public void onLeftRelease()
    {
        getPlayer1().Input.OnMobileLeftReleased();
    }

    public void onRightPress()
    {
        getPlayer1().Input.OnMobileRightPressed();
    }

    public void onRightRelease()
    {
        getPlayer1().Input.OnMobileRightReleased();
    }

    public void onUpPressed()
    {
        getPlayer1().Input.OnMobileUpPressed();
        getPlayer1().Input.OnUsePressed();
    }

    public void onUpReleased()
    {
        getPlayer1().Input.OnMobileUpReleased();
    }

   public void onDownPressed()
    {
        getPlayer1().Input.OnMobileDownPressed();
    }

    public void onDownReleased()
    {
        getPlayer1().Input.OnMobileDownReleased();
    }

    public void onJumpPress()
    {
        MobileHeldInputState.Jump = true;
        getPlayer1().Input.OnJumpPressed();
    }

    public void onJumpRelease()
    {
        MobileHeldInputState.Jump = false;
        getPlayer1().Input.OnJumpReleased();
    }

    public void onRunPress()
    {
        getPlayer1().Input.OnRunPressed();
    }

    public void onRunRelease()
    {
        getPlayer1().Input.OnRunReleased();
    }

    public void onUsePressed()
    {
        MobileHeldInputState.Use = true;
        MarioCore player = getPlayer1();
        if (player == null) return;

        // Preserve the established Use/X contract for normal levels. Ability
        // Freak temporarily routes it to Spin so spin jump and twirl are
        // accessible; FirePower reacts through the spin ability path.
        if (CheatFlags.AllAbilities)
            player.Input.OnSpinPressed();
        else
            player.Input.OnShootPressed();
    }

    public void onUseReleased()
    {
        MobileHeldInputState.Use = false;
        MarioCore player = getPlayer1();
        if (player == null) return;

        if (CheatFlags.AllAbilities)
            player.Input.OnSpinReleased();
        else
            player.Input.OnShootReleased();
    }

    public void onSpinPressed()
    {
        MobileHeldInputState.Spin = true;
        getPlayer1().Input.OnSpinPressed();
    }

    public void onSpinReleased()
    {
        MobileHeldInputState.Spin = false;
        getPlayer1().Input.OnSpinReleased();
    }

    public void onExtraPressed()
    {
        getPlayer1().Input.OnExtraActionPressed();
    }
}
