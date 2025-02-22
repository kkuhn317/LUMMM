using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bobomb : EnemyAI
{
    public enum EnemyState {
        walking,
        primed
    }

    public EnemyState state = EnemyState.walking;

    public Vector2 kickForce;

    private Animator animator;

    private AudioSource audioSource;

    public float explodeTime = 4f;

    public float explodeRadius = 3f;
    public GameObject explosionObject;

    private bool internalCarryable = false; // so you can say whether it's carryable in the editor (but it still gets managed by this script)
    public bool isGrabbing { get; private set; } = false;

    void Awake()
    {
        internalCarryable = carryable;
        carryable = false;

        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        switch(state) {
            case EnemyState.walking:
                ToWalking();
                break;
            case EnemyState.primed:
                ToPrimed();
                break;
        }
    }

    protected override void Update()
    {
        base.Update();

        // if we're walking or not moving, make objects act as walls
        // otherwise, don't so that we can knock them away (similar to koopa)
        if (state == EnemyState.walking || velocity.x == 0 || velocity.y == 0) {
            checkObjectCollision = true;
        } else {
            checkObjectCollision = false;
        }
    }

    protected override void touchNonPlayer(GameObject other)
    {
        if (other.gameObject.tag == "Projectile") {
            GameObject ball = other.gameObject;
            if (ball.GetComponent<Fireball>()) {
                switch (state) {
                    case EnemyState.walking:
                        ToPrimed();
                        kickBomb(ball.GetComponent<ObjectPhysics>().movingLeft);
                        break;
                    case EnemyState.primed:
                        kickBomb(ball.GetComponent<ObjectPhysics>().movingLeft);
                        break;
                }
            }
        } else if (other.CompareTag("Enemy") && state == EnemyState.primed && (velocity.x != 0 || velocity.y != 0)) {
            other.GetComponent<EnemyAI>().KnockAway(movingLeft);
        }
    }

    private void ToWalking() {
        state = EnemyState.walking;
    }

    private void ToPrimed() {
        if (state != EnemyState.primed) {
            state = EnemyState.primed;
            animator.SetTrigger("prime");
            if (internalCarryable) {
                carryable = true;
            }
            stopAfterLand = true;
            bounceOffWalls = false;
            Invoke("explode", explodeTime);
        }
        
        velocity = new Vector2(0, velocity.y);
    }


    protected override void hitByStomp(GameObject player) {
        MarioMovement playerScript = player.GetComponent<MarioMovement>();
        switch (state) {
            case EnemyState.walking:
                playerScript.Jump();
                GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
                audioSource.Play();
                ToPrimed();
                break;
            case EnemyState.primed:
                kickBomb(player.transform.position.x > transform.position.x);
                break;
        }
    }

    protected override void hitOnSide(GameObject player) {
        MarioMovement playerScript = player.GetComponent<MarioMovement>();
        switch (state) {
            case EnemyState.walking:
                playerScript.damageMario();
                break;
            case EnemyState.primed:
                kickBomb(player.transform.position.x > transform.position.x);
                break;
        }
    }

    private void kickBomb(bool direction) {
        audioSource.PlayOneShot(knockAwaySound);
        objectState = ObjectState.falling;
        movingLeft = direction;
        velocity = new Vector2(kickForce.x, kickForce.y);
    }

    private void explode() {

        if (carried) {
            escapeMario();
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explodeRadius);
        foreach (var hitCollider in hitColliders)
        {
            // just a reminder that sendmessage exists
            //hitCollider.SendMessage("AddDamage");

            if (hitCollider.gameObject.GetComponent<BreakableBlocks>()) {
                hitCollider.gameObject.GetComponent<BreakableBlocks>().Break();
                continue;
            }

            switch (hitCollider.gameObject.tag) {
                case "Player":
                    hitCollider.gameObject.GetComponent<MarioMovement>().damageMario();
                    break;
                case "Enemy":
                    hitCollider.gameObject.GetComponent<EnemyAI>().KnockAway(transform.position.x > hitCollider.transform.position.x);
                    break;
            }

        }
        Instantiate(explosionObject, transform.position, Quaternion.identity);
        Destroy(gameObject);

    }

}
