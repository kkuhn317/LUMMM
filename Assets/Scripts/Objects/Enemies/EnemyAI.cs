using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using PowerupState = PowerStates.PowerupState;

public class EnemyAI : ObjectPhysics
{
    [Header("Enemy AI")]
    public bool canBeFireballed = true;
    public float stompHeight = 0.2f;
    public GameObject heldItem;
    public Vector3 itemSpawnOffset = new Vector3(0, 0, 0);
    public GameObject customDeath;
    [HideInInspector] public UnityEvent<GameObject> onPlayerDamaged;

    public enum SpinJumpEffect {
        bounceOff,
        poof,
        stomp,
    }
    public SpinJumpEffect spinJumpEffect = SpinJumpEffect.bounceOff;

    // Define a condition for visibility
    public bool IsVisible
    {
        get { return isVisible; }
    }

    protected bool isVisible = false; // Flag to track if the enemy is visible to the camera.
    protected bool appeared = false; // Flag to track if the enemy has appeared on screen yet.

    protected override void Start()
    {
        base.Start();

        enabled = false; // Disable the enemy by default until it becomes visible.

        // Fix for editor bug where OnBecameVisible is not called on startup sometimes
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            
            if (renderer.isVisible)
            {
                OnBecameVisible();
            }
        }
    }

    public virtual void OnBecameVisible()
    {
        isVisible = true;

        if (!appeared) {
            enabled = true;
            appeared = true;
        }
    }

    public virtual void OnBecameInvisible()
    {
        isVisible = false;

        // once the knocked away object is off screen, destroy it
        if (objectState == ObjectState.knockedAway)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        // if we are enabled
        if (enabled == false) {
            return;
        }
        
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

        float playerHeightSubtract = player.GetComponent<Collider2D>().bounds.size.y / 2 * (PowerStates.IsSmall(playerscript.powerupState) ?  0.4f : 0.7f);

        if (rb.position.y - playerHeightSubtract > transform.position.y + stompHeight) {
            if (playerscript.spinning) {
                hitBySpinJump(playerscript);
            } else if (playerscript.groundPounding) {
                hitByGroundPound(playerscript);
            } else {
                hitByStomp(player);
            }
        } else {
            hitOnSide(player);
        }

    }

    protected virtual void hitByStarPower(GameObject player) {
        KnockAway(player.transform.position.x > transform.position.x);
        GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
    }

    protected virtual void hitByStomp(GameObject player) {
        // default behavior is to not die, and to instead damage mario (like a spiny)
        hitOnSide(player);
    }

    protected virtual void hitBySpinJump(MarioMovement player) {
        switch (spinJumpEffect) {
            case SpinJumpEffect.bounceOff:
                player.SpinJumpBounce(gameObject);
                break;
            case SpinJumpEffect.poof:
                player.SpinJumpPoof(gameObject);
                break;
            case SpinJumpEffect.stomp:
                hitByStomp(player.gameObject);
                break;
        }
    }

    protected virtual void hitByGroundPound(MarioMovement player) {
        // Default behavior is to act like a spiny (damage mario)
        // Override to knock away or stomp like normal
        hitOnSide(player.gameObject);
    }

    protected virtual void hitOnSide(GameObject player) {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();

        if(PowerStates.IsSmall(playerscript.powerupState) && customDeath != null) {
            playerscript.TransformIntoObject(customDeath);
            Debug.Log($"Is player small? {PowerStates.IsSmall(playerscript.powerupState)} | Custom Death: {customDeath.name}");
        } else {
            // usually mario would be damaged here
            playerscript.damageMario();
            onPlayerDamaged.Invoke(player);
        }
    }

    protected virtual void touchNonPlayer(GameObject other) {
        // override this for whatever needed
    }

    // This is called by the cape when it hits the enemy (direction is)
    public virtual void OnCapeAttack(bool hitFromLeft) {
        //KnockAway(!hitFromLeft);  // Knock away option

        // Flip the enemy the opposite direction of the hit
        if (hitFromLeft == movingLeft) {
            Flip();
        }
    }

    public override void KnockAway(bool direction, bool sound = true, KnockAwayType? type = null, Vector2? velocity = null)
    {
        base.KnockAway(direction, sound, type, velocity);
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
