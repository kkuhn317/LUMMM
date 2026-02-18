using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class LevelFlowController : MonoBehaviour
{
    [Header("Level Flow Settings")]
    [SerializeField] private string loseLifeSceneName = "LoseLife";
    [SerializeField] private float winScreenDelay = 1.5f;
    [SerializeField] private float deathTransitionDelay = 0.5f;

    private LevelCompletionController completionController;
    private TimerManager timerManager;
    private TimeBonusController timeBonusController;
    private LifeSystem lifeSystem;
    private PauseMenuController pauseController;

    private bool winSequenceRunning;
    private bool deathSequenceRunning;

    // Stored for debugging and future per-level flow rules
    private string currentLevelId;

    private void Awake()
    {
        completionController = GetComponent<LevelCompletionController>();
        timerManager = GetComponent<TimerManager>();
        timeBonusController = GetComponent<TimeBonusController>();
        lifeSystem = GetComponent<LifeSystem>();
        pauseController = FindObjectOfType<PauseMenuController>();

        if (completionController == null) Debug.LogError($"{nameof(LevelFlowController)} requires {nameof(LevelCompletionController)}.");
        if (timerManager == null) Debug.LogError($"{nameof(LevelFlowController)} requires {nameof(TimerManager)}.");
        if (lifeSystem == null) Debug.LogError($"{nameof(LevelFlowController)} requires {nameof(LifeSystem)}.");
        if (pauseController == null) Debug.LogError($"{nameof(LevelFlowController)} requires {nameof(PauseMenuController)} in the scene.");
    }

    /// <summary>
    /// Called by GameManager when a level starts. Resets internal state for a new level session.
    /// </summary>
    public void InitializeForLevel(string levelId)
    {
        currentLevelId = levelId;

        // Reset state to ensure reloading a level does not keep stale flags
        winSequenceRunning = false;
        deathSequenceRunning = false;
    }

    public void TriggerWin()
    {
        if (winSequenceRunning) return;
        winSequenceRunning = true;

        // Disable pausing during win flow
        pauseController?.SetPauseEnabled(false);

        StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        timerManager?.StopAllTimers();

        if (timeBonusController != null)
            yield return timeBonusController.AnimateTimeBonus();

        yield return new WaitForSeconds(winScreenDelay);

        // Level completion is owned by LevelCompletionController to avoid duplicate events
        completionController?.CompleteLevel();
        GameEvents.TriggerWinScreenShown();
    }

    public void TriggerCutsceneEnding(
        PlayableDirector cutscene,
        float cutsceneLength,
        bool destroyPlayersImmediately,
        bool stopMusicImmediately)
    {
        pauseController?.SetPauseEnabled(false);
        StartCoroutine(CutsceneEndSequence(cutscene, cutsceneLength, destroyPlayersImmediately, stopMusicImmediately));
    }

    private IEnumerator CutsceneEndSequence(
        PlayableDirector cutscene,
        float cutsceneLength,
        bool destroyPlayersImmediately,
        bool stopMusicImmediately)
    {
        timerManager?.StopAllTimers();

        if (destroyPlayersImmediately)
        {
            var registry = FindObjectOfType<PlayerRegistry>();
            if (registry == null)
            {
                Debug.LogError($"{nameof(LevelFlowController)} expected {nameof(PlayerRegistry)} for destroyPlayersImmediately.");
            }
            else
            {
                foreach (var p in registry.GetAllPlayerObjects())
                {
                    if (p != null) Destroy(p);
                }
            }
        }

        if (stopMusicImmediately && MusicManager.Instance != null)
            MusicManager.Instance.MuteAllMusic();

        if (cutscene != null)
        {
            cutscene.Play();
            yield return new WaitForSeconds(cutsceneLength);
        }

        TriggerWin();
    }

    public void TriggerDeath()
    {
        if (deathSequenceRunning) return;
        deathSequenceRunning = true;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        timerManager?.StopTimeWarningMusic();

        var checkpointManager = FindObjectOfType<CheckpointManager>();
        if (checkpointManager == null)
            Debug.LogError($"{nameof(LevelFlowController)} requires {nameof(CheckpointManager)} in the scene.");

        bool gameOver = lifeSystem.RemoveLifeSilent();

        if (checkpointManager != null && checkpointManager.HasCheckpoint)
            checkpointManager.SaveCurrentCheckpoint();

        if (gameOver)
        {
            checkpointManager?.ClearCheckpoint();

            GameEvents.TriggerGameOver();
            GameEvents.TriggerGameOverScreenShown();

            deathSequenceRunning = false;
            yield break;
        }

        yield return new WaitForSeconds(deathTransitionDelay);

        if (!string.IsNullOrEmpty(loseLifeSceneName) && !GlobalVariables.infiniteLivesMode)
        {
            PlayerPrefs.SetString("LastLevelScene", SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();

            SceneManager.LoadScene(loseLifeSceneName);
        }
        else
        {
            Debug.LogWarning($"{nameof(LevelFlowController)}: No lose life scene set or infinite lives mode enabled, reloading current scene on death.");
            GameManagerRefactored.Instance?.ReloadScene();
        }

        deathSequenceRunning = false;
    }
}