using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class WinScreenController : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private GameObject winScreenContainer;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Win Screen UI Elements")]
    [SerializeField] private TMP_Text winScreenScoreText;
    [SerializeField] private TMP_Text winScreenTimerText;
    [SerializeField] private TMP_Text winScreenCollectedCoinsText;
    [SerializeField] private TMP_Text winScreenTotalCoinsText;

    [SerializeField] private TMP_Text winScreenSpeedrunTimeText;
    [SerializeField] private GameObject winScreenSpeedrunTimeBox;

    [SerializeField] private RawImage winScreenObtainedRankImage;
    [SerializeField] private Image[] winScreenGreenCoinImages;

    [Header("Notification Text")]
    [SerializeField] private GameObject newHighScoreText;
    [SerializeField] private GameObject newBestRankText;

    [Header("Rank Visuals")]
    [SerializeField] private RankVisuals rankVisuals;

    [Header("Green Coin Sprites")]
    [SerializeField] private Sprite greenCoinCollectedSprite;

    [Header("Input Lock")]
    [Tooltip("UI navigation/submit will be locked right after pressing a Win Screen option, to prevent extra inputs during the transition.")]
    [SerializeField] private UIInputLock uiInputLock;
    [Tooltip("Failsafe: if for some reason a scene transition does NOT happen, unlock after this many real-time seconds. Set to 0 to never auto-unlock.")]
    [SerializeField] private float unlockFailsafeSeconds = 0.75f;
    private bool optionPressed;

    private int currentScore = 0;
    private int currentHighScore = 0;
    private int currentCoins = 0;
    private int totalCoinsInLevel = 0;

    private float currentTime = 0;
    private int finishTimeSnapshot = -1;
    private TimeSpan currentSpeedrunTime = TimeSpan.Zero;

    private PlayerRank currentRank = PlayerRank.Default;
    private PlayerRank highestRank = PlayerRank.Default;
    private PlayerRank highestRankAtLevelStart = PlayerRank.Default;
    private bool gotNewBestRankThisRun = false;

    private bool speedrunTimerVisible = false;
    private bool isWinScreenActive = false;

    private void Start()
    {
        if (winScreenContainer != null)
            winScreenContainer.SetActive(false);

        currentScore = GlobalVariables.score;
        currentCoins = GlobalVariables.coinCount;
        currentTime = GlobalVariables.infiniteTimeMode ? 0 : 300f;

        speedrunTimerVisible = GlobalVariables.SpeedrunMode;

        LoadHighScore();
        CaptureHighestRankAtStart();
        ResetWinScreenNotifications();
    }

    private void OnEnable()
    {
        // Buttons
        // using this "Remove + Add" pattern to avoid duplicate listeners in case of multiple enable/disable cycles
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // Events
        GameEvents.OnLevelStarted += OnLevelStarted;

        GameEvents.OnWinScreenShown += OnWinScreenShown;
        GameEvents.OnGameOverScreenShown += OnGameOverScreenShown;

        GameEvents.OnScoreChanged += OnScoreChanged;
        GameEvents.OnHighScoreChanged += OnHighScoreChanged;

        GameEvents.OnCoinsChanged += OnCoinsChanged;

        GameEvents.OnTimeBonusStarted += OnTimeBonusStarted;

        GameEvents.OnTimerChanged += OnTimerChanged;
        GameEvents.OnSpeedrunTimeChanged += OnSpeedrunTimeChanged;

        GameEvents.OnRankChanged += OnRankChanged;
        GameEvents.OnHighestRankChanged += OnHighestRankChanged;

        GameEvents.OnTotalCoinsCalculated += OnTotalCoinsCalculated;
        GameEvents.OnAllCoinsCollected += OnAllCoinsCollected;

        GameEvents.OnGreenCoinProgress += OnGreenCoinProgress;
        GameEvents.OnAllGreenCoinsCollected += OnAllGreenCoinsCollected;
    }

    private void OnDisable()
    {
        if (uiInputLock == null)
            uiInputLock = FindObjectOfType<UIInputLock>(true);

        // Buttons
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);

        // Events
        GameEvents.OnLevelStarted -= OnLevelStarted;

        GameEvents.OnWinScreenShown -= OnWinScreenShown;
        GameEvents.OnGameOverScreenShown -= OnGameOverScreenShown;

        GameEvents.OnScoreChanged -= OnScoreChanged;
        GameEvents.OnHighScoreChanged -= OnHighScoreChanged;

        GameEvents.OnCoinsChanged -= OnCoinsChanged;

        GameEvents.OnTimeBonusStarted -= OnTimeBonusStarted;

        GameEvents.OnTimerChanged -= OnTimerChanged;
        GameEvents.OnSpeedrunTimeChanged -= OnSpeedrunTimeChanged;

        GameEvents.OnRankChanged -= OnRankChanged;
        GameEvents.OnHighestRankChanged -= OnHighestRankChanged;

        GameEvents.OnTotalCoinsCalculated -= OnTotalCoinsCalculated;
        GameEvents.OnAllCoinsCollected -= OnAllCoinsCollected;

        GameEvents.OnGreenCoinProgress -= OnGreenCoinProgress;
        GameEvents.OnAllGreenCoinsCollected -= OnAllGreenCoinsCollected;
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("WinScreenController: GameManagerRefactored.Instance is null");
            return;
        }

        LockWinScreenInput();

        GameManager.Instance.RestartLevelFromBeginningWithFadeOut();
    }

    private void OnQuitClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("WinScreenController: GameManagerRefactored.Instance is null");
            return;
        }

        LockWinScreenInput();

        GameManager.Instance.QuitLevel();
    }

    private void LockWinScreenInput()
    {
        // Prefer your global lock system if it exists
        if (uiInputLock != null)
        {
            uiInputLock.Lock(rememberSelection: false);
        }

        // Block interaction at the Button level too (extra safety)
        if (restartButton != null) restartButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;

        // Failsafe unlock (only runs if we stay in this scene)
        if (unlockFailsafeSeconds > 0f)
            StartCoroutine(AutoUnlockRoutine(unlockFailsafeSeconds));
    }

    private void UnlockWinScreenInput()
    {
        if (uiInputLock != null)
        {
            uiInputLock.Unlock(restoreSelection: false);
        }

        if (restartButton != null) restartButton.interactable = true;
        if (quitButton != null) quitButton.interactable = true;
    }

    private IEnumerator AutoUnlockRoutine(float secondsRealtime)
    {
        yield return new WaitForSecondsRealtime(secondsRealtime);

        // If we didn't transition away, re-enable input so the player isn't stuck.
        if (isWinScreenActive)
        {
            optionPressed = false;
            UnlockWinScreenInput();

            // Restore selection to restart button for controller flow
            if (restartButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }
    }

    private void OnLevelStarted()
    {
        finishTimeSnapshot = -1;
        isWinScreenActive = false;

        if (winScreenContainer != null)
            winScreenContainer.SetActive(false);

        CaptureHighestRankAtStart();
        ResetWinScreenNotifications();
    }

    private void OnWinScreenShown()
    {
        isWinScreenActive = true;

        if (winScreenContainer != null)
            winScreenContainer.SetActive(true);
        
        UpdateWinScreenUI();

        if (restartButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }
    }

    private void OnGameOverScreenShown()
    {
        isWinScreenActive = false;

        if (winScreenContainer != null)
            winScreenContainer.SetActive(false);

        ResetWinScreenNotifications();
    }

    private void OnScoreChanged(int newScore)
    {
        currentScore = newScore;
        if (isWinScreenActive)
        {
            UpdateWinScreenScore();
            UpdateWinScreenNotifications();
        }
    }

    private void OnHighScoreChanged(int newHighScore)
    {
        currentHighScore = newHighScore;

        if (isWinScreenActive)
        {
            UpdateWinScreenScore();
            UpdateWinScreenNotifications();
        }
    }

    private void OnCoinsChanged(int newCoins)
    {
        currentCoins = newCoins;
        if (isWinScreenActive)
            UpdateWinScreenCoins();
    }

    private void OnTimeBonusStarted(int timeAtFinish)
    {
        finishTimeSnapshot = timeAtFinish;
    }

    private void OnTimerChanged(float newTime)
    {
        currentTime = newTime;
        if (isWinScreenActive)
            UpdateWinScreenTimer();
    }

    private void OnSpeedrunTimeChanged(TimeSpan newTime)
    {
        currentSpeedrunTime = newTime;
        if (isWinScreenActive)
            UpdateWinScreenSpeedrunTime();
    }

    private void OnRankChanged(PlayerRank rank)
    {
        currentRank = rank;
        if (isWinScreenActive)
            UpdateWinScreenRank();
    }

    private void OnHighestRankChanged(PlayerRank rank)
    {
        highestRank = rank;
        gotNewBestRankThisRun = highestRank > highestRankAtLevelStart;

        if (isWinScreenActive)
        {
            UpdateWinScreenRank();
            UpdateWinScreenNotifications();
        }
    }

    private void OnTotalCoinsCalculated(int totalCoins)
    {
        totalCoinsInLevel = totalCoins;
        if (isWinScreenActive)
            UpdateWinScreenTotalCoins();
    }

    private void OnAllCoinsCollected(bool allCollected)
    {
        if (allCollected && isWinScreenActive)
            UpdateWinScreenTotalCoins();
    }

    private void OnGreenCoinProgress(int collected, int total, int index)
    {
        if (winScreenGreenCoinImages == null || winScreenGreenCoinImages.Length == 0)
            return;

        if (index < 0 || index >= winScreenGreenCoinImages.Length)
            return;

        if (winScreenGreenCoinImages[index] != null && greenCoinCollectedSprite != null)
            winScreenGreenCoinImages[index].sprite = greenCoinCollectedSprite;
    }

    private void OnAllGreenCoinsCollected(bool allCollected)
    {
        
    }

    private void UpdateWinScreenUI()
    {
        UpdateWinScreenScore();
        UpdateWinScreenTimer();
        UpdateWinScreenCoins();
        UpdateWinScreenTotalCoins();
        UpdateWinScreenSpeedrunTime();
        UpdateWinScreenRank();
        UpdateWinScreenNotifications();
    }

    private void CaptureHighestRankAtStart()
    {
        string levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        var ps = new ProgressStore();

        if (ps.TryGetLevel(levelId, out var levelData))
            highestRankAtLevelStart = (PlayerRank)levelData.highestRank;
        else
            highestRankAtLevelStart = PlayerRank.Default;

        gotNewBestRankThisRun = false;
    }

    private void ResetWinScreenNotifications()
    {
        if (newHighScoreText != null) newHighScoreText.SetActive(false);
        if (newBestRankText != null) newBestRankText.SetActive(false);
    }

    private void UpdateWinScreenNotifications()
    {
        // New High Score
        if (newHighScoreText != null)
            newHighScoreText.SetActive(currentScore > currentHighScore);

        // New Best Rank (only show if we got a rank better than Default, to avoid showing it on first playthrough with no rank)
        if (newBestRankText != null)
            newBestRankText.SetActive(gotNewBestRankThisRun);
    }

    private void UpdateWinScreenScore()
    {
        if (winScreenScoreText != null)
            winScreenScoreText.text = currentScore.ToString("D9");
    }

    private void UpdateWinScreenTimer()
    {
        if (winScreenTimerText == null)
            return;

        if (GlobalVariables.infiniteTimeMode)
        {
            winScreenTimerText.text = "INF!";
            return;
        }

        winScreenTimerText.gameObject.SetActive(true);

        int timeToShow = (finishTimeSnapshot >= 0)
            ? finishTimeSnapshot
            : Mathf.Max(0, Mathf.FloorToInt(currentTime));

        winScreenTimerText.text = timeToShow.ToString("D3");
    }

    private void UpdateWinScreenCoins()
    {
        if (winScreenCollectedCoinsText != null)
            winScreenCollectedCoinsText.text = currentCoins.ToString("D3");
    }

    private void UpdateWinScreenTotalCoins()
    {
        if (winScreenTotalCoinsText != null)
            winScreenTotalCoinsText.text = totalCoinsInLevel.ToString("D3");
    }

    private void UpdateWinScreenSpeedrunTime()
    {
        if (winScreenSpeedrunTimeBox != null)
            winScreenSpeedrunTimeBox.SetActive(speedrunTimerVisible);
        else if (winScreenSpeedrunTimeText != null)
            winScreenSpeedrunTimeText.gameObject.SetActive(speedrunTimerVisible);

        if (!speedrunTimerVisible || winScreenSpeedrunTimeText == null)
            return;

        winScreenSpeedrunTimeText.text = currentSpeedrunTime.ToString(@"m\:ss\.ff");
    }

    private void UpdateWinScreenRank()
    {
        if (winScreenObtainedRankImage == null || rankVisuals == null)
            return;

        Sprite sprite = rankVisuals.GetSprite(currentRank);
        if (sprite != null)
            winScreenObtainedRankImage.texture = sprite.texture;
    }

    private void LoadHighScore()
    {
        string levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        var progressStore = new ProgressStore();

        if (progressStore.TryGetLevel(levelId, out var levelData))
            currentHighScore = levelData.highScore;
    }
}