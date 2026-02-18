using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;

public class HUDController : MonoBehaviour
{
    [Header("Core UI Elements")]
    [Tooltip("levelUI - Main canvas for the HUD. Can be used to enable/disable the entire HUD when needed.")]
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text speedrunTimerText;

    [Header("Special UI Elements")]
    [SerializeField] private RawImage currentRankImage;
    [SerializeField] private RawImage highestRankImage;
    [SerializeField] private Image infiniteTimeImage;
    [SerializeField] private Image infiniteLivesImage;

    [Header("Level UI")]
    [SerializeField] private LevelDatabase levelDatabase;
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private GameObject checkpointIndicator;

    [Header("Green Coin UI")]
    [SerializeField] private Image[] greenCoinImages;
    [SerializeField] private Sprite greenCoinCollectedSprite;

    [Header("Rank Sprites")]
    [SerializeField] private RankVisuals rankVisuals;

    [Header("Animation Settings")]
    [SerializeField] private bool enableAnimations = true;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Color flashColor = Color.yellow;

    // Current state
    private string currentLevelId = "unknown";
    private float startingTimeFromLevel = 300f;
    private int currentScore = 0;
    private int currentHighScore = 0;
    private int currentLives = 0;
    private int currentCoins = 0;
    private float currentTime = 0;
    private TimeSpan currentSpeedrunTime = TimeSpan.Zero;
    private PlayerRank currentRank = PlayerRank.Default;
    private PlayerRank highestRank = PlayerRank.Default;

    private bool infiniteTimeActive = false;
    private bool infiniteLivesActive = false;
    private bool speedrunTimerVisible = false;
    private bool isPaused = false;
    private bool isGameOverActive = false;
    private bool isWinScreenActive = false;

    // Original colors for animations
    private Color originalScoreColor;
    private Color originalCoinColor;
    private Color originalTimerColor;
    private Color originalLivesColor;
    
    private Coroutine livesFlashRoutine;
    private Coroutine timerFlashRoutine;
    private Coroutine levelNameRoutine;

    private void Awake()
    {
        // Store original colors for animations
        if (scoreText != null) originalScoreColor = scoreText.color;
        if (coinText != null) originalCoinColor = coinText.color;
        if (timerText != null) originalTimerColor = timerText.color;
        if (livesText != null) originalLivesColor = livesText.color;
    }

    private void Start()
    {
        // Initialize from global variables
        currentScore = GlobalVariables.score;
        currentLives = GlobalVariables.lives;
        currentCoins = GlobalVariables.coinCount;
        currentTime = GlobalVariables.infiniteTimeMode ? 0 : 300f;

        // Load high score
        LoadHighScore();

        // Check modifiers
        infiniteTimeActive = GlobalVariables.infiniteTimeMode;
        infiniteLivesActive = GlobalVariables.infiniteLivesMode;
        speedrunTimerVisible = GlobalVariables.SpeedrunMode;

        // Load highest rank
        LoadHighestRank();

        // Set initial UI states
        UpdateAllUI();
        RefreshCheckpointIndicatorFromScene();
    }

    private void OnEnable()
    {
        // Subscribe to HUD-related game events
        GameEvents.OnLevelContextChanged += OnLevelContextChanged;
        GameEvents.OnCheckpointChanged += OnCheckpointChanged; 

        GameEvents.OnScoreChanged += OnScoreChanged;
        GameEvents.OnHighScoreChanged += OnHighScoreChanged;
        GameEvents.OnScoreAdded += OnScoreAdded;

        GameEvents.OnLivesChanged += OnLivesChanged;
        GameEvents.OnExtraLifeGained += OnExtraLifeGained;

        GameEvents.OnCoinsChanged += OnCoinsChanged;
        GameEvents.OnCoinsAdded += OnCoinsAdded;

        GameEvents.OnTimerChanged += OnTimerChanged;
        GameEvents.OnTimeWarning += OnTimeWarning;
        GameEvents.OnTimeUp += OnTimeUp;
        GameEvents.OnSpeedrunTimeChanged += OnSpeedrunTimeChanged;

        GameEvents.OnRankChanged += OnRankChanged;
        GameEvents.OnHighestRankChanged += OnHighestRankChanged;

        GameEvents.OnGreenCoinCollected += OnGreenCoinCollected;
        GameEvents.OnGreenCoinProgress += OnGreenCoinProgress;
        GameEvents.OnAllGreenCoinsCollected += OnAllGreenCoinsCollected;

        GameEvents.OnGamePaused += OnGamePaused;
        GameEvents.OnGameResumed += OnGameResumed;
        GameEvents.OnGameOverScreenShown += OnGameOverScreenShown;
        GameEvents.OnWinScreenShown += OnWinScreenShown;
        
        GameEvents.OnLevelStarted += OnLevelStarted;

        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        RefreshCheckpointIndicatorFromScene();
        RefreshLevelContextForDebug();
    }

