using UnityEngine;

public class CustomDeathHandler : MonoBehaviour
{
    [Header("Custom Death Settings")]
    public GameObject customDeathPrefab; // The object to transform Mario into.

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            MarioMovement playerScript = other.GetComponent<MarioMovement>();
            if (playerScript != null)
            {
                HandlePlayerCollision(playerScript);
            }
        }
    }

    private void HandlePlayerCollision(MarioMovement player)
    {
        // Transform Mario if small or damage otherwise.
        if (PowerStates.IsSmall(player.powerupState) && customDeathPrefab != null)
        {
            // Transform Mario into the custom death object.
            player.TransformIntoObject(customDeathPrefab);
            Debug.Log($"Transforming Mario into: {customDeathPrefab.name}");
        }
        else
        {
            // Damage Mario if not small.
            player.damageMario();
        }
    }
}