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
        MarioCore marioMovement = null;
        if (playerRegistry != null)
        {
            var players = playerRegistry.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null) continue;
                marioMovement = players[i]; // Todo: account for multiplayer
                break;
            }
        }
        GameObject player = marioMovement != null ? marioMovement.gameObject : null;

        // Ensure player exists before proceeding
        if (player == null)
        {
            Debug.LogError("Player not found in the scene!");
            return;
        }

        // Get Animator and MarioMovement from the player
        Animator animator = player.GetComponent<Animator>();
        // Check if components exist before using them
        if (animator == null)
        {
            Debug.LogError("Animator component missing from Player!");
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
