using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : BulletBill
{
    [Header("Arrow")]
    public float stuckInWallTime = 1f;
    private bool stuckInWall = false;
    public int stuckOrderInLayer = -2;
    public float stickInOffset = 0.1f;
    public float fadeTime = 0.5f;
    private RaycastHit2D hit;

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

    // Don't do regular landing or wall touch. Instead, do our own behavior
    public override void Land(GameObject other = null) { }
    protected override void onTouchWall(GameObject wall) { }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (stuckInWall)
        {
            return;
        }

        // Raycast to check if we hit a wall
        float distance = realVelocity.magnitude * Time.fixedDeltaTime;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, realVelocity.normalized, distance, wallMask);
        if (hit)
        {
            stuckInWall = true;
            this.hit = hit;

            // Set order in layer
            GetComponentInChildren<SpriteRenderer>().sortingOrder = stuckOrderInLayer;

            float timeUntilStuck = (hit.distance + stickInOffset) / realVelocity.magnitude;

            // wait a bit before sticking in the wall
            // This amount is probably not perfect, but it'll get fixed in StickInGround
            Invoke(nameof(StickInGround), timeUntilStuck);
        }
    }

    private void StickInGround()
    {
        // Set position to hit position to correctly stick in wall
        transform.position = hit.point + realVelocity.normalized * stickInOffset;

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

        // Fade out
        // wait a bit before fading out
        Invoke(nameof(StartFadeOut), stuckInWallTime);
    }

    private void StartFadeOut()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
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


    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Raycast
        Gizmos.color = Color.yellow;
        float distance = realVelocity.magnitude * Time.fixedDeltaTime;
        Gizmos.DrawRay(transform.position, realVelocity.normalized * distance);
    }
}