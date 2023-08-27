using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBill : EnemyAI
{

    protected override void Update()
    {
        base.Update();

        // rotate to movement
        if (objectState != ObjectState.knockedAway) {
            float angle = Mathf.Atan2(realVelocity.y, realVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle + 180, Vector3.forward);
        }

        GetComponent<SpriteRenderer>().flipY = !movingLeft;
    }

    // knock away on stomp
    // TODO: make rotating knock away mode
    protected override void hitByStomp(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        playerscript.Jump();

        KnockAway(movingLeft, false);

        GetComponent<AudioSource>().Play();
        
        GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
    }

    // When we hit a wall, we should die
    protected override void onTouchWall(GameObject other){
        KnockAway(!movingLeft);
    }

    public override void Land() {
        KnockAway(!movingLeft);
    }

}
