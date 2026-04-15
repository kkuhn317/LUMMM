using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinator for the Goal Pole system.
///
/// Owns the pole state machine (Idle → Sliding → Cutscene) and delegates to:
///   - FlagPoleScoring  — score bands and reward granting
///   - FlagSlide        — cutscene puppet spawning and sliding
///   - FlagArrival      — post-slide movement (walk/jump/hop)
///   - FlagLevelFlow    — music/timer stopping and cutscene triggering
///
/// Add these four components to the FlagPole GameObject alongside this one.
/// </summary>
[RequireComponent(typeof(FlagPoleScoring))]
[RequireComponent(typeof(FlagSlide))]
[RequireComponent(typeof(FlagArrival))]
[RequireComponent(typeof(FlagLevelFlow))]
public class Flag : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────

    public float height;

    public GameObject flag;
    public GameObject pole;
    public bool       flagOnRight = false;
    public bool       playerSide  = false; // which side of the pole the player slides down

    // ─── State ───────────────────────────────────────────────────────────────

    private enum FlagState { Idle, Sliding, Cutscene }
    private FlagState _state = FlagState.Idle;

    // ─── Components ──────────────────────────────────────────────────────────

    private FlagPoleScoring _scoring;
    private FlagSlide       _slide;
    protected FlagSlide Slide => _slide;
    private FlagArrival     _arrival;
    private FlagLevelFlow   _flow;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        _scoring = GetComponent<FlagPoleScoring>();
        _slide   = GetComponent<FlagSlide>();
        _arrival = GetComponent<FlagArrival>();
        _flow    = GetComponent<FlagLevelFlow>();
    }

    protected virtual void Start()
    {
        ChangeHeight(height);
        _slide.Initialize(flag, _arrival);
        _slide.OnAllPlayersAtBottom  += OnAllPlayersAtBottom;
        _slide.OnFirstPlayerAtBottom += OnFirstPlayerAtBottom;
        _slide.OnSlideStarted        += _flow.OnSlideStarted;
        _arrival.OnPlayerArrived            += OnPlayerArrived;
        _arrival.OnArrivalMovementStarting  += _flow.OnArrivalMovementStarting;
        _flow.OnCutsceneAboutToPlay         += _slide.HideAllPuppets;
        _flow.OnCutsceneAboutToPlay         += () => Debug.Log("[Flag] OnCutsceneAboutToPlay fired — hiding puppets");

        _slide.flagOnRight = playerSide;
    }

    private void OnValidate()
    {
        if (flag == null || pole == null) return;
#if UNITY_EDITOR
        // Use delayCall so prefab variant overrides are fully applied first
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ChangeHeight(height);
        };
#endif
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        // In edit mode, re-apply height after prefab variant overrides are resolved
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) ChangeHeight(height);
            };
        }
    }
#endif

    protected virtual void Update()
    {
        if (_state != FlagState.Sliding) return;
        _slide.Tick();
    }

    // ─── Trigger ─────────────────────────────────────────────────────────────

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_state == FlagState.Cutscene) return;

        var mario = other.GetComponentInParent<MarioCore>();
        if (mario == null) return;
        if (_slide.IsRegistered(mario)) return;

        // First player — stop timers and music
        if (_state == FlagState.Idle)
        {
            _state = FlagState.Sliding;
            _flow.OnFirstPlayerTouched();
        }

        // Grant reward (scoring component may gate this)
        OnGrantReward(other, mario);

        // Register player for sliding
        _slide.RegisterPlayer(other, mario);

        // WhenFirstPlayerAtBottom mode — queue cutscene after first player lands
        // (handled in OnFirstPlayerAtBottom callback)
    }

    // ─── Callbacks ───────────────────────────────────────────────────────────

    private void OnFirstPlayerAtBottom(FlagSlide.PlayerSlideState ps)
    {
        if (_flow.TriggerMode == FlagLevelFlow.CutsceneTriggerMode.WhenFirstPlayerAtBottom)
            _flow.TriggerCutscene();
    }

    private void OnAllPlayersAtBottom(List<FlagSlide.PlayerSlideState> players)
    {
        // Only relevant for WhenAllPlayersAtBottom — arrival handles the rest
        // Cutscene fires when all have arrived (see OnPlayerArrived)
    }

    private void OnPlayerArrived(FlagSlide.PlayerSlideState ps)
    {
        if (_flow.TriggerMode != FlagLevelFlow.CutsceneTriggerMode.WhenAllPlayersAtBottom) return;
        if (_flow.LevelEndQueued) return;

        // Check if all players have arrived
        bool allArrived = true;
        foreach (var p in _slide.SlidingPlayers)
            if (!p.ArrivedAtTarget) { allArrived = false; break; }

        if (allArrived && !_slide.IsEmpty)
        {
            _state = FlagState.Cutscene;
            _flow.TriggerCutscene();
        }
    }

    // ─── Reward (virtual for subclasses) ─────────────────────────────────────

    /// <summary>
    /// Override to gate or modify reward granting.
    /// Default calls FlagPoleScoring.GrantReward.
    /// </summary>
    protected virtual void OnGrantReward(Collider2D other, MarioCore mario)
    {
        _scoring.GrantReward(other, mario);

        // Bonus for reaching the flag while carrying an item
        if (mario.State.Carrying && mario.Carry.HeldObjectPosition.transform.childCount > 0)
        {
            const int carryBonus = 1000;

            GameManager.Instance?.GetSystem<ScoreSystem>().AddScore(carryBonus);

            if (ScorePopupManager.Instance != null)
            {
                var result = new ComboResult(RewardType.Score, PopupID.Score1000, carryBonus);
                ScorePopupManager.Instance.ShowPopup(
                    result,
                    mario.Carry.HeldObjectPosition.transform.position + Vector3.up * 0.5f,
                    mario.State.PowerupState);
            }

            // Destroy the item so it doesn't follow Mario into the flagpole cutscene
            var heldTransform = mario.Carry.HeldObjectPosition.transform;
            if (heldTransform.childCount > 0)
            {
                var heldObj = heldTransform.GetChild(0).gameObject;
                heldObj.transform.SetParent(null);
                mario.State.Carrying = false;
                Destroy(heldObj);
            }
        }
    }

    // ─── Height / Layout ─────────────────────────────────────────────────────

    public void ChangeHeight(float h)
    {
        height = h;

        flag.transform.localPosition = new Vector3(flagOnRight ? 0.5f : -0.5f, height - 0.55f, 0);

        var flagRenderer = flag.GetComponent<SpriteRenderer>();
        if (flagRenderer != null)
            flagRenderer.flipX = flagOnRight;

        pole.GetComponent<SpriteRenderer>().size = new Vector2(0.5f, height);
        pole.transform.localPosition             = new Vector3(0, (height + 1) / 2, 0);
        GetComponent<BoxCollider2D>().size        = new Vector2(0.25f, height);
        GetComponent<BoxCollider2D>().offset      = new Vector2(0, (height + 1) / 2);

        // Keep slide component in sync
        if (_slide != null)
        {
            _slide.flagOnRight       = playerSide;
            _slide.flagVisualOnRight = flagOnRight;
        }
    }
}