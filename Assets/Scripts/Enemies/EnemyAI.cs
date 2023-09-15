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

    // Define a condition for visibility
    public bool IsVisible
    {
        get { return isVisible; }
    }

    private bool isVisible = false; // Flag to track if the enemy is visible to the camera.

    protected override void Start()
    {
        base.Start();
        enabled = false;
    }

    private void OnBecameVisible()
    {
        isVisible = true;
        enabled = true;
    }

    private void OnBecameInvisible()
    {
        isVisible = false;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        // don't do anything if we're already dead
        if (objectState == ObjectState.knockedAway || objectState == ObjectState.onLava) {
            return;
        }

        if (other.gameObject.CompareTag("Player")) {
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

        float playerHeightSubtract = player.GetComponent<Collider2D>().bounds.size.y / 2 * (playerscript.powerupState == MarioMovement.PowerupState.small ?  0.4f : 0.7f);

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

    public override void KnockAway(bool direction, bool sound = true)
    {
        base.KnockAway(direction, sound);
        releaseItem();
    }

    public void releaseItem() {
        // if the scene is closing, don't do anything
        if (gameObject.scene.isLoaded == false) {
            return;
        }

        if (heldItem != null) {
            // instantiate the item
            GameObject item = Instantiate(heldItem, transform.position + itemSpawnOffset, Quaternion.identity);
            heldItem = null;
        }
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        
        if (heldItem != null) {
            Gizmos.color = Color.red;
            // spawn position
            Gizmos.DrawSphere(transform.position + itemSpawnOffset, 0.1f);
        }

        // draw stomp height
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position + new Vector3(-width/2, stompHeight, 0), transform.position + new Vector3(width/2, stompHeight, 0));
    }

    void OnDestroy() {
        if (heldItem != null) {
            releaseItem();
        }
    }
}
