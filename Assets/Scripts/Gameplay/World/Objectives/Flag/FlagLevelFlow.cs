using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles level-flow integration — stopping music/timers, triggering the
/// cutscene/timeline, and ending the level. Swap this out for a different
/// game's level flow system without touching any other flag component.
/// </summary>
public class FlagLevelFlow : MonoBehaviour
{
    public enum CutsceneTriggerMode
    {
        WhenAllPlayersAtBottom,  // wait for everyone to finish sliding
        WhenFirstPlayerAtBottom, // fire as soon as first player lands
    }

    [Tooltip("When to trigger the end sequence after sliding.")]
    public CutsceneTriggerMode triggerMode = CutsceneTriggerMode.WhenAllPlayersAtBottom;

    [Tooltip("If true, plays a cutscene/timeline before triggering the win. If false, triggers the win directly.")]
    public bool useCutscene = true;

    [Tooltip("Custom cutscene selector. If null, uses PlayableDirector on this GameObject.")]
    [SerializeField] private CutsceneSelector cutsceneSelector;

    [Tooltip("How long the cutscene lasts before the level ends.")]
    public float cutsceneTime = 10f;

    [Tooltip("Extra wait time before the end sequence fires.")]
    public float cutsceneDelay = 0f;

    [Tooltip("Played the moment the player grabs the pole and starts sliding down.")]
    [SerializeField] private AudioClip slideSound;

    [Tooltip("Played when arrivalMode movement begins (course clear jingle).")]
    [SerializeField] private AudioClip endingMusic;

    // ─── Events ──────────────────────────────────────────────────────────────

    /// <summary>Fired the moment the cutscene is about to play — use to hide puppets.</summary>
    public System.Action OnCutsceneAboutToPlay;

    // ─── State ───────────────────────────────────────────────────────────────

    private bool _musicStopped        = false;
    private bool _levelEndQueued      = false;
    private bool _endingMusicPlayed   = false;

    private LevelFlowController _levelFlowController;

    // ─── Public API ──────────────────────────────────────────────────────────

    public CutsceneTriggerMode TriggerMode => triggerMode;
    public bool LevelEndQueued => _levelEndQueued;

    public void OnFirstPlayerTouched()
    {
        if (_levelFlowController == null)
            _levelFlowController = FindObjectOfType<LevelFlowController>(true);

        var timerManager = GameManager.Instance?.GetSystem<TimerManager>();
        LevelFlowController.MarkEndingLevel();
        timerManager?.StopAllTimers();
        timerManager?.StopTimeWarningMusic();

        if (!_musicStopped)
        {
            _musicStopped = true;
            MusicManager.Instance?.MuteAllMusic();
        }

        GameEvents.TriggerLevelEnding();
    }

    /// <summary>Called the moment the first player grabs the pole and begins sliding.</summary>
    public void OnSlideStarted()
    {
        if (slideSound != null)
            AudioManager.Instance?.Play(slideSound, SoundCategory.SFX);
    }

    /// <summary>Called right before arrivalMode movement begins.</summary>
    public void OnArrivalMovementStarting()
    {
        if (_endingMusicPlayed) return;
        _endingMusicPlayed = true;

        if (!useCutscene && endingMusic != null)
            AudioManager.Instance?.Play(endingMusic, SoundCategory.SFX);
    }

    public void TriggerCutscene()
    {
        if (_levelEndQueued) return;
        _levelEndQueued = true;
        StartCoroutine(EndSequenceRoutine());
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private IEnumerator EndSequenceRoutine()
    {
        if (cutsceneDelay > 0f)
            yield return new WaitForSeconds(cutsceneDelay);

        // Release camera override
        FindObjectOfType<CameraFollow>()?.ClearOverrideTargets();

        if (!useCutscene)
        {
            _levelFlowController?.TriggerWin();
            yield break;
        }

        var ctx = new CutsceneContext { scene = SceneManager.GetActiveScene() };

        // Fire before either path plays the cutscene
        OnCutsceneAboutToPlay?.Invoke();

        if (cutsceneSelector != null)
        {
            StartCoroutine(cutsceneSelector.PlaySelectedCutscene(ctx));
        }
        else
        {
            var director = GetComponent<PlayableDirector>();
            if (_levelFlowController != null && director != null)
                _levelFlowController.TriggerCutsceneEnding(director, cutsceneTime, false, false);
            else
                Debug.LogWarning("[FlagLevelFlow] useCutscene is true but no CutsceneSelector or PlayableDirector found.");
        }
    }
}