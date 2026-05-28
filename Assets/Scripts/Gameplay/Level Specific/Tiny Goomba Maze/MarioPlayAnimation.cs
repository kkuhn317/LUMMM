using System.Collections;
using UnityEngine;

// TODO: Improve this script as much as needed, then move it out of Tiny Goomba Maze folder
public class MarioPlayAnimation : MonoBehaviour
{
    private bool hasPlayed = false;
    public string animName = "";
    public string stopAnimTrigger = "";

    public void TryPlay()
    {
        // Find the player in the scene
        PlayerRegistry playerRegistry = GameManager.Instance.GetSystem<PlayerRegistry>();
        GameObject[] players = playerRegistry != null ? playerRegistry.GetAllPlayerObjects() : null;
        GameObject player = players != null && players.Length > 0 ? players[0] : null; // Todo: account for multiplayer

        // Ensure player exists before proceeding
        if (player == null)
        {
            Debug.LogError("Player not found in the scene!");
            return;
        }

        // Get Animator and MarioMovement from the player
        Animator animator = player.GetComponent<Animator>();
        MarioCore marioMovement = player.GetComponent<MarioCore>();
        

        // Check if components exist before using them
        if (animator == null)
        {
            Debug.LogError("Animator component missing from Player!");
            return;
        }
        if (marioMovement == null)
        {
            Debug.LogError("MarioCore component missing from Player!");
            return;
        }

        // If animation has already played, don't play it again
        if (hasPlayed) return;

        // Check if the player is still
        if (!marioMovement.State.IsMoving)
        {
            // Play animation
            animator.Play(animName);
            hasPlayed = true;
            StartCoroutine(WaitForAnimation(animator));
        }
    }

    private IEnumerator WaitForAnimation(Animator animator)
    {
        // Wait for the animation to finish
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 && 
                                      animator.GetCurrentAnimatorStateInfo(0).IsName(animName));

        // Trigger the stop animation
        if (stopAnimTrigger != "")
            animator.SetTrigger(stopAnimTrigger);
    }
}