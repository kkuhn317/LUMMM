using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Example, the spike enemy on the spike bonzai bill
public class KnockAwayOnStompEnemy : EnemyAI
{
    protected override void hitByStomp(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        playerscript.Jump();
        KnockAway(false);
        GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
    }
}
