using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class PlayerDeathTrigger : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnPlayerDeath; // Event triggered when the player "dies."

    private MarioMovement playerScript; // Tracks the player inside the trigger.

    private void Update()
    {
        if (playerScript != null)
        {
            Debug.Log($"{playerScript.Dead}");
            if (playerScript.Dead)
            {
                Debug.Log("Player is dead. Invoking OnPlayerDeath event.");
                HandlePlayerDeath(playerScript);
                playerScript = null; // Prevent repeated invocations.
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered trigger: {other.gameObject.name}");
            playerScript = other.GetComponent<MarioMovement>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited the trigger.");
            playerScript = null;
        }
    }

    private void HandlePlayerDeath(MarioMovement player)
    {
        // Trigger the OnPlayerDeath event.
        OnPlayerDeath?.Invoke();
        Debug.Log("OnPlayerDeath event invoked.");
    }
}
