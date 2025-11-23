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
            AwardShellComboReward();
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
                AwardShellComboReward();
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

        if (StompComboManager.Instance != null)
        {
            StompComboManager.Instance.shellChainActive = true;
        }
    }

    private bool HitCooldownCheck() {
        if (hitCooldownTimer > 0) {
            return false;
        }
        hitCooldownTimer = hitCooldown;
        return true;
    }

    private void KickShell(MarioMovement playerScript, Transform playerTransform)
    {
        audioSource.PlayOneShot(knockAwaySound);
        ToMovingShell(playerTransform.position.x > transform.position.x);

        int kickPoints = 400; // Default kick points

        if (StompComboManager.Instance != null)
        {
            int last = StompComboManager.Instance.LastStompScore;

            // If you come from higher stomp rewards, increase the kick
            if (last >= 400 && last < 800)
                kickPoints = 500;
            else if (last >= 800)
                kickPoints = 800;

            // Start shell sequence from 0 and mark the shell chain as active
            StompComboManager.Instance.ResetShellCombo();
            StompComboManager.Instance.shellChainActive = true;
        }

        // Give the kick reward (400 / 500 / 800) and popup
        AwardFlatScoreReward(kickPoints);
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
                AwardStompComboReward();
                break;
            case EnemyState.inShell:
                KickShell(playerScript, player.transform);
                break;
            case EnemyState.movingShell:
                playerScript.Jump();
                audioSource.Play();
                ToInShell();
                AwardStompComboReward();

                if (StompComboManager.Instance != null)
                {
                    StompComboManager.Instance.shellChainActive = false;
                    StompComboManager.Instance.ResetShellCombo();
                }
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
                KickShell(playerScript, player.transform);
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
            if (StompComboManager.Instance != null)
            {
                StompComboManager.Instance.shellChainActive = false;
                StompComboManager.Instance.ResetShellCombo();
            }
        }
    }

    public override void KnockAway(bool direction, bool sound = true, KnockAwayType? type = null, Vector2? velocity = null)
    {
        if (type.HasValue && type.Value == KnockAwayType.animation)
        {
            // Skip applying the movingLeft direction if the type is animation
            base.KnockAway(false, sound, type, velocity); // You might want to use "false" or some default direction here.
        }
        else
        {
            base.KnockAway(direction, sound, type, velocity); // Default KnockAway behavior
        }
    }
}