using System.Collections;
using UnityEngine;

public class HorizontalImpulseOnRise : MonoBehaviour
{
    public float horizontalImpulse = 2f; // Horizontal force applied when entering the trigger

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object has an ObjectPhysics script
        ObjectPhysics objectPhysics = other.GetComponent<ObjectPhysics>();
        if (objectPhysics != null)
        {
            // Start monitoring the object's velocity to apply the impulse at the correct time
            StartCoroutine(ApplyImpulseWhenRising(objectPhysics));
        }
    }

    private IEnumerator ApplyImpulseWhenRising(ObjectPhysics objectPhysics)
    {
        // Wait until the object starts moving upward (velocity.y > 0)
        while (objectPhysics.velocity.y <= 0)
        {
            yield return null; // Wait for the next frame
        }

        // Apply horizontal impulse
        objectPhysics.velocity = new Vector2(Mathf.Abs(objectPhysics.velocity.x + horizontalImpulse), objectPhysics.velocity.y);
        Debug.Log("Horizontal impulse applied while rising!");
    }
}
