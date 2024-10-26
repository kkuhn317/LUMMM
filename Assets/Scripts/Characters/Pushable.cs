using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pushable : MonoBehaviour
{
    ObjectPhysics physics;
    public float pushSpeed = 10;
    private MarioMovement playerScript;

    // Start is called before the first frame update
    void Start()
    {
        physics = GetComponentInParent<ObjectPhysics>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (playerScript == null)
        {
            return;
        }
        
        //print("pushing: " + playerScript.moveInput.x);

        GameObject mario = playerScript.gameObject;
        Rigidbody2D marioRb = mario.GetComponent<Rigidbody2D>();

        if (mario.transform.position.x < transform.position.x && playerScript.moveInput.x > 0 && marioRb.velocity.x >= 0)
        {
            physics.movingLeft = false;
            physics.velocity.x = pushSpeed;
            playerScript.FlipTo(true);
            playerScript.StartPushing(pushSpeed);
        }
        else if (mario.transform.position.x > transform.position.x && playerScript.moveInput.x < 0 && marioRb.velocity.x <= 0)
        {
            physics.movingLeft = true;
            physics.velocity.x = pushSpeed;
            playerScript.FlipTo(false);
            playerScript.StartPushing(pushSpeed);
        } else {
            physics.velocity.x = 0;
            playerScript.StopPushing();
        }
    }

    public void StopPushing()
    {
        print("stop pushing");
        // use to stop pushing until mario enters again
        physics.velocity.x = 0;
        if (playerScript != null)
        {
            playerScript.StopPushing();
            playerScript = null;
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            if (playerScript != null)
            {
                playerScript.StopPushing();
                playerScript = null;
                physics.velocity.x = 0;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        print("something entered: " + col.gameObject.name);
        if (col.gameObject.CompareTag("Player"))
        {
            print("Start pushing!");
            playerScript = col.gameObject.GetComponent<MarioMovement>();
        }
    }
}
