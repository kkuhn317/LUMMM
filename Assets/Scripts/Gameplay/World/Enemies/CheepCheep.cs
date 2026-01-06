using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CheepCheep : EnemyAI
{
    [Header("Cheep Cheep")]
    public float swimSpeed = 2f;
    public float groundSpeed = 1f;
    public float chaseSpeed = 2f;
    [Range(0f, 1f)]
    public float verticalChaseMultiplier = 0.65f;
    public float followRadius = 6f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.5f;
    public float squashFactor = 0.6f;
    public float squashSpeed = 10f;
    public float jumpForceOutOfWater = 5f;
    public float rotationSpeed = 200f;

    [Header("Rotation Limits")]
    [Range(0, 90)] [SerializeField] private float maxAimPitchDeg = 60f; // max up/down from forward 
    [SerializeField] private float aimHysteresisDeg = 3f; // small buffer to avoid edge jitter

    [Header("Bubbles")]
    public GameObject bubble;
    public GameObject dashParticles;
    public float bubbleIntervalSeconds = 3f;
    private float nextBubbleTime = -1f;

    [Header("Facing Smoothing")]
    [SerializeField] private float faceDeadZone = 0.35f;
    [SerializeField] private float faceHysteresis = 0.15f;
    [SerializeField] private float minFlipInterval = 0.20f;

    private float lastFlipTime = -999f;
    private int facingSign = 0;

    // runtime flags
    private bool isRotating = false;
    private bool isChasing = false;
    private bool isCelebrating = false;

    // chase/scripted
    private Transform target;
    private bool isChaseSequenceRunning = false;
    private Coroutine chaseSequenceCo = null;

    private bool isScriptedChasing = false;
    private Coroutine scriptedChaseCo = null;

    // rotate->scripted handoff
    private bool forceScriptedAfterRotate = false;
    private Transform forcedWaypoint = null;
    private PlayableDirector forcedDirector = null;
    private float forcedStopRadius = 0.2f;

    [Header("Scripted Chase")]
    public Transform scriptedWaypoint;
    public PlayableDirector scriptedDirector;
    public float scriptedStopRadius = 0.2f;
    private bool scriptedChaseExecuted = false;

    [Header("Chase fail-safe")]
    public float wallProbeDistance = 0.25f;
    public float wallGiveUpTime = 1.0f;
    public float reengageCooldown = 1.5f;
    private float blockedTimer = 0f;
    private float reengageUntil = -1f;
    private bool edgeHovering = false;
    private Coroutine edgeHoverCo = null;

    // visuals, physics
    private Vector3 originalScale;
    public Transform pivot;
    [SerializeField] private SpriteRenderer sprite;
    private SpriteRenderer Sprite => sprite ? sprite : GetComponentInChildren<SpriteRenderer>(true);
    [SerializeField] private Animator animator;
    private bool suppressBounce = false;
    private Coroutine angryCo;

    private void OnDisable()
    {
        suppressBounce = false;
        SuppressRaycastFlip(false);
        StopBubbles();
    }

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
        flipObject = false; // CheepCheep uses our own visual flip, not scale-flip
    }

    private bool IsBusy => isRotating || isChaseSequenceRunning || isScriptedChasing;

    private void SetVisualFacing()
    {
        var sr = Sprite;
        if (Sprite) Sprite.flipX = movingLeft;
    }

    private void SuppressRaycastFlip(bool on) => ignoreRaycastFlip = on;

    private void SetZeroVelocity()
    {
        velocity = Vector2.zero;
        if (rb) rb.velocity = Vector2.zero;
    }

    private void BeginCutscene()
    {
        SuppressRaycastFlip(true);
        suppressBounce = true;
        SetZeroVelocity();
    }

    private void EndCutscene()
    {
        suppressBounce = false;
        SuppressRaycastFlip(false);
    }

    private void StartChaseBubbles()
    {
        nextBubbleTime = (isInWater && bubble) ? Time.time + bubbleIntervalSeconds : -1f;
    }

    private void StopBubbles()
    {
        nextBubbleTime = -1f;
    }

    private void TickBubbles()
    {
        if (nextBubbleTime < 0f) return;
        if (!isInWater || !bubble)
        {
            nextBubbleTime = -1f;
            return;
        }

        if (Time.time >= nextBubbleTime)
        {
            Instantiate(bubble, transform.position, Quaternion.identity);
            nextBubbleTime = Time.time + bubbleIntervalSeconds;
        }
    }

    protected override void Update()
    {
        // Celebrate state
        if (isCelebrating)
        {
            if (isInWater)
            {
                SetZeroVelocity();
                gravity = 0f;
                SetVisualFacing();
                return;
            }
            else
            {
                stopAfterLand = true;
                ApplyGravity();
                base.Update();

                if (objectState == ObjectState.grounded)
                    velocity.x = 0f;

                SetVisualFacing();
                return;
            }
        }

        base.Update();

        if (isRotating)
        {
            velocity = Vector2.zero;
            SetVisualFacing();
            return;
        }

        if (target && Vector3.Distance(transform.position, target.position) > followRadius)
            StopChasing();

        if (isChasing && target)
            UpdateFacingToward(target.position);

        SetVisualFacing();

        if (isChasing && target)
        {
            Chase(target);
            TickBubbles();
        }

        if (isChasing && target)
        {
            if (Time.time < reengageUntil)
            {
                StopChasing();
                EnterEdgeHover();
            }
            else
            {
                if (IsHardBlocked())
                    blockedTimer += Time.deltaTime;
                else
                    blockedTimer = 0f;

                if (blockedTimer >= wallGiveUpTime)
                {
                    blockedTimer = 0f;
                    reengageUntil = Time.time + reengageCooldown;

                    StopChasing();
                    EnterEdgeHover();
                }
            }
        }

        if (isInWater && !isChasing && !isScriptedChasing && !edgeHovering)
        {
            velocity.x = swimSpeed;
            SwimAndBob();
        }
        else if (objectState == ObjectState.grounded || objectState == ObjectState.falling)
        {
            ApplyGravity();

            if (objectState == ObjectState.grounded)
            {
                velocity.x = groundSpeed;
                Bounce();
            }
        }
    }

    private bool IsHardBlocked()
    {
        float dir = movingLeft ? -1f : 1f;
        Vector2 c = (Vector2)transform.position + boundsOffset;
        float halfW = Mathf.Max(0f, width + sizePadding.x) * 0.5f;
        Vector2 origin = new Vector2(c.x + dir * (halfW - 0.01f), c.y);
        return Physics2D.Raycast(origin, new Vector2(dir, 0f), wallProbeDistance, wallMask);
    }

    private void EnterEdgeHover()
    {
        if (edgeHovering) return;
        edgeHovering = true;

        SetZeroVelocity();
        gravity = 0f;

        if (edgeHoverCo != null) StopCoroutine(edgeHoverCo);
        edgeHoverCo = StartCoroutine(EdgeHoverCR());
    }

    private IEnumerator EdgeHoverCR()
    {
        while (edgeHovering && Time.time < reengageUntil)
        {
            if (target) UpdateFacingToward(target.position);
            velocity.x = 0f;
            velocity.y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            yield return null;
        }

        edgeHovering = false;
        edgeHoverCo = null;
    }

    protected override void onTouchWall(GameObject other)
    {
        if (isChasing)
        {
            velocity.x = 0f;
            return;
        }

        if (bounceOffWalls)
        {
            if (!ignoreRaycastFlip)
                movingLeft = !movingLeft;
        }
        else
        {
            velocity.x = 0;
        }
    }

    private void UpdateFacingToward(Vector3 targetPos)
    {
        float dx = targetPos.x - transform.position.x;

        if (Mathf.Abs(dx) <= faceDeadZone) return;

        float boundary = faceDeadZone + faceHysteresis;
        bool cooldownOK = (Time.time - lastFlipTime) >= minFlipInterval;

        if (facingSign == 0 && Mathf.Abs(dx) > faceDeadZone)
        {
            facingSign = (dx < 0f) ? -1 : +1;
            movingLeft = (facingSign < 0);
            lastFlipTime = Time.time;
            return;
        }

        if (Mathf.Abs(dx) >= boundary && cooldownOK)
        {
            facingSign = (dx < 0f) ? -1 : +1;
            movingLeft = (facingSign < 0);
            lastFlipTime = Time.time;
        }
    }

    public void StartChasing(Transform playerTransform)
    {
        if (isChasing || IsBusy || isCelebrating) return;

        isChaseSequenceRunning = true;
        target = playerTransform;

        if (target) UpdateFacingToward(target.position);
        SetVisualFacing();

        StartChaseBubbles();

        chaseSequenceCo = StartCoroutine(RotateAndChase());
    }

    private IEnumerator RotateAndChase()
    {
        isRotating = true;
        SetZeroVelocity();

        if (animator) animator.SetTrigger("shocked");
        yield return null;
        if (animator) yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("shocked"));

        if (target) UpdateFacingToward(target.position);

        float targetAngle = 0f;
        if (target && pivot)
        {
            targetAngle = GetCappedPitchDeg(target.position);
        }

        while (animator && animator.GetCurrentAnimatorStateInfo(0).IsName("shocked") &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            if (pivot)
            {
                float z = Mathf.MoveTowardsAngle(pivot.eulerAngles.z, targetAngle, rotationSpeed * Time.deltaTime);
                pivot.rotation = Quaternion.Euler(0f, 0f, z);
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        if (animator) animator.SetTrigger("angry");
        if (pivot) pivot.rotation = Quaternion.identity;
        yield return null;
        if (animator)
        {
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("angry"));
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
            animator.SetTrigger("normal");
        }

        isRotating = false;

        if (forceScriptedAfterRotate && forcedWaypoint)
        {
            if (scriptedChaseCo != null) StopCoroutine(scriptedChaseCo);
            scriptedChaseCo = StartCoroutine(ScriptedChaseCR(forcedWaypoint, forcedDirector, forcedStopRadius));

            forceScriptedAfterRotate = false;
            forcedWaypoint = null;
            forcedDirector = null;

            isChaseSequenceRunning = false;
            chaseSequenceCo = null;
            yield break;
        }

        isChasing = true;
        isChaseSequenceRunning = false;
        chaseSequenceCo = null;
    }

    // Signed pitch in degrees, clamped to ±maxAimPitchDeg.
    // Handles flipX by inverting the sign when movingLeft.
    private float GetCappedPitchDeg(Vector3 worldTarget)
    {
        if (!pivot) return 0f;

        Vector2 to = (Vector2)(worldTarget - pivot.position);
        if (to.sqrMagnitude < Mathf.Epsilon) return 0f;

        bool left = movingLeft;

        // Mirror so forward is always +X for the measurement
        if (left) to.x = -to.x;

        // Measure signed pitch relative to +X, clamp it
        float pitch = Vector2.SignedAngle(Vector2.right, to.normalized); // [-180, 180]
        pitch = Mathf.Clamp(pitch, -maxAimPitchDeg, +maxAimPitchDeg); // example: ±60

        // flipX (negative X scale) reverses visible Z rotation
        if (left) pitch = -pitch;

        return pitch; // apply directly to pivot.localRotation.z
    }

    private void Chase(Transform player)
    {
        if (!player) return;

        UpdateFacingToward(player.position);

        Vector2 to = (Vector2)(player.position - transform.position);
        float dist = to.magnitude;
        if (dist < 0.001f) return;

        Vector2 dir = to.normalized;

        if (isInWater)
        {
            gravity = 0f;
            velocity.x = Mathf.Abs(dir.x) * chaseSpeed;
            velocity.y = dir.y * chaseSpeed * verticalChaseMultiplier;

            if (player.position.y > transform.position.y + 0.5f &&
                objectState == ObjectState.grounded)
            {
                JumpOutOfWater();
            }
        }
        else
        {
            velocity.x = chaseSpeed;
        }
    }

    public void StopChasing()
    {
        if (isRotating || isChaseSequenceRunning)
        {
            if (chaseSequenceCo != null)
            {
                StopCoroutine(chaseSequenceCo);
                animator?.SetTrigger("normal");
            }
            chaseSequenceCo = null;
            isRotating = false;
            isChaseSequenceRunning = false;
            if (pivot) pivot.rotation = Quaternion.identity;
        }

        target = null;
        isChasing = false;

        // cancel edge hover if active
        if (edgeHovering)
        {
            edgeHovering = false;
            if (edgeHoverCo != null) StopCoroutine(edgeHoverCo);
            edgeHoverCo = null;
        }

        if (isInWater) velocity.x = 0;

        // stop bubbles when chase ends
        StopBubbles();
    }

    public void SetGroundSpeed(float newGroundSpeed)
    {
        groundSpeed = newGroundSpeed;
    }

    public void ChangeGroundSpeedEvent(float newGroundSpeed)
    {
        SetGroundSpeed(newGroundSpeed);
        if (angryCo != null) StopCoroutine(angryCo);
        angryCo = StartCoroutine(AngryFlash());
    }

    private IEnumerator AngryFlash()
    {
        // Enter angry
        animator.ResetTrigger("normal");
        animator.SetTrigger("angry");
        
        yield return null;
        yield return new WaitForSeconds(0.5f);

        // Exit angry
        animator.ResetTrigger("angry");
        animator.SetTrigger("normal");

        angryCo = null;
    }

    public void ShowDashParticles()
    {
        Instantiate(dashParticles, transform.position, Quaternion.identity);
    }

    public void Celebrate()
    {
        isCelebrating = true;
        StopChasing();

        if (isInWater) gravity = 0f;
        if (pivot) pivot.rotation = Quaternion.identity;
        StartCoroutine(StartCelebrate());
    }

    private IEnumerator StartCelebrate()
    {
        if (isInWater)
        {
            yield return new WaitForSeconds(0.25f);
            animator?.SetTrigger("happy");
        }
        else
        {
            yield return new WaitUntil(() => objectState == ObjectState.grounded);
            animator?.SetTrigger("happy");
        }
    }

    public void QueueScriptedChase()
    {
        bool playerHasKey = GameManager.Instance != null &&
                            GameManager.Instance.keys != null &&
                            GameManager.Instance.keys.Count > 0;

        if (playerHasKey)
        {
            Debug.Log($"{name}: Skipping scripted chase — player has a key.");
            return;
        }

        if (scriptedChaseExecuted)
        {
            Debug.Log($"{name}: Scripted chase already executed, skipping.");
            return;
        }

        scriptedChaseExecuted = true;
        QueueScriptedChase(scriptedWaypoint, scriptedDirector, scriptedStopRadius);
    }

    public void QueueScriptedChase(Transform point, PlayableDirector director, float stopRadius = 0.2f)
    {
        if (!point)
        {
            Debug.LogWarning($"{name}: QueueScriptedChase called with null waypoint.");
            return;
        }

        // handoff rules
        if (isRotating || isChaseSequenceRunning)
        {
            SuppressRaycastFlip(true);
            forceScriptedAfterRotate = true;
            forcedWaypoint = point;
            forcedDirector = director;
            forcedStopRadius = stopRadius;
            return;
        }

        if (isScriptedChasing)
        {
            SuppressRaycastFlip(true);
            if (scriptedChaseCo != null) StopCoroutine(scriptedChaseCo);
            isScriptedChasing = false;
            scriptedChaseCo = StartCoroutine(ScriptedChaseCR(point, director, stopRadius));
            return;
        }

        if (isChasing)
        {
            SuppressRaycastFlip(true);
            StopChasing();
            if (scriptedChaseCo != null) StopCoroutine(scriptedChaseCo);
            scriptedChaseCo = StartCoroutine(ScriptedChaseCR(point, director, stopRadius));
            return;
        }

        // idle: do rotate first, then scripted
        SuppressRaycastFlip(true);
        forceScriptedAfterRotate = true;
        forcedWaypoint = point;
        forcedDirector = director;
        forcedStopRadius = stopRadius;

        isChaseSequenceRunning = true;
        if (chaseSequenceCo != null) StopCoroutine(chaseSequenceCo);
        chaseSequenceCo = StartCoroutine(RotateAndChase());
    }

    private IEnumerator ScriptedChaseCR(Transform waypoint, PlayableDirector director, float stopRadius)
    {
        isScriptedChasing = true;
        isChasing = false;
        target = null;
        isRotating = false;

        SuppressRaycastFlip(true);
        SetZeroVelocity();

        // start bubbles for scripted chase
        StartChaseBubbles();

        // move toward waypoint
        while (waypoint)
        {
            Vector2 to = (Vector2)(waypoint.position - transform.position);
            if (to.magnitude <= stopRadius) break;

            UpdateFacingToward(waypoint.position);
            Vector2 dir = to.normalized;

            if (isInWater)
            {
                gravity = 0f;
                velocity.x = Mathf.Abs(dir.x) * chaseSpeed;
                velocity.y = dir.y * chaseSpeed * verticalChaseMultiplier;
            }
            else
            {
                velocity.x = chaseSpeed;
            }

            // bubble tick while scripted chasing
            TickBubbles();

            yield return null;
        }

        SetZeroVelocity();

        // stop bubbles once we arrive
        StopBubbles();

        // play timeline with gameplay disabled, then restore
        yield return PlayDirectorWithScriptsMuted(director);

        SuppressRaycastFlip(false);

        isScriptedChasing = false;
        scriptedChaseCo = null;
    }

    private IEnumerator PlayDirectorWithScriptsMuted(PlayableDirector director)
    {
        if (!director) yield break;

        // collect
        var behaviours  = GetComponentsInChildren<Behaviour>(true);
        var colliders2D = GetComponentsInChildren<Collider2D>(true);
        var rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);

        // capture
        var prevBeh = new List<(Behaviour b, bool wasEnabled)>(behaviours.Length);
        var prevCol = new List<(Collider2D c, bool wasEnabled)>(colliders2D.Length);
        var prevRb  = new List<(Rigidbody2D r, bool wasSim)>(rigidbodies.Length);

        foreach (var b in behaviours)  if (b) prevBeh.Add((b, b.enabled));
        foreach (var c in colliders2D)  if (c) prevCol.Add((c, c.enabled));
        foreach (var r in rigidbodies)  if (r) prevRb.Add((r, r.simulated));

        // prep
        if (pivot) pivot.rotation = Quaternion.identity;
        animator?.SetTrigger("normal");

        BeginCutscene();

        // disable gameplay (keep this script, Animator, and Director)
        foreach (var b in behaviours)
        {
            if (!b) continue;
            bool keep = ReferenceEquals(b, this) || b is Animator || ReferenceEquals(b, director);
            if (!keep) b.enabled = false;
        }
        foreach (var c in colliders2D) if (c) c.enabled = false;
        foreach (var r in rigidbodies) if (r) r.simulated = false;

        // play
        director.Play();
        while (director.state == PlayState.Playing) yield return null;

        // restore
        foreach (var t in prevBeh) if (t.b) t.b.enabled = t.wasEnabled;
        foreach (var t in prevCol) if (t.c) t.c.enabled = t.wasEnabled;
        foreach (var t in prevRb)  if (t.r) t.r.simulated = t.wasSim;

        EndCutscene();

        Debug.Log($"{name}: PlayableDirector finished, components restored.");
    }

    private void SwimAndBob()
    {
        velocity.y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        gravity = 0f;
    }

    private void ApplyGravity() => gravity = 20f;

    private void Bounce()
    {
        if (objectState == ObjectState.grounded && velocity.y <= 0 && !isCelebrating && !suppressBounce)
        {
            StartCoroutine(SquashAndBounce());
            velocity.y = bounceHeight;
            objectState = ObjectState.falling;
        }
    }

    private IEnumerator SquashAndBounce()
    {
        float t = 0f;
        Vector3 squashed = new Vector3(originalScale.x, originalScale.y * squashFactor, originalScale.z);

        while (t < 1f)
        {
            t += Time.deltaTime * squashSpeed;
            transform.localScale = Vector3.Lerp(originalScale, squashed, t);
            yield return null;
        }

        while (velocity.y <= 0) yield return null;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * squashSpeed;
            transform.localScale = Vector3.Lerp(squashed, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void JumpOutOfWater()
    {
        velocity.y = jumpForceOutOfWater;
        objectState = ObjectState.falling;
    }

    private void OnDrawGizmos()
    {
        if (isChasing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, followRadius);

            if (target)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}