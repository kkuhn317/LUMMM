using UnityEngine;

// For simple objects that damage Mario while having a custom player death
// DO NOT USE DAMAGING TAG ON THE OBJECT
// Example: Firebar fireballs
public class DamagingCustomDeath : MonoBehaviour
{
    public DeathCause customDeath;

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled) return;

        // MarioCore is on the ROOT, not the child collider (Body_Collider)
        var playerscript = other.GetComponent<MarioCore>()
                        ?? other.GetComponentInParent<MarioCore>();
        if (playerscript == null) return;

        if (PowerStates.IsSmall(playerscript.State.PowerupState) && customDeath != null)
            playerscript.Combat.ToDead(customDeath);
        else
            playerscript.Combat.DamageMario();
    }
}