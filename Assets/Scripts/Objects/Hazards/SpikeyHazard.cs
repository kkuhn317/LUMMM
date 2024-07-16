using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// For hazards like sawblades that let you spin jump off of them but that's it
public class SpikeyHazard : MonoBehaviour
{
    public float stompHeight = 0.2f;

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player")) {
            hitByPlayer(other.gameObject);
        }
    }

    protected virtual void hitByPlayer(GameObject player) {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        MarioMovement playerscript = player.GetComponent<MarioMovement>();

        float playerHeightSubtract = player.GetComponent<Collider2D>().bounds.size.y / 2 * (PowerStates.IsSmall(playerscript.powerupState) ?  0.4f : 0.7f);

        if (rb.position.y - playerHeightSubtract > transform.position.y + stompHeight) {
            if (playerscript.spinning) {
                playerscript.SpinJumpBounce(gameObject);
            } else {
                playerscript.damageMario();
            }
        } else {
            playerscript.damageMario();
        }

    }

    protected virtual void OnDrawGizmosSelected() {
        // draw stomp height
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position + new Vector3(-1, stompHeight, 0), transform.position + new Vector3(1, stompHeight, 0));
    }


}
