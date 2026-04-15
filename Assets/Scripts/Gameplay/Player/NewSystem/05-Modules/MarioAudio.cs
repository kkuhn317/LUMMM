using UnityEngine;

/// <summary>
/// Owns all Mario sound effect playback.
/// Subscribes to MarioEvents — never polls MarioState.
///
/// One AudioSource per Mario (assigned in Inspector).
/// All clips are serialized fields so designers can swap them without code changes.
/// </summary>
[RequireComponent(typeof(MarioCore))]
[RequireComponent(typeof(AudioSource))]
public class MarioAudio : MonoBehaviour
{
    // ─── Audio Clips ─────────────────────────────────────────────────────────

    [Header("Movement")]
    public AudioClip SkidSound;

    [Header("Jump")]
    public AudioClip JumpSound;
    public AudioClip SpinJumpSound;
    public AudioClip SpinJumpBounceSound;
    public AudioClip SpinJumpPoofSound;
    public AudioClip WallJumpSound;
    public AudioClip MidAirSpinSound;

    [Header("Ground Pound")]
    public AudioClip GroundPoundSound;
    public AudioClip GroundPoundLandSound;

    [Header("Wall")]
    public AudioClip BonkSound;

    [Header("Swimming")]
    public AudioClip SwimSound;
    public AudioClip WaterEntrySound;
    public AudioClip WaterExitSound;

    [Header("Damage / Death")]
    public AudioClip DamageSound;

    [Header("Carry")]
    public AudioClip PickupSound;
    public AudioClip DropSound;
    public AudioClip ThrowSound;

    [Header("Cape")]
    public AudioClip CapePrepareAttackSound;
    public AudioClip CapeSound;

    // ─── References ──────────────────────────────────────────────────────────

    private MarioCore   _core;
    private AudioSource _audio;
    private int         PlayerIndex => _core.PlayerIndex;

    private void Awake()
    {
        _core  = GetComponent<MarioCore>();
        _audio = GetComponent<AudioSource>();
    }

    private void OnEnable()  => SubscribeEvents();
    private void OnDisable() => UnsubscribeEvents();

    private void SubscribeEvents()
    {
        MarioEvents.OnJumped             += OnJumped;
        MarioEvents.OnSpinJumped         += OnSpinJumped;
        MarioEvents.OnSpinJumpBounced    += OnSpinJumpBounced;
        MarioEvents.OnSpinJumpPoofed     += OnSpinJumpPoofed;
        MarioEvents.OnWallJumped         += OnWallJumped;
        MarioEvents.OnMidairSpinStarted  += OnMidairSpinStarted;
        MarioEvents.OnGroundPoundStarted += OnGroundPoundStarted;
        MarioEvents.OnGroundPoundLanded  += OnGroundPoundLanded;
        MarioEvents.OnBonked             += OnBonked;
        MarioEvents.OnSkidStarted        += OnSkidStarted;
        MarioEvents.OnSwam               += OnSwam;
        MarioEvents.OnEnteredWater       += OnEnteredWater;
        MarioEvents.OnExitedWater        += OnExitedWater;
        MarioEvents.OnDamaged            += OnDamaged;
        MarioEvents.OnPickedUp           += OnPickedUp;
        MarioEvents.OnDropped            += OnDropped;
        MarioEvents.OnThrown             += OnThrown;
        MarioEvents.OnCapeAttackStarted  += OnCapeAttackStarted;
        MarioEvents.OnCapeAttackSwung    += OnCapeAttackSwung;
    }

    private void UnsubscribeEvents()
    {
        MarioEvents.OnJumped             -= OnJumped;
        MarioEvents.OnSpinJumped         -= OnSpinJumped;
        MarioEvents.OnSpinJumpBounced    -= OnSpinJumpBounced;
        MarioEvents.OnSpinJumpPoofed     -= OnSpinJumpPoofed;
        MarioEvents.OnWallJumped         -= OnWallJumped;
        MarioEvents.OnMidairSpinStarted  -= OnMidairSpinStarted;
        MarioEvents.OnGroundPoundStarted -= OnGroundPoundStarted;
        MarioEvents.OnGroundPoundLanded  -= OnGroundPoundLanded;
        MarioEvents.OnBonked             -= OnBonked;
        MarioEvents.OnSkidStarted        -= OnSkidStarted;
        MarioEvents.OnSwam               -= OnSwam;
        MarioEvents.OnEnteredWater       -= OnEnteredWater;
        MarioEvents.OnExitedWater        -= OnExitedWater;
        MarioEvents.OnDamaged            -= OnDamaged;
        MarioEvents.OnPickedUp           -= OnPickedUp;
        MarioEvents.OnDropped            -= OnDropped;
        MarioEvents.OnThrown             -= OnThrown;
        MarioEvents.OnCapeAttackStarted  -= OnCapeAttackStarted;
        MarioEvents.OnCapeAttackSwung    -= OnCapeAttackSwung;
    }

    // ─── Event Handlers ──────────────────────────────────────────────────────

    private void OnJumped(int i)             { if (i != PlayerIndex) return; Play(JumpSound); }
    private void OnSpinJumped(int i)         { if (i != PlayerIndex) return; Play(SpinJumpSound); }
    private void OnSpinJumpBounced(int i)    { if (i != PlayerIndex) return; Play(SpinJumpBounceSound); }
    private void OnSpinJumpPoofed(int i, UnityEngine.Vector3 _) { if (i != PlayerIndex) return; Play(SpinJumpPoofSound); }
    private void OnWallJumped(int i)         { if (i != PlayerIndex) return; Play(WallJumpSound ?? JumpSound); }
    private void OnMidairSpinStarted(int i)  { if (i != PlayerIndex) return; Play(MidAirSpinSound); }
    private void OnGroundPoundStarted(int i) { if (i != PlayerIndex) return; Play(GroundPoundSound); }
    private void OnBonked(int i)             { if (i != PlayerIndex) return; Play(BonkSound, 0.5f); }
    private void OnSkidStarted(int i)        { if (i != PlayerIndex) return; Play(SkidSound); }
    private void OnSwam(int i)               { if (i != PlayerIndex) return; Play(SwimSound); }
    private void OnEnteredWater(int i)       { if (i != PlayerIndex) return; Play(WaterEntrySound); }
    private void OnExitedWater(int i)        { if (i != PlayerIndex) return; Play(WaterExitSound); }
    private void OnDamaged(int i)            { if (i != PlayerIndex) return; Play(DamageSound); }
    private void OnPickedUp(int i, ObjectPhysics o)  { if (i != PlayerIndex) return; Play(PickupSound); }
    private void OnDropped(int i, ObjectPhysics o)   { if (i != PlayerIndex) return; Play(DropSound); }
    private void OnThrown(int i, ObjectPhysics o)    { if (i != PlayerIndex) return; Play(ThrowSound); }
    private void OnCapeAttackStarted(int i)  { if (i != PlayerIndex) return; Play(CapePrepareAttackSound); }
    private void OnCapeAttackSwung(int i)    { if (i != PlayerIndex) return; Play(CapeSound); }

    private void OnGroundPoundLanded(int i, GameObject obj)
    {
        if (i != PlayerIndex) return;
        Play(GroundPoundLandSound);
    }

    // ─── Public ──────────────────────────────────────────────────────────────

    public void PlayDamageSound()
    {
        if (DamageSound != null)
            AudioManager.Instance?.Play(DamageSound, SoundCategory.SFX);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null) _audio.PlayOneShot(clip, volume);
    }
}