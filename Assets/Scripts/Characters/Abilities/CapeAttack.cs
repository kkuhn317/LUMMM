using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class CapeAttack : MarioAbility
{
    public AudioClip marioPrepareAttackSound;
    public bool canCape = true;
    public float capeCooldown = 0.75f;
    public float capeAttackDelay = 0.25f; // Time after button press until attack is executed (should line up with animation)

    public override void onExtraActionPressed()
    {
        if (canCape && !marioMovement.groundPounding && !marioMovement.spinning && !marioMovement.wallSliding) // Prevent cape attack while ground pounding, spinning, or wall sliding
        {
            marioMovement.isCapeActive = true; // Set cape attack active
            marioMovement.GetComponent<Animator>().SetBool("cape", true);

            // If in the air, allow flipping
            if (!marioMovement.onGround && marioMovement.moveInput.x != 0 && marioMovement.facingRight != (marioMovement.moveInput.x > 0))
            {
                marioMovement.FlipTo(marioMovement.moveInput.x > 0);
            }

            GetComponent<AudioSource>().PlayOneShot(marioPrepareAttackSound);
            isBlockingJump = true;
            canCape = false;

            StartCoroutine(CapeAttackDelay());
            StartCoroutine(CapeCooldown());
        }
    }

    private IEnumerator CapeAttackDelay()
    {
        yield return new WaitForSeconds(capeAttackDelay);

        // Perform cape attack logic (e.g., hit enemies)
        RaycastHit2D[] hit = Physics2D.RaycastAll(transform.position, marioMovement.facingRight ? Vector2.right : Vector2.left, 1.5f, LayerMask.GetMask("Enemy"));

        foreach (RaycastHit2D enemy in hit)
        {
            EnemyAI enemyAI = enemy.collider.GetComponent<EnemyAI>();
            if (enemyAI)
            {
                enemyAI.OnCapeAttack(marioMovement.facingRight);
            }
        }
    }

    private IEnumerator CapeCooldown()
    {
        yield return new WaitForSeconds(capeCooldown);

        marioMovement.isCapeActive = false; // Reset cape state
        marioMovement.GetComponent<Animator>().SetBool("cape", false); // Stop animation
        canCape = true;
        isBlockingJump = false;
    }

    private void CancelCapeAttack()
    {
        marioMovement.isCapeActive = false;
        marioMovement.GetComponent<Animator>().SetBool("cape", false);

        // Reset jump blocking immediately
        isBlockingJump = false;

        // If interrupted by landing, allow cape attack again
        if (marioMovement.onGround)
        {
            StopCoroutine(CapeAttackDelay());
            StopCoroutine(CapeCooldown());
        }
    }

    private void FixedUpdate()
    {
        Rigidbody2D rb = marioMovement.GetComponent<Rigidbody2D>();

        // Handle cape attack logic
        if (marioMovement.isCapeActive)
        {
            // Prevent overlapping actions during cape attack
            if (marioMovement.groundPounding || marioMovement.spinning || marioMovement.wallSliding)
            {
                CancelCapeAttack();
                rb.gravityScale = marioMovement.fallgravity;
                return;
            }

            if (!marioMovement.onGround)
            {
                // Air-specific cape attack logic
                rb.gravityScale = marioMovement.fallgravity * 0.3f;
                rb.velocity = new Vector2(0, Mathf.Max(rb.velocity.y, -1f)); // Retain horizontal momentum, slow descent
            }
            else if (marioMovement.onGround && !marioMovement.wasGrounded) // If touching ground during air cape attack
            {
                // Stop the air cape attack when landing
                CancelCapeAttack();
            } 
            else if (marioMovement.onGround)
            {
                // Limit horizontal movement in the air
                marioMovement.GetComponent<Rigidbody2D>().velocity = new Vector2(0, marioMovement.GetComponent<Rigidbody2D>().velocity.y);
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

        // Handle interruptions like spinning or ground pounding
        if (marioMovement.groundPounding && marioMovement.isCapeActive)
        {
            CancelCapeAttack();
        }

        if (marioMovement.spinning && marioMovement.isCapeActive)
        {
            CancelCapeAttack();
        }
    }
}