using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFade : MonoBehaviour
{
    public float fadeDuration = 2f; // Duration of the fade in seconds
    public float delayBeforeFade = 5f; // Delay before starting the fade in seconds

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float fadeTimer;
    private bool fading = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        fadeTimer = delayBeforeFade;
    }

    private void Update()
    {
        fadeTimer -= Time.deltaTime;

        // Check if it's time to start fading
        if (fadeTimer <= 0f && !fading)
        {
            fading = true;
            StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the object is fully faded out
        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }
}
