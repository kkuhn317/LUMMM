using UnityEngine;

public class PauseableMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 storedVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("PauseableMovement: No Rigidbody2D component found on " + gameObject.name);
        }
    }

    public void Pause()
    {
        if (rb != null)
        {
            // storedVelocity = rb.velocity;
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    public void Resume()
    {
        if (rb != null)
        {
            rb.simulated = true;
            // rb.velocity = storedVelocity;
        }
    }
}
