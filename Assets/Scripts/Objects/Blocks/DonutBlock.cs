using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DonutBlock : MonoBehaviour
{
    public float dropTime = 3f;
    public float regenerateTime = 3f;
    public float fallSpeed = 0.5f; // Control the falling speed
    public Sprite normalSprite;
    public Sprite droppedSprite;

    private bool isPlayerOn = false;
    private bool isDropping = false;
    private bool isRegenerating = false;
    private float timeOnBlock = 0f;

    private Vector3 initialPosition;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Animator animator;

    private void Start()
    {
        initialPosition = transform.position;
        spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        animator.enabled = false;
        rb.isKinematic = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Make the player a child of the DonutBlock
            collision.transform.parent = transform;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Remove the player as a child of the DonutBlock
            collision.transform.parent = null;
        }
    }

    private void Update()
    {
        // Check if the player is on the block
        isPlayerOn = transform.childCount > 1;

        // Change the sprite based on player interaction
        if (isPlayerOn)
        {
            animator.enabled = true;
            spriteRenderer.sprite = droppedSprite; // Change to droppedSprite when the player steps on the block
            timeOnBlock += Time.deltaTime;
            if (timeOnBlock >= dropTime)
            {
                StartCoroutine(Drop());
            }
        }
        else
        {
            if (!isDropping && !isRegenerating)
            {
                animator.enabled = false;
                // If the player is not on the block and not dropping or regenerating, change the sprite back to normalSprite
                spriteRenderer.sprite = normalSprite;
                timeOnBlock = 0f; // Reset the timer if the player leaves the block
            }
        }
    }

    private IEnumerator Drop()
    {
        animator.enabled = false;
        isDropping = true;
        rb.isKinematic = false; // Enable physics to let the block fall

        // Set the falling speed
        rb.velocity = new Vector2(0f, -fallSpeed);

        yield return new WaitForSeconds(dropTime);

        isPlayerOn = false;
        isDropping = false;
        isRegenerating = true;

        // Make the block static for regeneration
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        spriteRenderer.sprite = normalSprite;

        StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        yield return new WaitForSeconds(regenerateTime);

        isRegenerating = false;
        transform.position = initialPosition;

        // Reset the block's velocity and angular velocity
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.isKinematic = true; // Disable physics while at the initial position
    }
}
