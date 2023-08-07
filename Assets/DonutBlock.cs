using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DonutBlock : MonoBehaviour
{
    public float dropTime = 1f;
    public float regenerateTime = 5f;
    public Sprite normalSprite;
    public Sprite droppedSprite;

    private bool isPlayerOn = false;
    private bool isDropping = false;
    private bool isRegenerating = false;
    private float timeOnBlock = 0f;

    private Vector3 initialPosition;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private void Start()
    {
        initialPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
    }

    private void Update()
    {

        // if we have a child, then we assume the player is on it
        // this could probably be done better but it works for now
        isPlayerOn = transform.childCount > 0;

        if (isPlayerOn && !isDropping && !isRegenerating)
        {
            spriteRenderer.sprite = droppedSprite; // Change to droppedSprite when the player steps on the block
            timeOnBlock += Time.deltaTime;
            if (timeOnBlock >= dropTime)
            {
                StartCoroutine(Drop());
            }
        }
        else
        {
            spriteRenderer.sprite = normalSprite;
            timeOnBlock = 0f; // Reset the timer if the player leaves the block
        }
    }

    private IEnumerator Drop()
    {
        isDropping = true;
        rb.isKinematic = false; // Enable physics to let the block fall

        yield return new WaitForSeconds(dropTime);

        isPlayerOn = false;
        isDropping = false;
        isRegenerating = true;

        StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        yield return new WaitForSeconds(regenerateTime);

        isRegenerating = false;
        transform.position = initialPosition;
        rb.isKinematic = true; // Disable physics while at the initial position
    }

}