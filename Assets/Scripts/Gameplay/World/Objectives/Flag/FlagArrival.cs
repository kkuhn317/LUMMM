using System.Collections;
using UnityEngine;

/// <summary>
/// Handles post-slide movement — waiting for the flag to finish lowering,
/// flipping the puppet to face away from the pole, then walking/jumping/hopping
/// to the designated target position with simulated gravity.
/// </summary>
public class FlagArrival : MonoBehaviour
{
    public enum ArrivalMode
    {
        None,        // stays at pole bottom, no movement
        Walk,        // walks horizontally to target
        Jump,        // full jump arc to target
        HopThenWalk, // small hop then walks the rest
    }

    [Tooltip("Where cutscene Marios move after reaching the pole bottom. Leave null to skip.")]
    public Transform postSlideTarget;

    [Tooltip("How each player arrives at the post-slide target.")]
    public ArrivalMode arrivalMode = ArrivalMode.Walk;

    [Tooltip("Horizontal spacing between players at the target.")]
    public float playerSpacing = 0.5f;

    [Tooltip("Walk speed toward the target.")]
    public float walkSpeed = 3f;

    [Tooltip("Height of the jump/hop arc.")]
    public float jumpHeight = 2f;

    [Tooltip("Duration of the jump arc in seconds.")]
    public float jumpDuration = 0.6f;

    [Tooltip("For HopThenWalk: fraction of distance covered by the hop (0-1).")]
    [Range(0f, 1f)]
    public float hopDistance = 0.3f;

    [Tooltip("Simulated gravity strength for the drop phase.")]
    public float gravity = 20f;

    [Tooltip("If true, hides the cutscene puppet once it reaches its landing position.")]
    public bool hidePuppetOnArrival = false;


    // ─── Event ───────────────────────────────────────────────────────────────

    /// <summary>Fired when a player puppet has fully arrived at its target slot.</summary>
    public System.Action<FlagSlide.PlayerSlideState> OnPlayerArrived;

    /// <summary>Fired right before arrivalMode movement begins (puppet has flipped and is ready to walk/jump).</summary>
    public System.Action OnArrivalMovementStarting;

    // ─── Public API ──────────────────────────────────────────────────────────

    public bool HasTarget => postSlideTarget != null;

    public Vector3 GetSlot(int order, Vector3 polePos)
    {
        if (postSlideTarget == null) return Vector3.zero;
        float dir = postSlideTarget.position.x > polePos.x ? 1f : -1f;
        return postSlideTarget.position + new Vector3(order * playerSpacing * -dir, 0f, 0f);
    }

    // ─── Coroutines ──────────────────────────────────────────────────────────

    public IEnumerator WaitForFlagThenMove(
        FlagSlide.PlayerSlideState ps,
        GameObject flag,
        Vector3 flagFinalLocal,
        bool flagOnRight)
    {
        var puppet = ps.CutsceneMarioInstance;
        var anim   = puppet.GetComponent<Animator>();

        // Freeze climb animation immediately on arrival at bottom (keep isSideClimbing true while flag lowers)
        if (anim != null)
        {
            anim.SetFloat("climbSpeed", 0f);
            anim.SetBool("isSideClimbing", true);
        }

        // Keep waiting while flag finishes lowering
        while (Vector3.Distance(flag.transform.localPosition, flagFinalLocal) > 0.01f)
            yield return null;

        yield return new WaitForSeconds(0.1f);

        // Determine which way the level is (where the target is relative to the pole)
        bool levelIsRight = postSlideTarget != null
            ? postSlideTarget.position.x > transform.position.x
            : !flagOnRight;

        // Only flip and reposition if we're actually going to move
        if (arrivalMode != ArrivalMode.None && postSlideTarget != null)
        {
            // Flip toward pole AND move to other side at the same time
            FlipPuppet(puppet, !levelIsRight);
            float offsetX = flagOnRight ? -0.4f : 0.4f;
            puppet.transform.position = new Vector3(
                transform.position.x + offsetX,
                puppet.transform.position.y,
                puppet.transform.position.z);

            yield return new WaitForSeconds(0.05f);

            // Flip to face the level (away from pole)
            FlipPuppet(puppet, levelIsRight);

            // Clear climb state immediately after flip — before any further waits
            if (anim != null)
            {
                anim.SetBool("isSideClimbing", false);
                anim.SetBool("onGround", true);
            }
        }

        // Fire event — ideal time to start end music
        OnArrivalMovementStarting?.Invoke();

        yield return new WaitForSeconds(0.1f);

        // Execute post-slide movement if target is assigned
        if (postSlideTarget != null && arrivalMode != ArrivalMode.None)
        {
            ps.TargetSlot = GetSlot(ps.Order, transform.position);
            yield return StartCoroutine(MoveToTarget(puppet, anim, ps.TargetSlot, ps.Mario));
        }

        ps.ArrivedAtTarget = true;
        OnPlayerArrived?.Invoke(ps);

        if (hidePuppetOnArrival && arrivalMode != ArrivalMode.None)
            puppet.SetActive(false);
    }

