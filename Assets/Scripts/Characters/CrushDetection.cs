using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrushDetection : MonoBehaviour
{
    // This is used to detect when the player is crushed by a block
    // It should be placed on a child object of the player, with a small box collider that can normally not be collided with
    // See coin door maze for an example
    void OnCollisionEnter2D (Collision2D col)
    {
        // get the mariomovement in the parent object
        MarioMovement mario = GetComponentInParent<MarioMovement>();
        mario.damageMario(force: true);
    }
}