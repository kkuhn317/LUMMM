using System.Collections;
using UnityEngine;
using PowerupState = PowerStates.PowerupState;

public class BigDoor : Door
{
    [Header("Auto-Move to Center (Axe-style)")]
    [Tooltip("Horizontal speed while the door auto-moves the player to its center.")]
    public float autoMoveSpeed = 3.5f;

    [Tooltip("How close (in world units) to count as centered on X.")]
    public float autoArrivalThreshold = 0.08f;

    [Tooltip("Wait until the player is grounded before starting auto-move.")]
    public bool waitForGroundBeforeMove = true;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onStartAutoMove;
    public UnityEngine.Events.UnityEvent onReachCenter;

    // Local flag to avoid spamming 'blocked' SFX (since base has its own private flag)
    private bool localBlockedPlayed = false;

    protected override void Update()
    {
        if (inUse) return;

        // Ensure player reference (mirrors base Door’s fetch pattern)
        if (player == null)
        {
            var mm = GameManagerRefactored.Instance.GetSystem<PlayerRegistry>()?.GetPlayer(0);
            if (mm) player = mm.gameObject;
            if (player == null) return;
        }

        var mmScript = player.GetComponent<MarioMovement>();
        if (!mmScript) return;

        // Only react when the player is “at” the door and presses up
        if (PlayerAtDoor(mmScript))
        {
            if (mmScript.moveInput.y > 0.5f)
            {
                if (locked)
                {
                    if (CheckForKey())
                    {
                        inUse = true;
                        StartCoroutine(UnlockThenOpen_AutoCentered());
                    }
                    else if (!localBlockedPlayed)
                    {
                        audioSource.PlayOneShot(blockedSound);
                        animator.SetTrigger("Blocked");
                        localBlockedPlayed = true;
                    }
                }
                else
                {
                    inUse = true;
                    StartCoroutine(CenterThenOpen_Auto());
                }
            }
            else
            {
                localBlockedPlayed = false;
            }
        }
    }

    private IEnumerator UnlockThenOpen_AutoCentered()
    {
        // Spend key & sound now
        locked = false;
        SpendKey();
        audioSource.PlayOneShot(unlockSound);

        // Walk to the center like Axe auto-move
        yield return MovePlayerToDoorCenterWithVelocity();

        // Freeze and show unlock VFX before opening
        FreezePlayer();
        animator.SetTrigger("Unlock");
        yield return new WaitForSeconds(unlockTime);

        // Then open and finish like the base timing
        yield return DoOpenSequenceAfterCentered();
    }

    private IEnumerator CenterThenOpen_Auto()
    {
        yield return MovePlayerToDoorCenterWithVelocity();
        yield return DoOpenSequenceAfterCentered();
    }

    /// <summary>
    /// Mirrors the Axe auto-move style:
    /// Disable input -> Unfreeze physics -> wait grounded -> drive velocity to center -> stop -> event.
    /// </summary>
    private IEnumerator MovePlayerToDoorCenterWithVelocity()
    {
        if (!player) yield break;

        var rb = player.GetComponent<Rigidbody2D>();
        var mm = player.GetComponent<MarioMovement>();
        if (!rb || !mm) yield break;

        // Hand-off control to the door
        mm.DisableInputs();
        mm.Unfreeze();                   // allow physics & velocity to apply
        rb.velocity = new Vector2(0f, rb.velocity.y);

        // Optional: wait for ground, like the Axe logic
        if (waitForGroundBeforeMove)
        {
            // Safety timeout after 1.5s in case ground is never achieved
            float t = 0f;
            while (!mm.onGround && t < 1.5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        onStartAutoMove?.Invoke();

        float targetX = transform.position.x;
        float dir = Mathf.Sign(targetX - player.transform.position.x);

        // Flip sprite to face the motion direction (same idea as Axe’s FlipTo)
        mm.FlipTo(dir > 0);

        // Drive horizontal velocity until close enough
        while (Mathf.Abs(player.transform.position.x - targetX) > autoArrivalThreshold)
        {
            // Keep vertical velocity intact
            rb.velocity = new Vector2(dir * autoMoveSpeed, rb.velocity.y);

            // If we crossed the target (possible with high speed), snap out
            if (Mathf.Sign(targetX - player.transform.position.x) != dir)
                break;

            yield return null;
        }

        // Snap to exact center on X and stop horizontal motion
        player.transform.position = new Vector3(targetX, player.transform.position.y, player.transform.position.z);
        rb.velocity = new Vector2(0f, rb.velocity.y);

        onReachCenter?.Invoke();

        // We don't re-enable input here; the open sequence will Freeze, then Unfreeze+Enable later.
    }

    private IEnumerator DoOpenSequenceAfterCentered()
    {
        // Freeze during the open animation (mirrors base)
        FreezePlayer();

        audioSource.PlayOneShot(openSound);
        animator.SetTrigger("Open");

        if (blackFade)
            blackFade.GetComponent<Animator>().SetTrigger("Fade");

        if (otherDoor)
        {
            var bd = otherDoor as BigDoor;
            if (bd != null)
            {
                bd.inUse = true;
            }
            destination.GetComponent<Animator>().SetTrigger("Open");
        }

        // Match base delay before teleport
        yield return new WaitForSeconds(0.5f);
        TeleportAndRelease();

        // Close doors like base after 1s from open
        Invoke(nameof(Close), 1f);
        if (otherDoor) otherDoor.Invoke(nameof(Close), 1f);
    }

    private void TeleportAndRelease()
    {
        if (!player) return;

        if (otherDoor)
        {
            player.transform.position = destination.transform.position;

            var mm = player.GetComponent<MarioMovement>();
            if (mm && mm.powerupState == PowerupState.small)
            {
                player.transform.position -= new Vector3(0, 0.5f, 0);
            }
        }

        // Snap camera if configured
        var camPos = Camera.main.transform.position;
        if (snapCameraX) camPos.x = player.transform.position.x;
        if (snapCameraY) camPos.y = player.transform.position.y;
        Camera.main.transform.position = camPos;

        // Give control back
        UnfreezePlayer();
        var movement = player.GetComponent<MarioMovement>();
        if (movement) movement.EnableInputs();
    }
}