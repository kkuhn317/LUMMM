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

    private MarioMovement player1;

    private MarioMovement getPlayer1()
    {
        if (player1 == null || player1.gameObject == null)
        {
            player1 = GameManager.Instance.GetPlayer(0);
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

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(GlobalVariables.OnScreenControls);
    }

    // Update is called once per frame
    void Update()
    {
        
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
        getPlayer1().onMobileLeftPressed();
    }
    public void onLeftRelease()
    {
        getPlayer1().onMobileLeftReleased();
    }
    public void onRightPress()
    {
        getPlayer1().onMobileRightPressed();
    }
    public void onRightRelease()
    {
        getPlayer1().onMobileRightReleased();
    }
    public void onUpPressed()
    {
        getPlayer1().onMobileUpPressed();
        getPlayer1().onUsePressed();
    }
    public void onUpReleased()
    {
        getPlayer1().onMobileUpReleased();
    }
    public void onJumpPress()
    {
        getPlayer1().onJumpPressed();
    }
    public void onJumpRelease()
    {
        getPlayer1().onJumpReleased();
    }
    public void onRunPress()
    {
        getPlayer1().onRunPressed();
    }
    public void onRunRelease()
    {
        getPlayer1().onRunReleased();
    }
    public void onCrouchPressed()
    {
        getPlayer1().onMobileCrouchPressed();
    }
    public void onCrouchReleased()
    {
        getPlayer1().onMobileCrouchReleased();
    }
    public void onUsePressed()
    {
        //getPlayer1().onUsePressed();
        getPlayer1().onShootPressed();
    }
    public void onSpinPressed()
    {
        getPlayer1().onSpinPressed();
    }
    public void onSpinReleased()
    {
        getPlayer1().onSpinReleased();
    }
    public void onExtraPressed()
    {
        getPlayer1().onExtraActionPressed();
    }
}
