using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrushDetection : MonoBehaviour
{
    // Assign a custom crush death GameObject in the Inspector if desired
    public GameObject customCrushDeath; // Custom object to transform into when crushed

    // This is used to detect when the player is crushed by a block
    // It should be placed on a child object of the player, with a small box collider that can normally not be collided with
    // See coin door maze for an example
    void CheckCrush(GameObject col) {
        if (col.CompareTag("Crushing"))
        {
            // Get the MarioMovement component in the parent object
            MarioMovement mario = GetComponentInParent<MarioMovement>();

            // Apply custom crush death if available
            if (customCrushDeath != null)
            {
                mario.TransformIntoObject(customCrushDeath);
            }
            else
            {
                mario.damageMario(force: true);
            }
        }
    }

    void FixedUpdate()
    {
        // Move the crush detection collider inside the parent's box collider, so it doesn't stick out when you crouch
        BoxCollider2D parentBox = transform.parent.GetComponent<BoxCollider2D>();
        if (parentBox != null)
        {
            transform.localPosition = new Vector3(parentBox.offset.x, parentBox.offset.y, transform.localPosition.z);
        }
    }


    void OnCollisionEnter2D(Collision2D col)
    {
        CheckCrush(col.gameObject);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        CheckCrush(col.gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        CheckCrush(col.gameObject);
    }

}