    /// <summary>Flips the puppet sprite via SpriteRenderer.flipX on the Visual child.</summary>
    private void FlipPuppet(GameObject puppet, bool faceRight)
    {
        var sr = puppet.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = !faceRight;
    }

    private IEnumerator MoveToTarget(GameObject puppet, Animator anim, Vector3 end, MarioCore mario = null)
    {
        Vector3 start = puppet.transform.position;

        // Simulated gravity drop if target is lower
        if (start.y > end.y + 0.05f)
        {
            if (anim != null)
            {
                anim.SetBool("onGround", false);
                anim.SetFloat("Horizontal", 0f);
            }

            float fallVelocity = 0f;
            var groundDetection = puppet.GetComponent<PuppetGroundDetection>();

            while (puppet.transform.position.y > end.y + 0.01f)
            {
                fallVelocity += gravity * Time.deltaTime;
                float stepDistance = fallVelocity * Time.deltaTime;

                // Check if ground is within this frame's fall distance before moving
                if (groundDetection != null && groundDetection.CheckGround(stepDistance))
                {
                    puppet.transform.position = new Vector3(start.x, groundDetection.GroundY, start.z);
                    break;
                }

                float newY = Mathf.Max(puppet.transform.position.y - stepDistance, end.y);
                puppet.transform.position = new Vector3(start.x, newY, start.z);

                yield return null;
            }

            if (anim != null)
            {
                anim.SetBool("onGround", true);
                anim.SetFloat("Horizontal", 0f);
            }

            start = puppet.transform.position;
        }

        switch (arrivalMode)
        {
            case ArrivalMode.Walk:
                yield return StartCoroutine(Walk(puppet, anim, start, end));
                break;

            case ArrivalMode.Jump:
                yield return StartCoroutine(JumpArc(puppet, anim, start, end, jumpHeight, mario: mario));
                break;

            case ArrivalMode.HopThenWalk:
                // Find ground beneath the hop landing X so the arc descends into it
                float hopX = Mathf.Lerp(start.x, end.x, hopDistance);
                var gd = puppet.GetComponent<PuppetGroundDetection>();
                float hopGroundY = start.y; // fallback
                if (gd != null)
                {
                    var hit = Physics2D.Raycast(new Vector2(hopX, start.y + 5f), Vector2.down, 20f, gd.GroundLayer);
                    if (hit.collider != null) hopGroundY = hit.point.y + gd.pivotOffset;
                }
                Vector3 hopLanding = new Vector3(hopX, hopGroundY, start.z);
                yield return StartCoroutine(JumpArc(puppet, anim, start, hopLanding, jumpHeight * 0.5f, jumpDuration * hopDistance, mario: mario));
                yield return StartCoroutine(Walk(puppet, anim, puppet.transform.position, end));
                break;
        }
    }

