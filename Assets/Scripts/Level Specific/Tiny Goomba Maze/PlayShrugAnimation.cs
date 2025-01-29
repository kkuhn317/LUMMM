using System.Collections;
using UnityEngine;

public class PlayShrugAnimation : MonoBehaviour
{
    private bool hasPlayedShrug = false;

    public void TryPlayShrug()
    {
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // Ensure player exists before proceeding
        if (player == null)
        {
            Debug.LogError("Player not found in the scene!");
            return;
        }

        // Get Animator and MarioMovement from the player
        Animator animator = player.GetComponent<Animator>();
        MarioMovement marioMovement = player.GetComponent<MarioMovement>();

        // Check if components exist before using them
        if (animator == null)
        {
            Debug.LogError("Animator component missing from Player!");
            return;
        }
        if (marioMovement == null)
        {
            Debug.LogError("MarioMovement component missing from Player!");
            return;
        }

        // If animation has already played, don't play it again
        if (hasPlayedShrug) return;

        // Check if the player is still
        if (!marioMovement.isMoving)
        {
            // Play shrug animation
            animator.Play("mario_shrug");
            hasPlayedShrug = true;
            StartCoroutine(WaitForShrugAnimation(animator));
        }
    }

    private IEnumerator WaitForShrugAnimation(Animator animator)
    {
        // Wait for the shrug animation to finish
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 && 
                                      animator.GetCurrentAnimatorStateInfo(0).IsName("mario_shrug"));

        // Trigger the stop animation
        animator.SetTrigger("stopShrug");
    }
}