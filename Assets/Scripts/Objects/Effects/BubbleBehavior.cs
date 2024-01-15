using System.Collections;
using UnityEngine;

// The air bubbles Mario produces while he is underwater
public class BubbleBehavior : MonoBehaviour
{
    public float riseSpeed = 0.25f;
    public float bubbleDuration = 1.5f;
    public float fadeDuration = 0.5f;

    private bool firstFrame = true;
    private bool inWater = false;   // This should be set to true when it is spawned in water. if not, it will be destroyed

    private float startTime;
    private bool fading = false;
    private SpriteRenderer[] childSpriteRenderers; // Array to store child SpriteRenderers

    private void Start()
    {
        startTime = Time.time;
        // Get all SpriteRenderers in children
        childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void DestroyBubble()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        float elapsedTime = Time.time - startTime;

        if (!fading)
        {
            // Check if it's time to start fading
            if (elapsedTime >= bubbleDuration)
            {
                fading = true;
                StartCoroutine(FadeOut());
            }
        }

        // Apply rising velocity to the parent and child GameObjects
        transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
    }

    private void FixedUpdate() {
        if (!firstFrame && !inWater) {
            Destroy(gameObject);
        }
        firstFrame = false;
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            foreach (SpriteRenderer childRenderer in childSpriteRenderers)
            {
                Color startColor = childRenderer.color;
                Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

                float alpha = Mathf.Lerp(startColor.a, endColor.a, elapsedTime / fadeDuration);
                childRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final alpha is set for child GameObjects
        foreach (SpriteRenderer childRenderer in childSpriteRenderers)
        {
            childRenderer.color = new Color(childRenderer.color.r, childRenderer.color.g, childRenderer.color.b, 0f);
        }

        // Destroy the parent GameObject after fading out
        Destroy(gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            inWater = true;
        }
    }
}
