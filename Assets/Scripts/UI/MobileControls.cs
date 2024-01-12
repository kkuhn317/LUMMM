using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileControls : MonoBehaviour
{

    private MarioMovement player1;

    private MarioMovement getPlayer1()
    {
        if (player1 == null)
        {
            player1 = GameManager.Instance.GetPlayer(0);
        }
        return player1;
    }

    // Start is called before the first frame update
    void Start()
    {
        //print(GlobalVariables.OnScreenControls);
        gameObject.SetActive(GlobalVariables.OnScreenControls);
    }

    // Update is called once per frame
    void Update()
    {
        
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
        getPlayer1().onUsePressed();
        getPlayer1().onShootPressed();
    }


}
