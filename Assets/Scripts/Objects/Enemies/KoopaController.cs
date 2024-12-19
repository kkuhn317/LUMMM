using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KoopaController : EnemyAI
{
    public enum EnemyState {
        walking,
        inShell,
        movingShell
    }

    public EnemyState state = EnemyState.walking;
    public float walkingSpeed = 2;
    public float movingShellSpeed = 10;

    public float hitCooldown = 0.5f; // after mario touches the enemy, how long until it can be hit again
    public float hitCooldownTimer = 0;
    private Animator animator;

    private AudioSource audioSource;

    private bool dontFallOffLedgesInternal = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        dontFallOffLedgesInternal = DontFallOffLedges;

        switch(state) {
            case EnemyState.walking:
                ToWalking();
                break;
            case EnemyState.inShell:
                ToInShell();
                break;
            case EnemyState.movingShell:
                ToMovingShell(movingLeft);
                break;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (hitCooldownTimer > 0) {
            hitCooldownTimer -= Time.deltaTime;
        }
    }

    protected override void touchNonPlayer(GameObject other)
    {
        if (other.CompareTag("Enemy") && state == EnemyState.movingShell) {
            other.GetComponent<EnemyAI>().KnockAway(movingLeft);
            GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
        }
    }

    // TODO: Figure out if this ever used or if touchNonPlayer is always used instead
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state == EnemyState.movingShell)
        {
            if (collision.gameObject.CompareTag("Enemy"))
            {
                EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
                enemy.KnockAway(movingLeft);
                GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
            }
        }
    }

    private void ToWalking() {
        state = EnemyState.walking;
        animator.SetBool("inShell", false);
        velocity = new Vector2(walkingSpeed, velocity.y);
        checkObjectCollision = true;
        if (dontFallOffLedgesInternal) {
            DontFallOffLedges = true;
        }
    }

    private void ToInShell() {
        state = EnemyState.inShell;
        animator.SetBool("inShell", true);
        velocity = new Vector2(0, velocity.y);
        wallRaycastSpacing = 0.65f;
        floorRaycastSpacing = 0.5f;
        checkObjectCollision = true;
        DontFallOffLedges = false;
    }

    private void ToMovingShell(bool direction) {
        state = EnemyState.movingShell;
        movingLeft = direction;
        animator.SetBool("inShell", true);
        velocity = new Vector2(movingShellSpeed, velocity.y);
        wallRaycastSpacing = 0.65f;
        floorRaycastSpacing = 0.5f;
        checkObjectCollision = false;
        DontFallOffLedges = false;
    }

    private bool HitCooldownCheck() {
        if (hitCooldownTimer > 0) {
            return false;
        }
        hitCooldownTimer = hitCooldown;
        return true;
    }

    protected override void hitByStomp(GameObject player) {
        if (!HitCooldownCheck()) {
            return;
        }
        hitCooldownTimer = hitCooldown;
        MarioMovement playerScript = player.GetComponent<MarioMovement>();
        switch (state) {
            case EnemyState.walking:
                playerScript.Jump();
                audioSource.Play();
                ToInShell();
                break;
            case EnemyState.inShell:
                audioSource.PlayOneShot(knockAwaySound);
                ToMovingShell(player.transform.position.x > transform.position.x);
                break;
            case EnemyState.movingShell:
                playerScript.Jump();
                audioSource.Play();
                ToInShell();
                break;
        }

    }

    protected override void hitByGroundPound(MarioMovement player)
    {
        KnockAway(false);
    }

    protected override void hitOnSide(GameObject player) {
        if (!HitCooldownCheck()) {
            return;
        }
        MarioMovement playerScript = player.GetComponent<MarioMovement>();
        switch (state) {
            case EnemyState.walking:
                playerScript.damageMario();
                break;
            case EnemyState.inShell:
                audioSource.PlayOneShot(knockAwaySound);
                ToMovingShell(player.transform.position.x > transform.position.x);
                break;
            case EnemyState.movingShell:
                playerScript.damageMario();
                QuestionBlock questionBlock = player.GetComponent<QuestionBlock>();
                if (questionBlock != null)
                {
                    questionBlock.QuestionBlockBounce();
                }
                break;
        }  
    }

    protected override void onTouchWall(GameObject other)
    {
        base.onTouchWall(other);
        
        if (state == EnemyState.movingShell) {
            // if the wall is a question block, hit it
            if (other.GetComponent<QuestionBlock>()) {
                other.GetComponent<QuestionBlock>().Activate();
            }
        }
    }

    public void StopShell()
    {
        if (state == EnemyState.movingShell)
        {
            ToInShell();
        }
    }
}