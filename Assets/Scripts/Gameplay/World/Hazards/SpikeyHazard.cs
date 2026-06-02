using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// For hazards like sawblades that let you spin jump off of them but that's it
public class SpikeyHazard : MonoBehaviour
{
    public float stompHeight = 0.2f;

    [HideInInspector] public UnityEvent<GameObject> onPlayerDamaged;

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
            hitByPlayer(other.gameObject);
    }

    protected virtual void hitByPlayer(GameObject player)
    {
        // MarioCore, Rigidbody2D and the main Collider2D all live on the ROOT,
        // not on the child Body_Collider that fires the trigger.
        MarioCore playerscript = player.GetComponent<MarioCore>()
                              ?? player.GetComponentInParent<MarioCore>();
        if (playerscript == null) return;

        Rigidbody2D rb     = playerscript.Rb;
        Collider2D  col    = playerscript.Collider;

        float playerHeightSubtract = col.bounds.size.y / 2f
            * (PowerStates.IsSmall(playerscript.State.PowerupState) ? 0.4f : 0.7f);

        if (rb.position.y - playerHeightSubtract > transform.position.y + stompHeight)
        {
            if (playerscript.State.Spinning)
            {
                MarioEvents.FireSpinJumpBounced(playerscript.PlayerIndex);
                playerscript.State.IsBounced = true;
                playerscript.StateMachine.ForceTransition(MarioStateID.Rise);
            }
            else
            {
                playerscript.Combat.DamageMario();
                onPlayerDamaged.Invoke(playerscript.gameObject);
            }
        }
        else
        {
            playerscript.Combat.DamageMario();
            onPlayerDamaged.Invoke(playerscript.gameObject);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(
            transform.position + new Vector3(-1, stompHeight, 0),
            transform.position + new Vector3( 1, stompHeight, 0));
    }
}