    private void OnDisable()
    {
        GameEvents.OnLevelContextChanged -= OnLevelContextChanged;
        GameEvents.OnCheckpointChanged -= OnCheckpointChanged; 

        GameEvents.OnScoreChanged -= OnScoreChanged;
        GameEvents.OnHighScoreChanged -= OnHighScoreChanged;
        GameEvents.OnScoreAdded -= OnScoreAdded;

        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnExtraLifeGained -= OnExtraLifeGained;

        GameEvents.OnCoinsChanged -= OnCoinsChanged;
        GameEvents.OnCoinsAdded -= OnCoinsAdded;

        GameEvents.OnTimerChanged -= OnTimerChanged;
        GameEvents.OnTimeWarning -= OnTimeWarning;
        GameEvents.OnTimeUp -= OnTimeUp;
        GameEvents.OnSpeedrunTimeChanged -= OnSpeedrunTimeChanged;

        GameEvents.OnRankChanged -= OnRankChanged;
        GameEvents.OnHighestRankChanged -= OnHighestRankChanged;

        GameEvents.OnGreenCoinCollected -= OnGreenCoinCollected;
        GameEvents.OnGreenCoinProgress -= OnGreenCoinProgress;
        GameEvents.OnAllGreenCoinsCollected -= OnAllGreenCoinsCollected;

        GameEvents.OnGamePaused -= OnGamePaused;
        GameEvents.OnGameResumed -= OnGameResumed;
        GameEvents.OnGameOverScreenShown -= OnGameOverScreenShown;
        GameEvents.OnWinScreenShown -= OnWinScreenShown;
        
        GameEvents.OnLevelStarted -= OnLevelStarted;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    #region Event Handlers

    private void OnLevelStarted()
    {
        isGameOverActive = false;
        isWinScreenActive = false;
        ShowAllUI();

        currentScore = GlobalVariables.score;
        currentLives = GlobalVariables.lives;
        currentCoins = GlobalVariables.coinCount;
        currentTime = GlobalVariables.infiniteTimeMode ? 0 : startingTimeFromLevel;

        LoadHighScore();
        LoadHighestRank();

        UpdateLevelNameUI();
        RefreshCheckpointIndicatorFromScene();
        UpdateAllUI();
    }

    private void OnLevelContextChanged(GameEvents.LevelContext ctx)
{
        currentLevelId = string.IsNullOrEmpty(ctx.LevelId) ? "unknown" : ctx.LevelId;
        startingTimeFromLevel = ctx.StartingTime > 0 ? ctx.StartingTime : 300f;

        // Match legacy: timer starts at startingTime unless infinite time
        currentTime = GlobalVariables.infiniteTimeMode ? 0 : startingTimeFromLevel;
        infiniteTimeActive = GlobalVariables.infiniteTimeMode;

        UpdateLevelNameUI();
        UpdateTimerUI();
        RefreshCheckpointIndicatorFromScene();
    }

    private void RefreshCheckpointIndicatorFromScene(int fallbackCheckpointId = -1)
    {
        if (checkpointIndicator == null) return;

        var checkpointManager = FindObjectOfType<CheckpointManager>();

        // Prefer the real in-scene state
        if (checkpointManager != null)
        {
            checkpointIndicator.SetActive(checkpointManager.HasCheckpoint);
            return;
        }

        // Fallback to event data if manager isn't found (rare)
        checkpointIndicator.SetActive(fallbackCheckpointId != -1);
    }


    private void OnCheckpointChanged(int checkpointId)
    {
        RefreshCheckpointIndicatorFromScene(checkpointId);
    }

    private void RefreshLevelContextForDebug()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        if (GlobalVariables.levelInfo != null && GlobalVariables.levelInfo.levelScene == activeScene)
        {
            currentLevelId = GlobalVariables.levelInfo.levelID;
            UpdateLevelNameUI();
            return;
        }

        if (levelDatabase != null && levelDatabase.TryGetByScene(activeScene, out var inferred))
        {
            GlobalVariables.levelInfo = inferred;
            currentLevelId = inferred.levelID;
        }
        else
        {
            currentLevelId = activeScene;
        }

        UpdateLevelNameUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshLevelContextForDebug();
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateLevelNameUI();
    }

