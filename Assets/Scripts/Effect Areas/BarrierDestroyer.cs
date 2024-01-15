using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierDestroyer : MonoBehaviour
{
    // Define an array of LayerMasks to specify which layers can trigger the barrier.
    public LayerMask triggerLayers;

    // This method gets called when a Collider enters the trigger zone of the barrier.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the entering object is in one of the specified layers.
        if (triggerLayers == (triggerLayers | (1 << other.gameObject.layer)))
        {
            // Destroy the entering object.
            Destroy(other.gameObject);
        }
    }
}
