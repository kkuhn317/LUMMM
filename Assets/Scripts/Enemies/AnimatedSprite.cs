using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSprite : MonoBehaviour
{
    public Sprite[] sprites;
    public float framerate = 1f / 6f;

    private SpriteRenderer spriteRenderer;
    private int frame;
    private bool isAnimating = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        frame = 0;
        isAnimating = true;
        InvokeRepeating(nameof(Animate), framerate, framerate);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Animate()
    {
        if (!isAnimating)
            return;

        frame++;

        if (frame >= sprites.Length)
        {
            frame = 0;
        }

        if (frame >= 0 && frame < sprites.Length)
        {
            spriteRenderer.sprite = sprites[frame];
        }
    }
    public void StopAnimation()
    {
        isAnimating = false;
        spriteRenderer.sprite = sprites[0]; // Set the sprite to the first frame
    }

    public void PauseAnimation()
    {
        isAnimating = false;
    }

    public void ResumeAnimation()
    {
        isAnimating = true;
    }
}
