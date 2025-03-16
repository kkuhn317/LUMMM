using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For simple objects that damage Mario while having a custom player death
// DO NOT USE DAMAGING TAG ON THE OBJECT
// Example: Firebar fireballs
// TODO: Figure out if there's a better way to organize all this behavior
public class DamagingCustomDeath : MonoBehaviour
{
    public GameObject customDeath;


    protected virtual void OnTriggerEnter2D(Collider2D other) {
        // if we are enabled
        if (enabled == false) {
            return;
        }
        
        MarioMovement playerscript = other.GetComponent<MarioMovement>();

        if (playerscript == null) {
            return;
        }

        if(PowerStates.IsSmall(playerscript.powerupState) && customDeath != null) {
            playerscript.TransformIntoObject(customDeath);
        } else {
            playerscript.damageMario();
        }
    }
}
