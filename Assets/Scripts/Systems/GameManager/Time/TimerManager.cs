using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float startingTime = 300f;
    [SerializeField] private GameObject timeWarningOverridePrefab;

    private float currentTime;
    private bool stopTimer = false;
    private bool timeWarningActive = false;
    private GameObject timeWarningInstance;

    public float StartingTime => startingTime;
    public float CurrentTime => currentTime;
    public bool IsTimeUp => currentTime <= 0;

    private void Start()
    {
        currentTime = startingTime;

        GameEvents.TriggerTimerChanged(currentTime);

        if (GlobalVariables.SpeedrunMode)
            GlobalVariables.speedrunTimer.Start();
    }

    private void Update()
    {
        if (GlobalVariables.stopTimeLimit || stopTimer) return;

        if (currentTime > 0)
        {
            float previousTime = currentTime;
            currentTime -= Time.deltaTime;

            if (Mathf.FloorToInt(currentTime) != Mathf.FloorToInt(previousTime))
                GameEvents.TriggerTimerChanged(currentTime);

            if (currentTime <= 100f && !timeWarningActive)
            {
                TriggerTimeWarning();
                GameEvents.TriggerTimeWarning();
            }

            if (currentTime <= 0)
            {
                currentTime = 0;
                GameEvents.TriggerTimeUp();
            }
        }

        if (GlobalVariables.SpeedrunMode)
            GameEvents.TriggerSpeedrunTimeChanged(GlobalVariables.elapsedTime);
    }

    private void TriggerTimeWarning()
    {
        if (timeWarningOverridePrefab == null) return;

        timeWarningInstance = Instantiate(timeWarningOverridePrefab);
        timeWarningInstance.GetComponent<MusicOverride>()?.stopPlayingAfterTime(3f);
        timeWarningActive = true;
    }

    // NEW: Legacy parity cleanup
    public void StopTimeWarningMusic()
    {
        if (timeWarningInstance != null)
        {
            Destroy(timeWarningInstance);
            timeWarningInstance = null;
        }
        timeWarningActive = false;
    }

    public void StopAllTimers()
    {
        stopTimer = true;
        GlobalVariables.speedrunTimer.Stop();
        StopTimeWarningMusic();
    }

    public void PauseTimers()
    {
        GlobalVariables.speedrunTimer.Stop();
    }

    public void ResumeTimers()
    {
        if (!stopTimer)
            GlobalVariables.speedrunTimer.Start();
    }
}