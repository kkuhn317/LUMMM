using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBill : EnemyAI
{
    [Header("Bullet Bill")]

    public bool rotateToMovement = true; // if true, rotate to movement direction (like a bullet bill)
    // This should be off for cannon balls

    protected override void Start()
    {
        base.Start();

        if (rotateToMovement) {
            RotateToMovement();
        }
    }

    protected override void Update()
    {
        base.Update();

        // rotate to movement
        if ((objectState != ObjectState.knockedAway) && rotateToMovement) {
            RotateToMovement();
        }

        
    }

    protected virtual void RotateToMovement() {
        float angle = Mathf.Atan2(realVelocity.y, realVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle + 180, Vector3.forward);
        GetComponentInChildren<SpriteRenderer>().flipY = !movingLeft;
    }

    // knock away on stomp
    protected override void hitByStomp(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        playerscript.Jump();

        KnockAway(movingLeft, false, KnockAwayType.flip, new Vector2(1,0));

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
