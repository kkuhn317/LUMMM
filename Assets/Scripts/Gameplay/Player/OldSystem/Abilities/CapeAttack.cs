using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class CapeAttack : MarioAbility
{
    [Header("Audio")]
    public AudioClip marioPrepareAttackSound;
    public AudioClip capeSound;

    [Header("Cape Settings")]
    public bool canCape = true;
    public float capeCooldown = 0.75f;
    public float capeAttackDelay = 0.3f; // Time after button press until attack is executed (should line up with animation)
    
    private AudioSource audioSource;
    private Animator animator;
    private Rigidbody2D marioRb;
    private int enemyLayerMask;

    private Coroutine capeDelayRoutine;
    private Coroutine capeCooldownRoutine;

    private void EnsureCachedComponents()
    {
        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (!animator && marioMovement)
            animator = marioMovement.GetComponent<Animator>();

        if (!marioRb && marioMovement)
            marioRb = marioMovement.GetComponent<Rigidbody2D>();

        if (enemyLayerMask == 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    public override void onExtraActionPressed()
    {
        EnsureCachedComponents();

        // Prevent cape attack while ground pounding, spinning, or wall sliding
        if (!canCape || marioMovement.groundPounding || marioMovement.spinning || marioMovement.wallSliding || marioMovement.IsMidairSpinning)
            return;


        marioMovement.isCapeActive = true; // Set cape attack active
        animator.SetBool("cape", true);

        // If in the air, allow flipping
        if (!marioMovement.onGround &&
            marioMovement.moveInput.x != 0 &&
            marioMovement.facingRight != (marioMovement.moveInput.x > 0))
        {
            marioMovement.FlipTo(marioMovement.moveInput.x > 0);
        }

        audioSource.PlayOneShot(marioPrepareAttackSound);
        isBlockingJump = true;
        canCape = false;

        capeDelayRoutine = StartCoroutine(CapeAttackDelay());
        capeCooldownRoutine = StartCoroutine(CapeCooldown());
    }

    private IEnumerator CapeAttackDelay()
    {
        EnsureCachedComponents();

        yield return new WaitForSeconds(capeAttackDelay);

        audioSource.PlayOneShot(capeSound);

        // Perform cape attack logic (hit enemies in front)
        Vector2 direction = marioMovement.facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            transform.position,
            direction,
            1.5f,
            enemyLayerMask
        );

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyAI enemyAI = hits[i].collider.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.OnCapeAttack(marioMovement.facingRight);
            }
        }

        capeDelayRoutine = null;
    }

    private IEnumerator CapeCooldown()
    {
        yield return new WaitForSeconds(capeCooldown);

        EndCape();

        canCape = true;
        isBlockingJump = false;

        capeCooldownRoutine = null;
    }

    /// <summary>
    /// Ends the cape state and restores normal movement parameters.
    /// </summary>
    private void EndCape()
    {
        EnsureCachedComponents();

        marioMovement.isCapeActive = false;
        if (animator != null)
            animator.SetBool("cape", false);

        if (marioRb != null)
            marioRb.gravityScale = marioMovement.fallgravity;
    }

    private void CancelCapeAttack()
    {
        EndCape();

        // Reset jump blocking immediately
        isBlockingJump = false;

        // Stop running coroutines if any
        if (capeDelayRoutine != null)
        {
            StopCoroutine(capeDelayRoutine);
            capeDelayRoutine = null;
        }

        if (capeCooldownRoutine != null)
        {
            StopCoroutine(capeCooldownRoutine);
            capeCooldownRoutine = null;
        }
    }

    private void FixedUpdate()
    {
        if (!marioMovement)
            return;

        EnsureCachedComponents();

        // Handle cape attack logic
        if (marioMovement.isCapeActive)
        {
            // Prevent overlapping actions during cape attack
            if (marioMovement.groundPounding || marioMovement.spinning || marioMovement.wallSliding)
            {
                CancelCapeAttack();
                return;
            }

            if (!marioMovement.onGround)
            {
                // Air-specific cape attack logic
                marioRb.gravityScale = marioMovement.fallgravity * 0.3f;
                // Slow descent while keeping vertical velocity above a minimum
                marioRb.velocity = new Vector2(0f, Mathf.Max(marioRb.velocity.y, -1f));
            }
            else if (!marioMovement.wasGrounded) // If touching ground during air cape attack
            {
                // Stop the air cape attack when landing
                CancelCapeAttack();
            }
            else // marioMovement.onGround && marioMovement.wasGrounded
            {
                // Limit horizontal movement while on ground during cape
                marioRb.velocity = new Vector2(0f, marioRb.velocity.y);
            }

            return; // Skip other movement logic during cape attack
        }

        // Update the grounded state tracker
        marioMovement.wasGrounded = marioMovement.onGround;

        // Allow jump and reset canCape only after cooldown
        if (!marioMovement.isCapeActive && marioMovement.onGround && !isBlockingJump && canCape)
        {
            isBlockingJump = false; // Allows jumping immediately
        }
    }
}