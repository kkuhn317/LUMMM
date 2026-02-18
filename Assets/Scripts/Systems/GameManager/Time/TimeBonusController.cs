using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeBonusController : MonoBehaviour
{
    [Header("Time Bonus Settings")]
    [SerializeField] private int timeBonusPerSecond = 50;
    [SerializeField] private float timeBonusTickRealtime = 0.02f;
    [SerializeField] private int timeBonusFastChunkThreshold = 100;
    [SerializeField] private int timeBonusFastChunkSize = 5;
    [SerializeField] private bool animateTimeBonus = true;

    [Header("Audio")]
    [SerializeField] private AudioClip timeBonusTickSfx;
    [SerializeField] private float timeBonusTickVolume = 1f;
    [SerializeField] private int timeBonusTickEverySteps = 1;

    [Header("Input")]
    [SerializeField] private InputActionReference skipEndSequenceActionRef;

    private ScoreSystem scoreSystem;
    private TimerManager timerManager;
    private AudioSource audioSource;
    private PlayerRegistry playerRegistry;

    private bool skipTimeBonus = false;
    private InputAction skipEndSequenceAction;

    private void Awake()
    {
        scoreSystem = GetComponent<ScoreSystem>();
        timerManager = GetComponent<TimerManager>();
        audioSource  = GetComponent<AudioSource>();

        // Cache once (avoid FindObjectOfType every frame)
        playerRegistry = FindObjectOfType<PlayerRegistry>();

        if (skipEndSequenceActionRef != null)
            skipEndSequenceAction = skipEndSequenceActionRef.action;
    }

    private bool IsAnyPlayerSkipPressed()
    {
        if (playerRegistry == null) return false;
        
        foreach (var player in playerRegistry.GetAllPlayers())
        {
            if (player == null) continue;

            var pi = player.GetComponent<PlayerInput>();
            if (pi == null || pi.actions == null) continue;
            
            var submit = pi.actions.FindAction("Submit", false);
            if (submit != null && submit.WasPressedThisFrame()) return true;
            
            var confirm = pi.actions.FindAction("Confirm", false);
            if (confirm != null && confirm.WasPressedThisFrame()) return true;

            var jump = pi.actions.FindAction("Jump", false);
            if (jump != null && jump.WasPressedThisFrame()) return true;
        }

        return false;
    }

    private bool IsSkipPressedThisFrame()
    {
        // If an explicit skip action exists, use it (already enabled during the sequence).
        if (skipEndSequenceAction != null)
            return skipEndSequenceAction.WasPressedThisFrame();

        // Otherwise, fallback to "any player submit/confirm/jump".
        return IsAnyPlayerSkipPressed();
    }

    public IEnumerator AnimateTimeBonus()
    {
        if (!animateTimeBonus) yield break;
        if (GlobalVariables.infiniteTimeMode) yield break;
        if (timerManager == null) yield break;

        int timeLeft = Mathf.Max(0, Mathf.FloorToInt(timerManager.CurrentTime));

        // Snapshot for win-screen purposes
        GameEvents.TriggerTimeBonusStarted(timeLeft);
        GameEvents.TriggerTimerChanged((float)timeLeft);

        skipTimeBonus = false;

        int sfxCounter = 0;

        while (timeLeft > 0 && !skipTimeBonus)
        {
            if (IsSkipPressedThisFrame())
            {
                skipTimeBonus = true;
                break;
            }

            int step = (timeLeft > timeBonusFastChunkThreshold)
                ? Mathf.Min(timeBonusFastChunkSize, timeLeft)
                : 1;

            timeLeft -= step;

            GameEvents.TriggerTimeBonusTick(step);

            // Animate timer down alongside the score
            GameEvents.TriggerTimerChanged((float)timeLeft);

            scoreSystem?.AddScore(step * timeBonusPerSecond);

            sfxCounter += step;
            if (timeBonusTickSfx != null && audioSource != null &&
                (timeBonusTickEverySteps <= 1 || sfxCounter >= timeBonusTickEverySteps))
            {
                audioSource.PlayOneShot(timeBonusTickSfx, timeBonusTickVolume);
                sfxCounter = 0;
            }

            yield return new WaitForSecondsRealtime(timeBonusTickRealtime);
        }

        // If skipped, add remaining bonus instantly, and snap timer to 0
        if (skipTimeBonus && timeLeft > 0)
        {
            scoreSystem?.AddScore(timeLeft * timeBonusPerSecond);
            timeLeft = 0;
            GameEvents.TriggerTimerChanged(0f);
        }

        GameEvents.TriggerTimeBonusComplete();
    }

    public void AwardTimeBonusInstant()
    {
        if (GlobalVariables.infiniteTimeMode) return;
        if (timerManager == null) return;

        int timeLeft = Mathf.Max(0, Mathf.FloorToInt(timerManager.CurrentTime));

        scoreSystem?.AddScore(timeLeft * timeBonusPerSecond);

        // Snap timer to 0 visually too
        GameEvents.TriggerTimerChanged(0f);

        GameEvents.TriggerTimeBonusComplete();
    }

    public void SkipTimeBonus()
    {
        skipTimeBonus = true;
    }
}