    /// <summary>
    /// Raycasts straight down from well above the target X to find the actual
    /// ground surface Y. Falls back to the original position if nothing is hit.
    /// </summary>
    /// <summary>
    /// Falls straight down with gravity until grounded. Used to land cleanly
    /// after the hop in HopThenWalk before walking begins.
    /// </summary>
    private IEnumerator DropToGround(GameObject puppet, Animator anim)
    {
        var groundDetection = puppet.GetComponent<PuppetGroundDetection>();
        if (groundDetection == null) yield break;

        // Already on ground — nothing to do
        if (groundDetection.CheckGround(0.05f)) yield break;

        if (anim != null)
        {
            anim.SetBool("onGround", false);
            anim.SetFloat("Horizontal", 0f);
            anim.SetBool("isRunning", false);
        }

        float fallVelocity = 0f;

        while (!groundDetection.IsGrounded)
        {
            fallVelocity += gravity * Time.deltaTime;
            float stepDistance = fallVelocity * Time.deltaTime;

            if (groundDetection.CheckGround(stepDistance))
            {
                puppet.transform.position = new Vector3(
                    puppet.transform.position.x,
                    groundDetection.GroundY,
                    puppet.transform.position.z);
                break;
            }

            puppet.transform.position = new Vector3(
                puppet.transform.position.x,
                puppet.transform.position.y - stepDistance,
                puppet.transform.position.z);

            yield return null;
        }

        if (anim != null)
            anim.SetBool("onGround", true);
    }

