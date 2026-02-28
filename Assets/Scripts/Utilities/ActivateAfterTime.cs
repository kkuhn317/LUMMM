using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateAfterTime : MonoBehaviour
{
    public Axe axeScript; // Reference to the Axe script
    public GameObject objectToActivate; // Reference to the object you want to activate
    public float timerAfterBridgeDestroy = 3.0f;

    private SpriteRenderer attachedSpriteRenderer;
    private SpriteRenderer objectToActivateSpriteRenderer;
    private Animator objectToActivateAnimator;

    // Start is called before the first frame update
    void Start()
    {
        // Assuming you want to get the SpriteRenderer of the objectToActivate
        objectToActivateSpriteRenderer = objectToActivate.GetComponent<SpriteRenderer>();
        objectToActivateAnimator = objectToActivate.GetComponent<Animator>();
        objectToActivateAnimator.enabled = false;

        // Assuming you want to get the SpriteRenderer of the current GameObject (this script is attached to)
        attachedSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the Axe script is not null and has destroyed the bridge
        if (axeScript != null && axeScript.bridgeDestroyed)
        {
            // Start the timer
            timerAfterBridgeDestroy -= Time.deltaTime;

            // When the timer reaches zero or less, activate the specified GameObject
            if (timerAfterBridgeDestroy <= 0f)
            {
                // Activate the specified object
                objectToActivate.SetActive(true);

                // Assign the current GameObject's SpriteRenderer to objectToActivate's SpriteRenderer (redundant-looking line)
                objectToActivateSpriteRenderer = attachedSpriteRenderer;

                // Disable the current GameObject's SpriteRenderer to make it invisible
                attachedSpriteRenderer.enabled = false;

                // Enable the Animator component of the objectToActivate to start animations (if any)
                objectToActivateAnimator.enabled = true;
            }
        }
    }
}
