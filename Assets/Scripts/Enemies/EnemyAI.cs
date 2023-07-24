using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyAI : ObjectPhysics
{
    [Header("Enemy AI")]
    public bool canBeFireballed = true;

    public float stompHeight = 0.2f;

    public GameObject heldItem;
    public Vector3 itemSpawnOffset = new Vector3(0, 0, 0);

    protected override void Start() {
        base.Start();
        enabled = false;
    }

    void OnBecameVisible() {
        
        enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.tag == "Player" && objectState != ObjectState.knockedAway) {
            hitByPlayer(other.gameObject);
        } else {
            touchNonPlayer(other.gameObject);
        }
    }

    protected virtual void hitByPlayer(GameObject player) {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        if (playerscript.starPower) {
            hitByStarPower(player);
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        float playerHeightSubtract = playerscript.powerupState == MarioMovement.PowerupState.small ? 0.2f : 0.7f;

        if (rb.position.y - playerHeightSubtract > transform.position.y + stompHeight) {
            hitByStomp(player);
        } else {
            hitOnSide(player);
        }

    }

    protected virtual void hitByStarPower(GameObject player) {
        KnockAway(player.transform.position.x > transform.position.x);
    }

    protected virtual void hitByStomp(GameObject player) {
        // default behavior is to not die, and to instead damage mario (like a spiny)
        hitOnSide(player);
    }

    protected virtual void hitOnSide(GameObject player) {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        // usually mario would be damaged here
        playerscript.damageMario();
    }

    protected virtual void touchNonPlayer(GameObject other) {
        // override this for whatever needed
    }

    public override void KnockAway(bool direction)
    {
        base.KnockAway(direction);
        releaseItem();
    }

    public void releaseItem() {
        if (heldItem != null) {
            // instantiate the item
            GameObject item = Instantiate(heldItem, transform.position + itemSpawnOffset, Quaternion.identity);
            heldItem = null;
        }
    }

    private void OnDrawGizmos() {
        if (heldItem != null) {
            Gizmos.color = Color.red;
            // spawn position
            Gizmos.DrawSphere(transform.position + itemSpawnOffset, 0.1f);
        }
    }
}
