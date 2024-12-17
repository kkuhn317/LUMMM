using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class CapeAttack : MarioAbility
{
    public AudioClip capeSound;
    private bool canCape = true;
    public float capeCooldown = 0.5f;
    public float capeAttackDelay = 0.25f; // Time after button press until attack is executed (should line up with animation)

    public override void onExtraActionPressed()
    {
        if (canCape)
        {
            GetComponent<Animator>().SetTrigger("cape");
            GetComponent<AudioSource>().PlayOneShot(capeSound);
            isBlockingJump = true;
            canCape = false;

            // Delay the attack
            StartCoroutine(CapeAttackDelay());

            // Start the cooldown
            StartCoroutine(CapeCooldown());
        }
    }

    private IEnumerator CapeAttackDelay()
    {
        yield return new WaitForSeconds(capeAttackDelay);
        // Attack
        // Raycast in front of Mario to hit enemies
        RaycastHit2D[] hit = Physics2D.RaycastAll(transform.position, marioMovement.facingRight ? Vector2.right : Vector2.left, 1.5f, LayerMask.GetMask("Enemy"));

        foreach (RaycastHit2D enemy in hit)
        {
            print("Enemy hit: " + enemy.collider.name);
            EnemyAI enemyAI = enemy.collider.GetComponent<EnemyAI>();
            if (enemyAI) {
                enemy.collider.GetComponent<EnemyAI>().OnCapeAttack(marioMovement.facingRight);
            }
        }
    }

    private IEnumerator CapeCooldown()
    {
        yield return new WaitForSeconds(capeCooldown);
        canCape = true;
        isBlockingJump = false;
    }
}