    private IEnumerator Walk(GameObject puppet, Animator anim, Vector3 from, Vector3 to)
    {
        float dir = to.x > from.x ? 1f : -1f;
        var groundDetection = puppet.GetComponent<PuppetGroundDetection>();

        // Flip sprite to match actual walk direction
        var sr = puppet.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = dir < 0f;

        if (anim != null)
        {
            anim.SetFloat("Horizontal", dir);
            anim.SetBool("onGround", true);
            anim.SetBool("isSideClimbing", false);
            anim.SetBool("isRunning", true);
        }

        float verticalVelocity = 0f;
        float stuckTimer       = 0f;
        float lastX            = puppet.transform.position.x;
        const float stuckThreshold  = 0.01f; // min X movement per second to not be considered stuck
        const float stuckTimeLimit  = 0.4f;  // seconds before giving up and snapping

        while (Mathf.Abs(puppet.transform.position.x - to.x) > 0.01f)
        {
            float dt  = Time.deltaTime;
            float newX = Mathf.MoveTowards(puppet.transform.position.x, to.x, walkSpeed * dt);

            // Stuck detection — if X barely moved, count up and eventually snap
            float xDelta = Mathf.Abs(newX - lastX);
            if (xDelta < stuckThreshold * dt)
            {
                stuckTimer += dt;
                if (stuckTimer >= stuckTimeLimit)
                {
                    puppet.transform.position = to;
                    break;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastX = newX;

            // Apply gravity
            bool grounded = groundDetection != null && groundDetection.CheckGround(Mathf.Max(verticalVelocity * dt, 0.05f));
            if (grounded)
            {
                verticalVelocity = 0f;
                puppet.transform.position = new Vector3(newX, groundDetection.GroundY, puppet.transform.position.z);
            }
            else
            {
                verticalVelocity = Mathf.Min(verticalVelocity + gravity * dt, 30f); // clamp terminal velocity
                float newY = puppet.transform.position.y - verticalVelocity * dt;
                puppet.transform.position = new Vector3(newX, newY, puppet.transform.position.z);

                if (anim != null) anim.SetBool("onGround", false);
            }

            if (grounded && anim != null) anim.SetBool("onGround", true);

            yield return null;
        }

        puppet.transform.position = to;

        if (anim != null)
        {
            anim.SetFloat("Horizontal", 0f);
            anim.SetBool("isRunning", false);
            anim.SetBool("onGround", true);
        }
    }

    private IEnumerator JumpArc(GameObject puppet, Animator anim, Vector3 from, Vector3 to, float height, float duration = -1f, MarioCore mario = null)
    {
        float dir = to.x > from.x ? 1f : -1f;

        // Flip sprite to match actual jump direction
        var sr = puppet.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = dir < 0f;

        if (anim != null)
        {
            anim.SetBool("isSideClimbing", false);
            anim.SetBool("onGround", false);
            anim.SetFloat("Horizontal", dir);
        }

        // Auto-calculate duration from horizontal distance if not overridden,
        // so the arc scales with distance the same way Walk does.
        float horizDist = Mathf.Abs(to.x - from.x);
        float autoDuration = horizDist > 0.01f ? horizDist / walkSpeed : jumpDuration;
        float actualDuration = duration > 0f ? duration : autoDuration;

        // Play the player's own jump sound from MarioAudio
        if (mario != null)
        {
            var marioAudio = mario.GetComponent<MarioAudio>();
            if (marioAudio != null && marioAudio.JumpSound != null)
                AudioManager.Instance?.Play(marioAudio.JumpSound, SoundCategory.SFX);
        }
        float elapsed = 0f;
        var groundDetection = puppet.GetComponent<PuppetGroundDetection>();
        bool hitGround = false;

        while (elapsed < actualDuration && !hitGround)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / actualDuration);
            float x    = Mathf.Lerp(from.x, to.x, t);
            float yArc = 4f * height * t * (1f - t);
            float y    = Mathf.Lerp(from.y, to.y, t) + yArc;

            // On the way down, check if ground is within this frame's drop
            bool descending = y < puppet.transform.position.y;
            if (descending && groundDetection != null)
            {
                float stepDistance = puppet.transform.position.y - y;
                if (groundDetection.CheckGround(stepDistance))
                {
                    puppet.transform.position = new Vector3(x, groundDetection.GroundY, from.z);
                    hitGround = true;
                    break;
                }
            }

            puppet.transform.position = new Vector3(x, y, from.z);
            yield return null;
        }

        if (!hitGround)
            puppet.transform.position = to;

        if (anim != null)
        {
            anim.SetBool("onGround", true);
            anim.SetFloat("Horizontal", 0f);
        }
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (postSlideTarget == null) return;

        Vector3 poleBottomPos = transform.position + new Vector3(0f, 1.0f, 0f);
        Vector3 targetPos     = postSlideTarget.position;
        float   dir           = targetPos.x > transform.position.x ? 1f : -1f;

        for (int i = 0; i < 4; i++)
        {
            Vector3 slot  = targetPos + new Vector3(i * playerSpacing * -dir, 0f, 0f);
            float   alpha = 1f - (i * 0.2f);

            Gizmos.color = new Color(1f, 1f, 0f, alpha);
            Gizmos.DrawWireSphere(slot, 0.2f);

            switch (arrivalMode)
            {
                case ArrivalMode.Walk:
                    Gizmos.color = new Color(0f, 1f, 0f, alpha);
                    Gizmos.DrawLine(poleBottomPos, slot);
                    break;

                case ArrivalMode.Jump:
                    Gizmos.color = new Color(0f, 0.8f, 1f, alpha);
                    DrawArc(poleBottomPos, slot, jumpHeight, 20);
                    break;

                case ArrivalMode.HopThenWalk:
                    Vector3 hopEnd = Vector3.Lerp(poleBottomPos, slot, hopDistance);
                    Gizmos.color = new Color(1f, 0.5f, 0f, alpha);
                    DrawArc(poleBottomPos, hopEnd, jumpHeight * 0.5f, 12);
                    Gizmos.color = new Color(0f, 1f, 0f, alpha);
                    Gizmos.DrawLine(hopEnd, slot);
                    break;
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, 0.15f);
        Gizmos.DrawLine(targetPos + Vector3.up * 0.3f,    targetPos + Vector3.down  * 0.3f);
        Gizmos.DrawLine(targetPos + Vector3.left * 0.3f,  targetPos + Vector3.right * 0.3f);
    }

    private void DrawArc(Vector3 from, Vector3 to, float height, int segments)
    {
        Vector3 prev = from;
        for (int i = 1; i <= segments; i++)
        {
            float t    = i / (float)segments;
            float x    = Mathf.Lerp(from.x, to.x, t);
            float yArc = 4f * height * t * (1f - t);
            float y    = Mathf.Lerp(from.y, to.y, t) + yArc;
            Vector3 next = new Vector3(x, y, from.z);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}