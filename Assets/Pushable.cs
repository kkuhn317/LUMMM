using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pushable : MonoBehaviour
{

    ObjectPhysics physics;
    public float pushSpeed = 10;

    private bool playerTouching = false;

    // Start is called before the first frame update
    void Start()
    {
        physics = GetComponentInParent<ObjectPhysics>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            // Vector2 impulse = Vector2.zero;
            
            // int contactCount = col.contactCount;
            // for(int i = 0; i < contactCount; i++) {
            //     var contact = col.GetContact(i);
            //     impulse += contact.normal * contact.normalImpulse;
            //     impulse.x += contact.tangentImpulse * contact.normal.y;
            //     impulse.y -= contact.tangentImpulse * contact.normal.x;
            // }

            playerTouching = true;

            MarioMovement playerScript = col.gameObject.GetComponent<MarioMovement>();
            GameObject mario = col.gameObject;

            if (mario.transform.position.x < transform.position.x && playerScript.moveInput.x > 0)
            {
                physics.movingLeft = false;
                physics.velocity.x = pushSpeed;
                playerScript.StartPushing(pushSpeed);
            }
            else if (mario.transform.position.x > transform.position.x && playerScript.moveInput.x < 0)
            {
                physics.movingLeft = true;
                physics.velocity.x = pushSpeed;
                playerScript.StartPushing(pushSpeed);
            } else {
                physics.velocity.x = 0;
                playerScript.StopPushing();
            }

        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            playerTouching = false;
            physics.velocity.x = 0;
            col.gameObject.GetComponent<MarioMovement>().StopPushing();
        }
    }
}
