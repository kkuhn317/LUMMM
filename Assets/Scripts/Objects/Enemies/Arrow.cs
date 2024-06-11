using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : BulletBill
{
    [Header("Arrow")]
    public float stuckInWallTime = 1f;
    public bool stuckInWall = false;
    public int stuckOrderInLayer = -2;
    public float stickInOffset = 0.1f;

    public float fadeTime = 0.5f;

    protected override void hitByStomp(GameObject player)
    {   
        // Just damage the player from all sides
        base.hitOnSide(player);
    }

    protected override void hitOnSide(GameObject player)
    {
        base.hitOnSide(player);
        Destroy(gameObject);
    }

    protected override void onTouchWallRaycast(RaycastHit2D hit)
    {
        if (!stuckInWall)
        {
            stuckInWall = true;

            // Go in the wall a bit
            Vector2 stickOutOffset = realVelocity.normalized * stickInOffset;
            transform.position = hit.point + stickOutOffset;

            // Stop moving
            velocity = Vector2.zero;
            movement = ObjectMovement.still;

            // No hitbox
            GetComponent<BoxCollider2D>().enabled = false;

            // Stop rotating to movement
            rotateToMovement = false;

            // Call animation
            GetComponent<Animator>().SetTrigger("HitWall");

            // Play sound
            GetComponent<AudioSource>().Play();

            // Set order in layer
            GetComponentInChildren<SpriteRenderer>().sortingOrder = stuckOrderInLayer;

            // Fade out
            // wait a bit before fading out
            Invoke(nameof(StartFadeOut), stuckInWallTime);
        }
    }

    private void StartFadeOut()
    {
        StartCoroutine(FadeOut());
    }

    protected IEnumerator FadeOut()
    {
        // Fade out over time, then destroy
        float time = 0;
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            Color color = GetComponentInChildren<SpriteRenderer>().color;
            color.a = 1 - time / fadeTime;
            GetComponentInChildren<SpriteRenderer>().color = color;
            yield return null;
        }

        Destroy(gameObject);
    }
}