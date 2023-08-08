using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBill : EnemyAI
{
    bool facingleft = false;

    protected override void Start()
    {
        base.Start();

        // if angle is on the right side of the screen, moving left = false
        facingleft = transform.rotation.eulerAngles.z > 90 && transform.rotation.eulerAngles.z < 270;

        if (facingleft)
        {
            GetComponent<SpriteRenderer>().flipY = true;
        }
    }

    // do special movement for bullet bills
    protected override Vector3 HorizontalMovement(Vector3 vector)
    {
        if (objectState != ObjectState.falling)
        {
            return base.HorizontalMovement(vector);
        }

        float angle = transform.rotation.eulerAngles.z;

        // move forward in the angle we are facing (0 degrees means right)
        Vector2 movement = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        vector += adjDeltaTime * velocity.x * (Vector3)movement;

        return vector;
    }

    protected override Vector3 VerticalMovement(Vector3 vector)
    {
        
        if (objectState != ObjectState.falling)
        {
            return base.VerticalMovement(vector);
        }

        return vector;
    }

    // die on stomp
    protected override void hitByStomp(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        playerscript.Jump();

        KnockAway(facingleft, false);

        GetComponent<AudioSource>().Play();
        
        GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
    }

    // When we hit a wall, we should die
    protected override void OnTriggerEnter2D(Collider2D other)
    {

        // if layer is in wallmask
        if (wallMask == (wallMask | (1 << other.gameObject.layer)))
        {
            KnockAway(!facingleft);
        } else
        {
            base.OnTriggerEnter2D(other);
        }
    }


    // do nothing when we touch a wall
    protected override void onTouchWall(GameObject other){}




}
