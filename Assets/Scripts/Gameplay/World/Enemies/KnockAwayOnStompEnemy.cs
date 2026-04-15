using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Example, the spike enemy on the spike bonzai bill
public class KnockAwayOnStompEnemy : EnemyAI
{
    protected override void hitByStomp(GameObject player)
    {
        MarioCore playerscript = player.GetComponent<MarioCore>();
        playerscript.StateMachine.ForceTransition(MarioStateID.Rise);
        KnockAway(false);
        // GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
        GameManager.Instance.GetSystem<ScoreSystem>().AddScore(100); // Gives a hundred points to the player
    }

    protected override void hitByGroundPound(MarioCore player)
    {
        KnockAway(false);
        // GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
        GameManager.Instance.GetSystem<ScoreSystem>().AddScore(100); // Gives a hundred points to the player
    }
}