    private void UpdateLevelNameUI()
    {
        if (levelNameText == null) return;

        if (levelNameRoutine != null) StopCoroutine(levelNameRoutine);
        levelNameRoutine = StartCoroutine(UpdateLevelNameUIRoutine());
    }

    private IEnumerator UpdateLevelNameUIRoutine()
    {
        // Ensure localization system is initialized
        yield return LocalizationSettings.InitializationOperation;

        string key = "Level_" + currentLevelId;

        var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("Game Text", key);
        yield return op;

        if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded
            && !string.IsNullOrEmpty(op.Result))
        {
            levelNameText.text = op.Result;
        }
        else
        {
            levelNameText.text = currentLevelId;
        }

        levelNameRoutine = null;
    }

    private void OnScoreChanged(int newScore)
    {
        currentScore = newScore;
        UpdateScoreUI();
    }

    private void OnHighScoreChanged(int newHighScore)
    {
        currentHighScore = newHighScore;
        UpdateHighScoreUI();
        
        /*if (enableAnimations && newHighScore > 0)
        {
            StartCoroutine(AnimateTextColor(highScoreText, flashColor, animationDuration));
        }*/
    }

    private void OnLivesChanged(int newLives)
    {
        int oldLives = currentLives;
        currentLives = newLives;
        UpdateLivesUI();

        if (!enableAnimations) return;

        if (newLives > oldLives)
            FlashText(ref livesFlashRoutine, livesText, originalLivesColor, Color.green, animationDuration);
    }

    private void OnScoreAdded(int amountAdded)
    {
        /*if (enableAnimations && scoreText != null)
        {
            // Show popup text with +amount
            StartCoroutine(AnimateTextColor(scoreText, flashColor, 0.3f));
        }*/
    }

    private void OnCoinsChanged(int newCoins)
    {
        currentCoins = newCoins;
        UpdateCoinsUI();
    }

    private void OnCoinsAdded(int amountAdded)
    {
        /*if (enableAnimations && coinText != null)
        {
            StartCoroutine(AnimateTextColor(coinText, flashColor, 0.3f));
        }*/
    }

    private void OnExtraLifeGained()
    {
        currentLives = GlobalVariables.lives;
        UpdateLivesUI();
        
        // NOTE: ref is just a reference to the original, not a copy, so it will update the actual routine variable
        FlashText(ref livesFlashRoutine, livesText, originalLivesColor, Color.green, animationDuration);
    }

    private void OnTimerChanged(float newTime)
    {
        currentTime = newTime;
        UpdateTimerUI();
    }

    private void OnTimeWarning()
    {
        if (enableAnimations && timerText != null)
        {
            StartCoroutine(FlashTimerWarning());
        }
    }

    private void OnTimeUp()
    {
        if (timerText != null)
        {
            timerText.text = "<mspace=0.8em>000";
            if (enableAnimations)
                FlashText(ref timerFlashRoutine, timerText, originalTimerColor, Color.red, 0.5f);
        }
    }

    private void OnSpeedrunTimeChanged(TimeSpan newTime)
    {
        currentSpeedrunTime = newTime;
        UpdateSpeedrunTimerUI();
    }

    private void OnRankChanged(PlayerRank rank)
    {
        currentRank = rank;
        UpdateCurrentRankUI();
    }

    private void OnHighestRankChanged(PlayerRank rank)
    {
        highestRank = rank;
        UpdateHighestRankUI();
        
        if (enableAnimations && highestRankImage != null)
        {
            StartCoroutine(AnimateRawImageScale(highestRankImage, 1.2f, 0.3f));
        }
    }

    private void OnGreenCoinCollected(GameObject coin)
    {
        // Optional: play collection effect
    }

    private void OnGreenCoinProgress(int collected, int total, int index)
    {
        if (greenCoinImages == null || greenCoinImages.Length == 0)
            return;

        if (index < 0 || index >= greenCoinImages.Length)
            return;

        if (greenCoinImages[index] != null && greenCoinCollectedSprite != null)
        {
            greenCoinImages[index].sprite = greenCoinCollectedSprite;
            
            if (enableAnimations)
            {
                StartCoroutine(AnimateImageScale(greenCoinImages[index], 1.3f, 0.2f));
            }
        }
    }

    private void OnAllGreenCoinsCollected(bool allCollected)
    {
        if (allCollected && enableAnimations)
        {
            // Celebrate all coins collected
            foreach (var coinImage in greenCoinImages)
            {
                if (coinImage != null)
                {
                    StartCoroutine(AnimateImageScale(coinImage, 1.5f, 0.5f));
                }
            }
        }
    }

    private void OnGamePaused()
    {
        isPaused = true;
        // Optional: dim HUD
        if (hudCanvas != null)
        {
            CanvasGroup canvasGroup = hudCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.5f;
            }
        }
    }

    private void OnGameResumed()
    {
        isPaused = false;
        // Restore HUD
        if (hudCanvas != null)
        {
            CanvasGroup canvasGroup = hudCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
    }

    private void OnGameOverScreenShown()
    {
        isGameOverActive = true;
        HideAllUI();
    }

    private void OnWinScreenShown()
    {
        isWinScreenActive = true;
        HideAllUI();
    }

    #endregion

    #region UI Updates

    private void UpdateAllUI()
    {
        UpdateScoreUI();
        UpdateHighScoreUI();
        UpdateLivesUI();
        UpdateCoinsUI();
        UpdateTimerUI();
        UpdateSpeedrunTimerUI();
        UpdateCurrentRankUI();
        UpdateHighestRankUI();
        UpdateModifiersUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "<mspace=0.8em>" + currentScore.ToString("D9");
    }

    private void UpdateHighScoreUI()
    {
        if (highScoreText != null)
            highScoreText.text = "<mspace=0.8em>" + currentHighScore.ToString("D9");
    }

    private void UpdateLivesUI()
    {
        if (livesText == null) return;
        
        if (infiniteLivesActive)
        {
            livesText.gameObject.SetActive(false);
            if (infiniteLivesImage != null)
                infiniteLivesImage.gameObject.SetActive(true);
        }
        else
        {
            livesText.gameObject.SetActive(true);
            livesText.text = "<mspace=0.8em>" + Mathf.Max(0, currentLives).ToString("D2");
            if (infiniteLivesImage != null)
                infiniteLivesImage.gameObject.SetActive(false);
        }
    }

    private void UpdateCoinsUI()
    {
        if (coinText != null)
            coinText.text = "<mspace=0.8em>" + currentCoins.ToString("D2");
    }

    private void UpdateTimerUI()
    {
        if (infiniteTimeActive)
        {
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (infiniteTimeImage != null) infiniteTimeImage.gameObject.SetActive(true);
        }
        else
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                timerText.text = "<mspace=0.8em>" + Mathf.Max(0, Mathf.FloorToInt(currentTime)).ToString("D3");
            }
            if (infiniteTimeImage != null) infiniteTimeImage.gameObject.SetActive(false);
        }
    }

    private void UpdateSpeedrunTimerUI()
    {
        if (speedrunTimerText == null)
            return;

        speedrunTimerText.gameObject.SetActive(speedrunTimerVisible);

        if (speedrunTimerVisible)
            speedrunTimerText.text = "<mspace=0.8em>" + currentSpeedrunTime.ToString(@"m\:ss\.ff");
    }

    private void UpdateCurrentRankUI()
    {
        if (currentRankImage == null || rankVisuals == null) return;

        Sprite sprite = rankVisuals.GetSprite(currentRank);
        if (sprite != null)
            currentRankImage.texture = sprite.texture;
    }

    private void UpdateHighestRankUI()
    {
        if (highestRankImage == null || rankVisuals == null) return;

        Sprite sprite = rankVisuals.GetSprite(highestRank);
        if (sprite != null)
            highestRankImage.texture = sprite.texture;
    }

    private void UpdateModifiersUI()
    {
        if (infiniteLivesImage != null)
            infiniteLivesImage.gameObject.SetActive(infiniteLivesActive);

        if (infiniteTimeImage != null)
            infiniteTimeImage.gameObject.SetActive(infiniteTimeActive);
    }

    private void ResetAnimatedTextColors()
    {
        if (livesFlashRoutine != null) { StopCoroutine(livesFlashRoutine); livesFlashRoutine = null; }
        if (timerFlashRoutine != null) { StopCoroutine(timerFlashRoutine); timerFlashRoutine = null; }

        if (timerText != null) timerText.color = originalTimerColor;
        if (livesText != null) livesText.color = originalLivesColor;
    }

    #endregion

    #region Data Loading

    private void LoadHighScore()
    {
        string levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        var progressStore = new ProgressStore();

        if (progressStore.TryGetLevel(levelId, out var levelData))
            currentHighScore = levelData.highScore;
        else
            currentHighScore = 0;
    }

    private void LoadHighestRank()
    {
        string levelId = GlobalVariables.levelInfo?.levelID ?? "unknown";
        var progressStore = new ProgressStore();

        if (progressStore.TryGetLevel(levelId, out var levelData))
            highestRank = (PlayerRank)levelData.highestRank;
        else
            highestRank = PlayerRank.Default;
    }

    #endregion

    #region UI Visibility

    public void HideAllUI()
    {
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(false);
            return;
        }

        // Fallback: hide individual elements
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (highScoreText != null) highScoreText.gameObject.SetActive(false);
        if (livesText != null) livesText.gameObject.SetActive(false);
        if (coinText != null) coinText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (speedrunTimerText != null) speedrunTimerText.gameObject.SetActive(false);

        if (currentRankImage != null) currentRankImage.gameObject.SetActive(false);
        if (highestRankImage != null) highestRankImage.gameObject.SetActive(false);

        if (infiniteTimeImage != null) infiniteTimeImage.gameObject.SetActive(false);
        if (infiniteLivesImage != null) infiniteLivesImage.gameObject.SetActive(false);

        if (greenCoinImages != null)
        {
            foreach (var coinImage in greenCoinImages)
            {
                if (coinImage != null) coinImage.gameObject.SetActive(false);
            }
        }
    }

    public void ShowAllUI()
    {
        if (isGameOverActive || isWinScreenActive)
            return;
            
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(true);
            return;
        }

        // Fallback: show individual elements
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (highScoreText != null) highScoreText.gameObject.SetActive(true);
        if (livesText != null) livesText.gameObject.SetActive(!infiniteLivesActive);
        if (coinText != null) coinText.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(!infiniteTimeActive);
        if (speedrunTimerText != null) speedrunTimerText.gameObject.SetActive(speedrunTimerVisible);
        if (currentRankImage != null) currentRankImage.gameObject.SetActive(true);
        if (highestRankImage != null) highestRankImage.gameObject.SetActive(true);
        
        if (infiniteLivesImage != null) infiniteLivesImage.gameObject.SetActive(infiniteLivesActive);
        if (infiniteTimeImage != null) infiniteTimeImage.gameObject.SetActive(infiniteTimeActive);
        
        if (greenCoinImages != null)
        {
            foreach (var coinImage in greenCoinImages)
            {
                if (coinImage != null) coinImage.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    #region Animations

    private void FlashText(ref Coroutine routine, TMP_Text text, Color baseColor, Color flash, float duration)
    {
        if (!enableAnimations || text == null) return;

        if (routine != null) StopCoroutine(routine);

        // Importante: fuerza un "base" estable antes de animar (evita baseColor a mitad)
        text.color = baseColor;

        routine = StartCoroutine(AnimateTextColor(text, baseColor, flash, duration));
    }

    private IEnumerator AnimateTextColor(TMP_Text text, Color baseColor, Color flashColor, float duration)
    {
        if (text == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime; // se congela con pausa y continúa al quitar pausa
            float k = Mathf.Clamp01(t / duration);
            text.color = Color.Lerp(baseColor, flashColor, k);
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            text.color = Color.Lerp(flashColor, baseColor, k);
            yield return null;
        }

        text.color = baseColor;
    }

    private IEnumerator AnimateImageScale(Image image, float targetScale, float duration)
    {
        if (image == null) yield break;
        
        Vector3 originalScale = image.rectTransform.localScale;
        Vector3 targetScaleVec = originalScale * targetScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float bounce = 1f + (targetScale - 1f) * Mathf.Sin(t * Mathf.PI);
            image.rectTransform.localScale = originalScale * bounce;
            yield return null;
        }
        
        image.rectTransform.localScale = originalScale;
    }

    private IEnumerator AnimateRawImageScale(RawImage image, float targetScale, float duration)
    {
        if (image == null) yield break;
        
        Vector3 originalScale = image.rectTransform.localScale;
        Vector3 targetScaleVec = originalScale * targetScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float bounce = 1f + (targetScale - 1f) * Mathf.Sin(t * Mathf.PI);
            image.rectTransform.localScale = originalScale * bounce;
            yield return null;
        }
        
        image.rectTransform.localScale = originalScale;
    }

    private IEnumerator FlashTimerWarning()
    {
        if (timerText == null) yield break;
        
        float flashInterval = 0.3f;
        float warningDuration = 3f;
        float elapsed = 0f;
        Color originalColor = timerText.color;
        
        while (elapsed < warningDuration && currentTime <= 100f)
        {
            timerText.color = Color.red;
            yield return new WaitForSeconds(flashInterval * 0.5f);
            timerText.color = originalColor;
            yield return new WaitForSeconds(flashInterval * 0.5f);
            elapsed += flashInterval;
        }
        
        timerText.color = originalColor;
    }

    #endregion

    #region Public Methods (for external calls)

    public void ForceUpdateAllUI()
    {
        UpdateAllUI();
    }

    public void RefreshHighScore()
    {
        LoadHighScore();
        UpdateHighScoreUI();
    }

    public void SetSpeedrunTimerVisibility(bool visible)
    {
        speedrunTimerVisible = visible;
        UpdateSpeedrunTimerUI();
    }

    public void SetInfiniteLivesMode(bool active)
    {
        infiniteLivesActive = active;
        UpdateLivesUI();
        UpdateModifiersUI();
    }

    public void SetInfiniteTimeMode(bool active)
    {
        infiniteTimeActive = active;
        UpdateTimerUI();
        UpdateModifiersUI();
    }

    #endregion

    #region Properties

    public bool IsHUDVisible => hudCanvas != null ? hudCanvas.activeSelf : scoreText?.gameObject.activeSelf == true;
    public int CurrentScore => currentScore;
    public int CurrentHighScore => currentHighScore;
    public int CurrentLives => currentLives;
    public int CurrentCoins => currentCoins;
    public float CurrentTime => currentTime;
    public PlayerRank CurrentRank => currentRank;
    public PlayerRank HighestRank => highestRank;

    #endregion
}