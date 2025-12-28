using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Waterfall : MonoBehaviour
{
    [Header("Physics")]
    public float pushForce = 20f;

    [Header("Fall-off")]
    public float fallSpeed = 8f;
    public float extraMargin = 1f;
    public Transform fallBelowTarget; // optional

    [Header("Phase target (0..1 inside the tile)")]
    [Range(0f, 1f)] public float targetPhase = 0.0f;   // set this so the tile "ends" on that part
    public float phaseTolerance = 0.01f;                // how close we need to be
    public bool pollScrollSpeedEveryFrame = false;      // if something else animates _ScrollSpeed

    private SpriteRenderer sr;
    private BoxCollider2D col;

    private static readonly int ScrollSpeedID = Shader.PropertyToID("_ScrollSpeed");
    private MaterialPropertyBlock mpb;
    private Vector4 originalScroll;

    private bool isFalling;

    // UV scroll tracking
    private float uvOffsetY; // accumulated offset (matches shader’s _Time * _ScrollSpeed.y)
    private float scrollY;   // current _ScrollSpeed.y

    private enum ArmMode { None, OnPhase }
    private ArmMode armMode = ArmMode.None;

    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);

        if (sr.sharedMaterial && sr.sharedMaterial.HasProperty(ScrollSpeedID))
            originalScroll = sr.sharedMaterial.GetVector(ScrollSpeedID);
        else
            originalScroll = mpb.GetVector(ScrollSpeedID);

        scrollY   = originalScroll.y;
        uvOffsetY = 0f; // start phase
    }

    void Update()
    {
        // Wait for target phase (only when armed and not yet falling)
        if (!isFalling && armMode == ArmMode.OnPhase)
        {
            if (pollScrollSpeedEveryFrame)
            {
                sr.GetPropertyBlock(mpb);
                var v = mpb.GetVector(ScrollSpeedID);
                if (v == Vector4.zero && sr.sharedMaterial && sr.sharedMaterial.HasProperty(ScrollSpeedID))
                    v = sr.sharedMaterial.GetVector(ScrollSpeedID);
                scrollY = v.y;
            }

            // If not scrolling, drain now
            if (Mathf.Approximately(scrollY, 0f))
            {
                TriggerDrainNow();
                return;
            }

            // Advance our local UV phase
            uvOffsetY += scrollY * Time.deltaTime;
            float frac = Mathf.Repeat(uvOffsetY, 1f);

            // Detect crossing the target phase (direction-aware)
            bool hit = false;
            if (scrollY > 0f)
            {
                // going up: we "hit" when frac >= targetPhase
                hit = (frac >= targetPhase);
            }
            else
            {
                // going down: we "hit" when frac <= targetPhase
                hit = (frac <= targetPhase);
            }

            // Optional tighter check with tolerance:
            if (hit)
            {
                float dist = Mathf.Abs(Mathf.DeltaAngle(frac * 360f, targetPhase * 360f)) / 360f;
                if (dist <= phaseTolerance) // close enough
                {
                    TriggerDrainNow();
                    return;
                }
                // If you want exact visual alignment without tolerance,
                // just remove the tolerance block and keep `if (hit) TriggerDrainNow();`
            }
        }

        if (!isFalling) return;

        // Fall-off movement
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

        float bottomY;
        if (fallBelowTarget != null)
            bottomY = fallBelowTarget.position.y;
        else if (Camera.main != null)
            bottomY = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).y - extraMargin;
        else
            bottomY = transform.position.y - 50f;

        if (sr.bounds.max.y < bottomY)
        {
            col.enabled = false;
            sr.enabled  = false;
            enabled     = false;
        }
    }

    // Arm to drain when the tile reaches a specific internal phase (0..1)
    public void DrainOnPhase(float phase)
    {
        if (isFalling) return;
        targetPhase = Mathf.Repeat(phase, 1f);
        armMode = ArmMode.OnPhase;
    }

    // Keep your immediate drain too, if you need it
    public void DrainWaterfall()
    {
        if (isFalling) return;
        TriggerDrainNow();
    }

    public void RestoreScroll()
    {
        isFalling = false;
        armMode   = ArmMode.None;
        col.enabled = true;
        sr.enabled  = true;

        sr.GetPropertyBlock(mpb);
        mpb.SetVector(ScrollSpeedID, originalScroll);
        sr.SetPropertyBlock(mpb);

        uvOffsetY = 0f;
        scrollY   = originalScroll.y;
    }

    void OnTriggerEnter2D(Collider2D c) => OnTriggerStay2D(c);
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var rb = other.attachedRigidbody;
            if (rb != null) rb.AddForce(Vector2.down * pushForce);
        }
    }

    private void TriggerDrainNow()
    {
        // stop the shader scroll instantly (per renderer)
        sr.GetPropertyBlock(mpb);
        mpb.SetVector(ScrollSpeedID, Vector4.zero);
        sr.SetPropertyBlock(mpb);

        isFalling = true;
        armMode   = ArmMode.None;
    }
}