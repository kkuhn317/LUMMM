using System.Collections.Generic;
using UnityEngine;

public class EnemyStateSpriteSwitcher : MonoBehaviour
{
    private BoxCollider2D boxCollider2D;
    private SpriteSwapArea spriteSwapArea;
    private List<FallingSpike> fallingSpikesInTrigger = new List<FallingSpike>();

    private void Start()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FallingSpike fallingSpike = other.GetComponent<FallingSpike>();
        if (fallingSpike != null)
        {
            fallingSpikesInTrigger.Add(fallingSpike);
            UpdateColliderState();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        FallingSpike fallingSpike = other.GetComponent<FallingSpike>();
        if (fallingSpike != null)
        {
            fallingSpikesInTrigger.Remove(fallingSpike);
            UpdateColliderState();
        }
    }

    private void Update()
    {
        UpdateColliderState();
    }

    private void UpdateColliderState()
    {
        // Check if any FallingSpike in the trigger is falling
        bool hasFallingSpikes = fallingSpikesInTrigger.Exists(spike =>
            spike != null && spike.movement == ObjectPhysics.ObjectMovement.sliding
        );

        // Enable/disable the collider based on falling spike state
        if (boxCollider2D != null)
        {
            boxCollider2D.enabled = hasFallingSpikes;
        }
    }
}
