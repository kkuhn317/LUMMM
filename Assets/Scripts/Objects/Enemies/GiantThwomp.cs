using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantThwomp : EnemyAI
{
    [Header("Giant Thwomp")]
    public SpriteRenderer front;
    public Sprite idleSprite;
    public Sprite angrySprite;

    public enum ThwompStates {
        Idle, // The Thwomp remains stationary, waiting for the player to come into range
        Falling, // The Thwomp falls rapidly towards the player or a specific target area
        Stunned, // After hitting the ground, the Thwomp might remain in a stunned state for a short period, unable to move
        Rising, // The Thwomp rises back to its original position after completing its fall and stun duration
        DetectPlayerLeft, // The Thwomp moves left after being stunned or rising if detects a player
        DetectPlayerRight, // The Thwomp moves right after being stunned or rising if detects a player
        DetectPlayerAbove, // The Thwomp detects the player above and rises (player has different death when spinning)
        Vulnerable, // The Thwomp becomes vulnerable after being hit by Mario's cape, remaining in this state for a certain period
    }
    private ThwompStates currentState;

    protected override void Start()
    {
        base.Start();
    }

    private void ChangeState(ThwompStates newState)
    {
        currentState = newState;
    }
}
