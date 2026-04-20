using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Bridges MarioEvents → Animator and drives the skeletal Visual hierarchy.
///
/// Design rules:
/// - NEVER polls MarioState — only reacts to events.
/// - NEVER calls RequestTransition or touches gameplay state.
/// - All animator parameters set here, nowhere else.
/// - Skeletal pivot overrides (head tilt, arm raise) applied here too.
///
/// Subscribes to events in OnEnable, unsubscribes in OnDisable.
/// This makes it safe for pooling and instantiation.
///
/// Pivot references are optional — if the hierarchy doesn't have them yet,
/// the skeletal overrides are skipped gracefully.
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioAnimatorController : MonoBehaviour
{
    // ─── Inspector References ────────────────────────────────────────────────

    [Header("Animator")]
    [SerializeField] private Animator _animator;

    // We set the animator parameter hashes to avoid string lookups every frame

    private static readonly int H_Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int H_IsRunning = Animator.StringToHash("isRunning");
    private static readonly int H_OnGround = Animator.StringToHash("onGround");
    private static readonly int H_IsSkidding = Animator.StringToHash("isSkidding");
    private static readonly int H_IsCrouching = Animator.StringToHash("isCrouching");
    private static readonly int H_IsCrawling = Animator.StringToHash("isCrawling");
    private static readonly int H_IsDropping = Animator.StringToHash("isDropping");
    private static readonly int H_CancelDropping = Animator.StringToHash("cancelDropping");
    private static readonly int H_IsSpinning    = Animator.StringToHash("isSpinning");
    private static readonly int H_IsWallSliding = Animator.StringToHash("isWallSliding");
    private static readonly int H_IsPushing = Animator.StringToHash("isPushing");
    private static readonly int H_ClimbSpeed = Animator.StringToHash("climbSpeed");
    private static readonly int H_Yeah = Animator.StringToHash("yeah");
    private static readonly int H_Swim = Animator.StringToHash("swim");
    private static readonly int H_EnterWater = Animator.StringToHash("enterWater");
    private static readonly int H_ExitWater = Animator.StringToHash("exitWater");
    private static readonly int H_Grab = Animator.StringToHash("grab");
    private static readonly int H_GrabMethod = Animator.StringToHash("grabMethod");
    private static readonly int H_IsClimbing = Animator.StringToHash("isClimbing");
    private static readonly int H_IsSideClimbing = Animator.StringToHash("isSideClimbing");
    private static readonly int H_isMidAirSpinning = Animator.StringToHash("isMidairSpinning");
    private static readonly int H_Cape = Animator.StringToHash("cape");
    private static readonly int H_IsLookingUp      = Animator.StringToHash("isLookingUp");
    private static readonly int H_LookUpVariant    = Animator.StringToHash("lookUpVariant");
    private static readonly int H_IsScared      = Animator.StringToHash("isScared");
    private static readonly int H_IsWorried     = Animator.StringToHash("isWorried");

    // ─── Runtime State ───────────────────────────────────────────────────────

    private MarioCore  _core;
    private MarioState State       => _core.State;
    private int        PlayerIndex => _core.PlayerIndex;

    // Skeletal override targets
    private bool       _isYeahPlaying;
    private bool       _hasTriggeredYeah;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _core = GetComponent<MarioCore>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        var carry = GetComponent<MarioCarry>();
        if (carry != null)
            _animator.SetInteger(H_GrabMethod, (int)carry.CarryMethod);
    }

    private void OnEnable()  => SubscribeEvents();
    private void OnDisable() => UnsubscribeEvents();

    private void SubscribeEvents()
    {
        MarioEvents.OnStateChanged           += OnStateChanged;
        MarioEvents.OnLanded                 += OnLanded;
        MarioEvents.OnLeftGround             += OnLeftGround;
        MarioEvents.OnJumped                 += OnJumped;
        MarioEvents.OnSpinJumped             += OnSpinJumped;
        MarioEvents.OnMidairSpinStarted      += OnMidairSpinStarted;
        MarioEvents.OnMidairSpinEnded        += OnMidairSpinEnded;
        MarioEvents.OnGroundPoundStarted     += OnGroundPoundStarted;
        MarioEvents.OnGroundPoundFalling     += OnGroundPoundFalling;
        MarioEvents.OnGroundPoundLanded      += OnGroundPoundLanded;
        MarioEvents.OnGroundPoundCancelled   += OnGroundPoundCancelled;
        MarioEvents.OnSkidStarted            += OnSkidStarted;
        MarioEvents.OnSkidEnded              += OnSkidEnded;
        MarioEvents.OnCrouchStarted          += OnCrouchStarted;
        MarioEvents.OnCrouchEnded            += OnCrouchEnded;
        MarioEvents.OnCrawlStarted           += OnCrawlStarted;
        MarioEvents.OnCrawlEnded             += OnCrawlEnded;
        MarioEvents.OnWallSlideStarted       += OnWallSlideStarted;
        MarioEvents.OnWallSlideEnded         += OnWallSlideEnded;
        MarioEvents.OnPickedUp               += OnPickedUp;
        MarioEvents.OnDropped                += OnDropped;
        MarioEvents.OnThrown                 += OnThrown;
        MarioEvents.OnCapeAttackStarted      += OnCapeAttackStarted;
        MarioEvents.OnCapeAttackEnded        += OnCapeAttackEnded;
        MarioEvents.OnSwam                   += OnSwam;
        MarioEvents.OnEnteredWater           += OnEnteredWater;
        MarioEvents.OnExitedWater            += OnExitedWater;
        MarioEvents.OnClimbStarted           += OnClimbStarted;
        MarioEvents.OnClimbEnded             += OnClimbEnded;
        MarioEvents.OnLookUpStarted          += OnLookUpStarted;
        MarioEvents.OnLookUpEnded            += OnLookUpEnded;
        MarioEvents.OnCelebrationStarted            += OnCelebration;
        MarioEvents.OnFlipped                += OnFlipped;
        MarioEvents.OnEmoteStarted           += OnEmoteStarted;
        MarioEvents.OnEmoteEnded             += OnEmoteEnded;
    }

    private void UnsubscribeEvents()
    {
        MarioEvents.OnStateChanged           -= OnStateChanged;
        MarioEvents.OnLanded                 -= OnLanded;
        MarioEvents.OnLeftGround             -= OnLeftGround;
        MarioEvents.OnJumped                 -= OnJumped;
        MarioEvents.OnSpinJumped             -= OnSpinJumped;
        MarioEvents.OnMidairSpinStarted      -= OnMidairSpinStarted;
        MarioEvents.OnMidairSpinEnded        -= OnMidairSpinEnded;
        MarioEvents.OnGroundPoundStarted     -= OnGroundPoundStarted;
        MarioEvents.OnGroundPoundFalling     -= OnGroundPoundFalling;
        MarioEvents.OnGroundPoundLanded      -= OnGroundPoundLanded;
        MarioEvents.OnGroundPoundCancelled   -= OnGroundPoundCancelled;
        MarioEvents.OnSkidStarted            -= OnSkidStarted;
        MarioEvents.OnSkidEnded              -= OnSkidEnded;
        MarioEvents.OnCrouchStarted          -= OnCrouchStarted;
        MarioEvents.OnCrouchEnded            -= OnCrouchEnded;
        MarioEvents.OnCrawlStarted           -= OnCrawlStarted;
        MarioEvents.OnCrawlEnded             -= OnCrawlEnded;
        MarioEvents.OnWallSlideStarted       -= OnWallSlideStarted;
        MarioEvents.OnWallSlideEnded         -= OnWallSlideEnded;
        MarioEvents.OnPickedUp               -= OnPickedUp;
        MarioEvents.OnDropped                -= OnDropped;
        MarioEvents.OnThrown                 -= OnThrown;
        MarioEvents.OnCapeAttackStarted      -= OnCapeAttackStarted;
        MarioEvents.OnCapeAttackEnded        -= OnCapeAttackEnded;
        MarioEvents.OnSwam                   -= OnSwam;
        MarioEvents.OnEnteredWater           -= OnEnteredWater;
        MarioEvents.OnExitedWater            -= OnExitedWater;
        MarioEvents.OnClimbStarted           -= OnClimbStarted;
        MarioEvents.OnClimbEnded             -= OnClimbEnded;
        MarioEvents.OnLookUpStarted          -= OnLookUpStarted;
        MarioEvents.OnLookUpEnded            -= OnLookUpEnded;
        MarioEvents.OnCelebrationStarted            -= OnCelebration;
        MarioEvents.OnFlipped                -= OnFlipped;
        MarioEvents.OnEmoteStarted           -= OnEmoteStarted;
        MarioEvents.OnEmoteEnded             -= OnEmoteEnded;
    }

    public void SetAnimatorSpeed(float speed)
    {
        if (_animator != null)
            _animator.speed = speed;
    }

    public void SetAnimatorEnabled(bool isEnabled)
    {
        if (_animator != null)
            _animator.enabled = isEnabled;
    }

    // ─── Update: Continuous Params + Skeletal Lerp ───────────────────────────

    private void Update()
    {
        if (State.IsPaused)
        {
            _animator.speed = 0f;
            return;
        }
        _animator.speed = 1f;

        UpdateContinuousParams();
    }

    private void UpdateContinuousParams()
    {
        float absVelX = Mathf.Abs(_core.Rb.velocity.x);

        _animator.SetFloat(H_Horizontal,  absVelX * _core.Physics.Config.WalkAnimatorSpeed);
        _animator.SetBool (H_IsRunning,   absVelX > 0.2f);
        _animator.SetBool (H_OnGround,    State.OnGround);
        _animator.SetBool (H_IsPushing,   State.Pushing);
        // While looking up, force isCrouching off every frame — prevents a
        // one-frame crouch flicker when look-up ends regardless of event order.
        if (State.IsLookingUp)
            _animator.SetBool(H_IsCrouching, false);
        _animator.SetBool (H_IsSpinning,       State.Spinning);
        _animator.SetBool (H_isMidAirSpinning,  State.IsMidairSpinning);
        _animator.SetBool (H_IsWallSliding,     State.WallSliding);
        // climbSpeed: normalised 0..1 value used as a speed multiplier on the
        // climb animation state in the Animator Controller. When 0 the clip
        // freezes (idle on climbable); when 1 it plays at full speed.
        // Set this parameter as the "Speed Multiplier" on the climb Animator state.
        if (_core.State.Climbing)
        {
            float refSpeed = _core.State.CurrentClimbable != null
                ? _core.State.CurrentClimbable.climbSpeed
                : 1f;
            float normalised = refSpeed > 0f
                ? Mathf.Clamp01(_core.State.Velocity.magnitude / refSpeed)
                : 0f;
            _animator.SetFloat(H_ClimbSpeed, normalised);
        }
        else
        {
            _animator.SetFloat(H_ClimbSpeed, 0f);
        }
    }

    // ─── Event Handlers ──────────────────────────────────────────────────────

    private void OnStateChanged(int playerIndex, string stateId)
    {
        if (playerIndex != PlayerIndex) return;

        // Locked → play level-up or cutscene animation (driven by animator state machine)
        // The animation system handles the specific clip via state parameters
    }

    private void OnLanded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_OnGround, true);
        // isSpinning and isMidairSpinning are polled every frame from State — no manual clear needed
    }

    private void OnLeftGround(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_OnGround, false);
    }

    private void OnJumped(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_OnGround, false);
    }

    private void OnSpinJumped(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsSpinning, true);
        _animator.SetBool(H_OnGround, false);
    }

    private void OnMidairSpinStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_isMidAirSpinning, true);
    }

    private void OnMidairSpinEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_isMidAirSpinning, false);
    }

    private void OnGroundPoundStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsDropping,     true);
        _animator.SetBool(H_CancelDropping, false);
    }

    private void OnGroundPoundFalling(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsDropping, true);
    }

    private void OnGroundPoundLanded(int playerIndex, GameObject hitObject)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsDropping,     false);
        _animator.SetBool(H_CancelDropping, false);
    }

    private void OnGroundPoundCancelled(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_CancelDropping, true);
        _animator.SetBool(H_IsDropping,     false);
    }

    private void OnSkidStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsSkidding, true);
    }

    private void OnSkidEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsSkidding, false);
    }

    private void OnCrouchStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsCrouching, true);
        _animator.SetBool(H_IsCrawling,  false);
    }

    private void OnCrouchEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsCrouching, false);
        _animator.SetBool(H_IsCrawling,  false);
    }

    private void OnCrawlStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsCrawling,  true);
        _animator.SetBool(H_IsCrouching, true);
    }

    private void OnCrawlEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsCrawling, false);
    }

    private void OnWallSlideStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsSpinning,    false);
        _animator.SetBool(H_IsWallSliding, true);
    }

    private void OnWallSlideEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsWallSliding, false);
    }

    private void OnPickedUp(int playerIndex, ObjectPhysics obj)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetTrigger(H_Grab);
    }

    private void OnDropped(int playerIndex, ObjectPhysics obj)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetTrigger(H_Grab);
    }

    private void OnThrown(int playerIndex, ObjectPhysics obj)
    {
        if (playerIndex != PlayerIndex) return;
        // throw animation trigger — add when animator state is created
    }

    private void OnCapeAttackStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        Debug.Log($"[Animator] Cape bool → TRUE. Stack: {new System.Diagnostics.StackTrace()}");
        _animator.SetBool(H_Cape, true);
    }

    private void OnCapeAttackEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        Debug.Log($"[Animator] Cape bool → FALSE. Stack: {new System.Diagnostics.StackTrace()}");
        _animator.SetBool(H_Cape, false);
    }

    private void OnSwam(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetTrigger(H_Swim);
    }

    private void OnEnteredWater(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetTrigger(H_EnterWater);
        _animator.SetBool(H_IsSpinning, false);
    }

    private void OnExitedWater(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetTrigger(H_ExitWater);
    }

    private void OnClimbStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsDropping,   false);
        _animator.SetBool(H_IsSpinning,   false);
        _animator.SetBool(H_IsCrouching,  false);
        _animator.SetBool(H_IsClimbing,   true);
        // Distinguish ladder (front) from pipe/wall (side) for the animator.
        // ClimbSideState fires ClimbStarted after setting State.Climbing = true
        // and its ID is MarioStateID.ClimbSide, so we read the current state.
        bool isSide = _core.StateMachine.CurrentStateID == MarioStateID.ClimbSide;
        _animator.SetBool(H_IsSideClimbing, isSide);
    }

    private void OnClimbEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsClimbing,     false);
        _animator.SetBool(H_IsSideClimbing, false);
    }

    private void OnLookUpStarted(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        int variant = State.PowerupState switch
        {
            PowerStates.PowerupState.tiny => 2,
            _                             => 1,
        };
        _animator.SetBool(H_IsLookingUp,   true);
        _animator.SetInteger(H_LookUpVariant, variant);
    }

    private void OnLookUpEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsLookingUp,      false);
        _animator.SetInteger(H_LookUpVariant, 0);
        // Force isCrouching off immediately — without this, the animator shows
        // one frame of crouch limb positions before UpdateContinuousParams catches up.
        _animator.SetBool(H_IsCrouching, false);
    }

    private void OnCelebration(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        if (_isYeahPlaying || _hasTriggeredYeah) return;
        StartCoroutine(PlayYeahAnimation());
    }

    private System.Collections.IEnumerator PlayYeahAnimation()
    {
        _isYeahPlaying       = true;
        _hasTriggeredYeah    = true;

        _animator.SetBool(H_Yeah, true);

        yield return new WaitForSeconds(0.7f);

        _animator.SetBool(H_Yeah, false);
        _isYeahPlaying = false;
    }

    private void OnEmoteStarted(int playerIndex, MarioEmote emote)
    {
        if (playerIndex != PlayerIndex) return;

        // Reset all emote bools before applying the new one
        _animator.SetBool(H_IsScared,  false);
        _animator.SetBool(H_IsWorried, false);

        switch (emote)
        {
            case MarioEmote.Scared:
                _animator.SetBool(H_IsScared, true);
                break;
            case MarioEmote.Worried:
                _animator.SetBool(H_IsWorried, true);
                break;
            case MarioEmote.Celebrating:
                if (!_isYeahPlaying && !_hasTriggeredYeah)
                    StartCoroutine(PlayYeahAnimation());
                break;
        }
    }

    private void OnEmoteEnded(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        _animator.SetBool(H_IsScared,  false);
        _animator.SetBool(H_IsWorried, false);
    }

    private void OnFlipped(int playerIndex)
    {
        if (playerIndex != PlayerIndex) return;
        // Visual scale is already handled by MarioPhysics.Flip
        // This hook is available for any animator-level flip logic if needed
    }
}