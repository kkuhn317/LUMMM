using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps; //Access to tilemaps libraries

public class Fade : MonoBehaviour
{
    public float fadeDuration;
    private bool isFadedOut = true; //It's true, so the game starts with the FadeIn effect
    private Tilemap tilemap;
    public string playerTag = "Player";

    private void Start()
    {
        tilemap = GetComponent<Tilemap>();
        isFadedOut = false; //Set to false so that the scene starts with the fade in effect
    }

    public void FadeIn()
    {
        if (tilemap.color.a == 1f) return; //Skip fade in if already fully opaque
        StartCoroutine(FadeEffect(0f, 1f));
    }

    public void FadeOut()
    {
        if (tilemap.color.a == 0f) return; //Skip fade out if already fully transparent
        StartCoroutine(FadeEffect(1f, 0f));
    }

    private IEnumerator FadeEffect(float start, float end)
    {
        float elapsedTime = 0f;
        Color color = tilemap.color;
        color.a = start;
        tilemap.color = color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(start, end, elapsedTime / fadeDuration); //smoothstep
            tilemap.color = color;
            yield return null;
        }
    }

    //Detect player colliders and start fade effects
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            if (!isFadedOut)
            {
                isFadedOut = true;
                FadeOut();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            if (isFadedOut)
            {
                isFadedOut = false;
                FadeIn();
            }
        }
    }
}
