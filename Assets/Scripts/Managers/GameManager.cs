using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using static LeanTween;
using System.Linq;
using System.Globalization;
using UnityEngine.Localization.Settings;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // It allows me to access other scripts
    public static GameManager Instance { get; private set; }
    private string levelID;

    [HideInInspector]
    public float currentTime;

    [Header("Options Menu actions")]
    public bool isOptionsMenuLevel = false;

    public bool hideCursor = true;

    public GameObject levelUI;

    [Header("Timer")]
    public float startingTime;
    public static bool isPaused = false;
    private static bool pauseable = true; // turn off when win screen shows up or when fading to another scene
    private bool isTimeUp = false;
    private bool stopTimer = false;
    public float timeWarningTimer = 3f;
    [SerializeField] private GameObject timeWarningOverridePrefab;
    private GameObject timeWarningInstance;
    private bool timeWarningActive = false;
    [SerializeField] protected TMP_Text timerText;
    public Image infiniteTimeImage;
    [SerializeField] protected TMP_Text speedrunTimerText;

    [Header("Time Bonus (Timer Points)")]
    public bool awardTimeBonusOnFinish = true;
    public bool animateTimeBonusOnFinish = true;
    public int timeBonusPerSecond = 50; // classic SMB feel
    public float timeBonusTickRealtime = 0.02f; // tick speed (uses realtime)
    public int timeBonusFastChunkThreshold = 100; // above this, tick faster in chunks
    [Min(0)] public int timeBonusFastChunkSize = 5; // subtract 5 "seconds" per tick when large
    private int finishTimeSnapshot = -1;

    [Header("Time Bonus SFX")]
    public AudioClip timeBonusTickSfx;
    public float timeBonusTickVolume = 1f;
    public int timeBonusTickEverySteps = 1;
    
    [Header("Lives")]
    private int maxLives = 99;
    [SerializeField] TMP_Text livesText;
    public Image infiniteLivesImage;
    public Color defaultColor = Color.white;
    public Color targetColor = Color.green;

    [Header("Coin System")]
    [SerializeField] TMP_Text coinText;
    private int totalCoins = 0;
    public bool saveCoinsAfterDeath = true; // set false for coin door levels

    [Header("Green coins")]
    public GameObject[] greenCoins; // Array of green coin GameObjects in the scene
    public List<Image> greenCoinUIImages; // List of UI Image components representing green coins
    public Sprite collectedSprite; // Sprite for the collected state

    [Header("On-screen controls")]
    public GameObject onScreenControls;

    [Header("Checkpoints")]
    private List<Checkpoint> checkpoints = new List<Checkpoint>(); // List of checkpoints in the scene

    #region GreenCoindata
    private List<GameObject> collectedGreenCoins = new List<GameObject>();  // ALL green coins ever collected
    private List<GameObject> collectedGreenCoinsInRun = new List<GameObject>(); // Green coins collected in the current run

    [Header("Players")]
    private List<MarioMovement> players = new();  // The players will tell the game manager who they are on start or when the player changes

    public InputActionAsset playerInputActions; // Used to force update player input actions on resume from pause

    void SaveCollectedCoins()
    {
        foreach (GameObject coin in collectedGreenCoins)
        {
            int coinIndex = Array.IndexOf(greenCoins, coin);
            PlayerPrefs.SetInt("CollectedCoin" + coinIndex + "_" + levelID, 1);
        }
    }

    // There are 2 KINDS of collected green coins:
    // 1. Green coins that you get and then beat the level. These show in the level selection screen. The coins will be partially transparent in the next run.
    // 2. Green coins that you get and then hit a checkpoint. If you exit the level and come back, these coins will be gone.
    //    If you lose the saved progress, these coins will go back to not being collected.
    public void LoadCollectedCoins()
    {
        for (int i = 0; i < greenCoins.Length; i++)
        {
            // Collected green coins (the ones you get and then beat the level)
            if (PlayerPrefs.GetInt("CollectedCoin" + i + "_" + levelID, 0) == 1)
            {
                GameObject coinObject = greenCoins[i];

                collectedGreenCoins.Add(coinObject);

                // Change the alpha of the sprite renderer to indicate it's collected in a previous run
                SpriteRenderer coinRenderer = coinObject.GetComponent<SpriteRenderer>();
                Color coinColor = coinRenderer.color;
                coinColor.a = 0.5f;
                coinRenderer.color = coinColor;

                // Update UI for the collected coin
                Image uiImage = greenCoinUIImages[i];
                uiImage.sprite = collectedSprite;
            }

            // Saved green coins (the ones you get and then hit a checkpoint)
            // Check if the level is the saved level
            // We need to check here because LevelSelectionManager can't load the green coins for the saved level
            if (levelID != PlayerPrefs.GetString("SavedLevel", "none"))
            {
                continue;
            }

            if (PlayerPrefs.GetInt("SavedGreenCoin" + i, 0) == 1)
            {
                GameObject coinObject = greenCoins[i];

                collectedGreenCoins.Add(coinObject);
                collectedGreenCoinsInRun.Add(coinObject);

                // Destroy the green coin object (to indicate it was collected before hitting a checkpoint)
                Destroy(coinObject);

                // Update UI for the collected coin
                Image uiImage = greenCoinUIImages[i];
                uiImage.sprite = collectedSprite;
            }

        }
    }
    #endregion

    [Header("High Score System")]
    public int highScore;
    [SerializeField] TMP_Text highScoreText;

    [Header("Score System")]
    [SerializeField] TMP_Text scoreText;
    
    [Header("Pause Menu")]
    public GameObject pausemenu;
    public GameObject mainPauseMenu;
    public GameObject CheckpointIndicator;
    public GameObject ResetPopUp;
    public GameObject optionsPauseMenu;
    public GameObject[] disablingMenusOnResume; // Other menus to disable when the game is resumed
    public TMP_Text levelNameText;
    public Button resumeButton;
    public InputAction pauseAction;
    // TEMPORARY
    public Button optionsButton;
    public Button restartButton;
    public Button optionsBackButton;
    public Button restartFromBeginningButton;
    public Slider masterSlider;
    public Button restartButtonWinScreen;

    [Header("Rank")]
    public RawImage currentRankImage;
    public RawImage highestRankImage;
    public Sprite questionsprite;
    public Sprite[] rankTypes;
    public UnityEvent onSetCurrentRank;

    [Header("Rank conditions")]
    public int scoreForSRank = 10000;
    public int scoreForARank = 9000;
    public int scoreForBRank = 7000;
    public int scoreForCRank = 5000;
    public int scoreForDRank = 3000;

    private PlayerRank highestRank; // The highest rank achieved after the level is completed (saved and loaded from PlayerPrefs)
    private PlayerRank prevRank;  // The previous rank, to see if the rank has changed
    private PlayerRank currentRank; // The current rank

    #region RankSystem
    public enum PlayerRank
    {
        Default,    // Question mark
        D,          // Rotten Mushroom
        C,          // Mushroom
        B,          // Fire Flower
        A,          // 1up
        S           // Star
    }

    private bool AllEnemiesKilled()
    {
        // Check if there are any objects with the tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        return enemies.Length == 0; // True if there are no enemies, false otherwise
    }

    private void UpdateRank()
    {
        if (GlobalVariables.score >= scoreForSRank)
        {
            currentRankImage.texture = rankTypes[4].texture; // S Rank
            currentRank = PlayerRank.S;
        }
        else if (GlobalVariables.score >= scoreForARank)
        {
            currentRankImage.texture = rankTypes[3].texture; // A Rank
            currentRank = PlayerRank.A;
        }
        else if (GlobalVariables.score >= scoreForBRank)
        {
            currentRankImage.texture = rankTypes[2].texture; // B Rank
            currentRank = PlayerRank.B;
        }
        else if (GlobalVariables.score >= scoreForCRank)
        {
            currentRankImage.texture = rankTypes[1].texture; // C Rank
            currentRank = PlayerRank.C;
        }
        else if (GlobalVariables.score >= scoreForDRank)
        {
            currentRankImage.texture = rankTypes[0].texture; // D Rank
            currentRank = PlayerRank.D;
        }
        else
        {
            if (highestRank == PlayerRank.Default)
            {
                currentRankImage.texture = questionsprite.texture; // Default
            }
        }

        // Check if the current rank is higher than the previous rank (last frame)
        if (currentRank > prevRank)
        {
            prevRank = currentRank;

            SetCurrentRank(currentRank);
        }
    }

    private void SetCurrentRank(PlayerRank newRank)
    {
        currentRank = newRank;
        onSetCurrentRank.Invoke(); // Trigger the scale animation
    }

    private void SaveHighestRank(PlayerRank rank)
    {
        // Save the highest rank to PlayerPrefs
        PlayerPrefs.SetInt("HighestPlayerRank_" + levelID, (int)rank);
    }

    private PlayerRank LoadHighestRank()
    {
        // Load the highest rank from PlayerPrefs, defaulting to "Default" if it doesn't exist.
        return (PlayerRank)PlayerPrefs.GetInt("HighestPlayerRank_" + levelID, (int)PlayerRank.Default);
    }

    private void ResetCurrentRank()
    {
        currentRank = PlayerRank.Default;
        currentRankImage.texture = questionsprite.texture; // Set currentRankImage to the default texture
    }
    #endregion

    [Header("Game Over & Lose Life")]
    // Game Over Screen Object
    public GameObject GameOverScreenGameObject;
    // Name of the Lose Life scene
    public string loseLifeSceneName;

    [Header("Key System")]
    public List<GameObject> keys = new List<GameObject>();

    private AudioSource audioSource;
    // List to keep track of all PauseableObject scripts.
    private List<PauseableObject> pauseableObjects = new List<PauseableObject>();
    private float originalVolume;

    [Header("Win Screen")]
    public GameObject WinScreenGameObject;
    [SerializeField] TMP_Text timerFinishText;
    [SerializeField] TMP_Text collectedCoinsText;
    [SerializeField] TMP_Text totalCoinsText;
    [SerializeField] TMP_Text scoreWinScreenText;
    [SerializeField] TMP_Text speedrunTimeFinishText;
    public GameObject speedrunTimeFinishBox;    // The object that holds the speedrun time info
    public GameObject NewBestRankText;
    public GameObject NewHighScoreText;
    public List<Image> greenCoinUIWin;
    public RawImage ObtainedRank;

    [Header("Cheats")]
    public GameObject tinyMarioPrefab;
    public GameObject iceMarioPrefab;
    public GameObject fireMarioPrefab;

    void Awake()
    {
        print("GameManager Awake");
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            print("Destroying duplicate GameManager instance");
            Destroy(gameObject);
        }
        audioSource = GetComponent<AudioSource>();

        if (optionsBackButton != null && optionsButton != null)
        {
            optionsBackButton.onClick.AddListener(() =>
                EventSystem.current.SetSelectedGameObject(optionsButton.gameObject)
            );
        }
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        pauseable = true;
        isPaused = false;
        if (hideCursor)
        {
            CursorHelper.HideCursor();
        }

        if (isOptionsMenuLevel)
        {
            // Options menu-specific settings
            ApplyOptionsMenuSettings();
        }

        currentTime = startingTime;

        if (GlobalVariables.levelInfo == null)
        {
            GlobalVariables.levelInfo = TestLevelInfo();
        }

        levelID = GlobalVariables.levelInfo.levelID;
        Debug.Log("Current level ID: " + levelID);

        if (!isOptionsMenuLevel)
        {
            // Load the high score from PlayerPrefs, defaulting to 0 if it doesn't exist.
            highScore = PlayerPrefs.GetInt("HighScore", 0);

            // Load the highest rank from PlayerPrefs
            highestRank = LoadHighestRank();

            // Set the texture for highestRankImage based on the loaded highest rank
            if (highestRank != PlayerRank.Default)
                highestRankImage.texture = rankTypes[(int)highestRank - 1].texture;

            // Set level name text
            levelNameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Level_" + levelID);
        }

        if (!saveCoinsAfterDeath)
        {
            GlobalVariables.coinCount = 0;
        }

        GetTotalCoins();    // TODO: remove because we are not tracking total coins anymore
        ResetCurrentRank();
        if (!isOptionsMenuLevel)
        {
            LoadCollectedCoins(); // Load collected coins data from PlayerPrefs
            UpdateHighScoreUI();
            UpdateLivesUI();
            UpdateCoinsUI();
            UpdateScoreUI();
            InitSpeedrunTimer();
        }
        CheckForInfiniteTime();
    }

    void OnEnable()
    {
        pauseAction.Enable();
    }

    void OnDisable()
    {
        pauseAction.Disable();
    }

    // So no error when running starting in the level scene
    LevelInfo TestLevelInfo()
    {
        LevelInfo info = ScriptableObject.CreateInstance<LevelInfo>();
        info.levelID = "test";
        info.levelScene = SceneManager.GetActiveScene().name;
        info.lives = 3;
        info.videoYear = "Unknown";
        info.videoLink = "Unknown";
        return info;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (levelNameText != null)
        {
            levelNameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Level_" + levelID);
        }

        if (CheckpointIndicator != null)
        {
            if (GlobalVariables.checkpoint != -1)
            {
                CheckpointIndicator.SetActive(true);
            }
            else
            {
                CheckpointIndicator.SetActive(false);
            }
        }

        // Check for pause input only if the game is not over
        if (!isTimeUp)
        {
            UpdateRank();
            UpdateSpeedrunTimerUI();

            if (pauseAction.WasPressedThisFrame())
            {
                if (!isOptionsMenuLevel || GetComponent<OptionsGameManager>().CanTogglePause())
                {
                    TogglePauseGame();
                }
            }

            // Check if enablePlushies is true, then activate "Plushie" objects.
            if (GlobalVariables.cheatPlushies)
            {
                ActivatePlushieObjects();
            }
            else
            {
                DeactivatePlushieObjects();
            }

            if (!GlobalVariables.stopTimeLimit) // If it's false
            {
                if (!isPaused && !stopTimer)
                {
                    // Timer decrease
                    currentTime -= 1 * Time.deltaTime;
                    UpdateTimerUI();

                    if (GlobalVariables.lives == maxLives)
                    {
                        GlobalVariables.lives = 99;
                    }

                    // TURN ON warning when crossing threshold
                    if (currentTime <= 100f && !timeWarningActive)
                    {
                        timeWarningInstance = Instantiate(timeWarningOverridePrefab);
                        timeWarningInstance.GetComponent<MusicOverride>()?.stopPlayingAfterTime(timeWarningTimer);
                        timeWarningActive = true;
                    }
                    else if (currentTime > 100f && timeWarningActive)
                    {
                        StopTimeWarningMusic();
                    }

                    if (currentTime <= 0 && !isTimeUp)
                    {
                        currentTime = 0;
                        isTimeUp = true;
                        // Debug.Log("Stop music!");
                        // Debug.Log("The time has run out!");
                        DecrementLives();
                    }
                }
            }
        }
    }

    private void AwardTimeBonusInstant()
    {
        if (!awardTimeBonusOnFinish) return;
        if (GlobalVariables.stopTimeLimit) return;

        int timeLeft = Mathf.Max(0, Mathf.FloorToInt(currentTime));
        AddScorePoints(timeLeft * timeBonusPerSecond);
        
        currentTime = 0;
        UpdateTimerUI();
    }

    private IEnumerator AnimateTimeBonus()
    {
        // No bonus if infinite time mode
        if (!awardTimeBonusOnFinish) yield break;
        if (GlobalVariables.stopTimeLimit) yield break;

        int timeLeft = Mathf.Max(0, Mathf.FloorToInt(currentTime));

        // Show the starting time on the win screen immediately
        if (timerText != null)
            timerText.text = timeLeft.ToString("D3");

        int sfxCounter = 0;

        while (timeLeft > 0)
        {
            int step = 1;
            if (timeLeft > timeBonusFastChunkThreshold)
                step = Mathf.Min(timeBonusFastChunkSize, timeLeft);

            timeLeft -= step;
            currentTime = timeLeft;

            UpdateTimerUI();
            AddScorePoints(step * timeBonusPerSecond);

            sfxCounter += step;
            if (timeBonusTickSfx != null && audioSource != null && (timeBonusTickEverySteps <= 1 || sfxCounter >= timeBonusTickEverySteps))
            {
                audioSource.PlayOneShot(timeBonusTickSfx, timeBonusTickVolume);
                sfxCounter = 0;
            }

            yield return new WaitForSeconds(timeBonusTickRealtime);
        }
    }

    public void StopTimeWarningMusic()
    {
        if (timeWarningInstance != null)
            Destroy(timeWarningInstance);

        timeWarningInstance = null;
        timeWarningActive = false;
    }

    private void ApplyOptionsMenuSettings()
    {
        // Disable level UI
        HideUI();
        // Stop timer
        stopTimer = true;

        // Pause the game to start
        PauseGame();
    }

    public void Restart()
    {
        if (GlobalVariables.checkpoint != -1)
        {
            GoToResetPopUp();
        }
        else
        {
            RestartLevelFromBeginning();
        }
    }

    public void RestartLevelFromBeginning()
    {
        // reset lives, checkpoint, etc
        GlobalVariables.ResetForLevel();

        // Remove saved progress
        RemoveProgress();

        // turn off all music overrides
        MusicManager.Instance.ClearMusicOverrides(MusicManager.MusicStartMode.Continue);

        stopTimer = true;   // Stops speedrun timer from starting early

        // Reloads the level
        ReloadScene();

        // Unpause the game
        ResumeGame();
    }

    public void RestartLevelFromBeginningWithFadeOut()
    {
        // reset lives, checkpoint, etc
        GlobalVariables.ResetForLevel();

        // Remove saved progress
        RemoveProgress();

        // turn off all music overrides
        MusicManager.Instance.ClearMusicOverrides(MusicManager.MusicStartMode.Continue);

        // Reloads the level
        ReloadSceneWithFade();

        // Unpause the game
        ResumeGame();
    }

    public void RestartLevelFromCheckpoint()
    {
        // turn off all music overrides
        MusicManager.Instance.ClearMusicOverrides(MusicManager.MusicStartMode.Continue);

        // Reloads the level
        ReloadScene();

        // Unpause the game
        ResumeGame();
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReloadSceneWithFade()
    {
        FadeInOutScene.Instance.LoadSceneWithFade(SceneManager.GetActiveScene().name);
    }

    public void HideUI()
    {
        if (levelUI != null)
        {
            print("UI hidden");
            levelUI.SetActive(false);
        }
    }

    public virtual void DecrementLives()
    {
        MusicManager.Instance.ClearMusicOverrides(MusicManager.MusicStartMode.Continue);

        if (!GlobalVariables.infiniteLivesMode) // Check if the player is not in infinite lives mode
        {
            GlobalVariables.lives--;

            // Check if the player has run out of lives
            if (GlobalVariables.lives <= 0)
            {
                PlayerPrefs.Save();
                pauseable = false;
                // Enable the Game Over screen
                GameOverScreenGameObject.SetActive(true);
                HideUI();

                // Remove saved progress
                RemoveProgress();
            }
            else
            {
                // Save progress
                SaveProgress();

                // Load the LoseLife scene and restart the current level
                if (!FadeInOutScene.Instance.isTransitioning)
                {
                    SceneManager.LoadScene(loseLifeSceneName);
                }
            }
        }
        else
        {
            // Save progress
            SaveProgress();

            // Prevent reloading if a transition is already happening (e.g., quitting the level)
            if (!FadeInOutScene.Instance.isTransitioning)
            {
                ReloadScene();
            }
        }
    }

    public void AddLives()
    {
        GlobalVariables.lives++;
        UpdateLivesUI();

        // Start the color change coroutine
        StartCoroutine(AnimateColor(GlobalVariables.infiniteLivesMode ? infiniteLivesImage : livesText, targetColor, 0.5f));
    }

    private IEnumerator AnimateColor(Component target, Color overrideTargetColor, float duration = 1f)
    {
        if (target == null) yield break;

        Color initialColor = defaultColor;
        Color targetColor = overrideTargetColor;

        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            if (target is TMP_Text text)
                text.color = Color.Lerp(initialColor, targetColor, timeElapsed / duration);
            else if (target is Image image)
                image.color = Color.Lerp(initialColor, targetColor, timeElapsed / duration);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if (target is TMP_Text finalText)
            finalText.color = targetColor;
        else if (target is Image finalImage)
            finalImage.color = targetColor;

        yield return new WaitForSeconds(0.1f);

        timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            if (target is TMP_Text revertText)
                revertText.color = Color.Lerp(targetColor, initialColor, timeElapsed / duration);
            else if (target is Image revertImage)
                revertImage.color = Color.Lerp(targetColor, initialColor, timeElapsed / duration);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if (target is TMP_Text finalRevertText)
            finalRevertText.color = initialColor;
        else if (target is Image finalRevertImage)
            finalRevertImage.color = initialColor;
    }

    #region updateUI
    private void UpdateHighScore()
    {
        if (GlobalVariables.score > highScore)
        {
            highScore = GlobalVariables.score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
    }

    public void UpdateLivesUI()
    {
        livesText.gameObject.SetActive(!GlobalVariables.infiniteLivesMode); // if infinite lives mode is deactivated
        infiniteLivesImage.gameObject.SetActive(GlobalVariables.infiniteLivesMode); // if infinite lives mode is activated

        if (!GlobalVariables.infiniteLivesMode)
        {
            livesText.text = "<mspace=0.8em>" + GlobalVariables.lives.ToString("D2"); // 00
        }
    }

    private void UpdateCoinsUI()
    {
        coinText.text = "<mspace=0.8em>" + GlobalVariables.coinCount.ToString("D2"); // 00
    }

    private void UpdateTimerUI()
    {
        if (!GlobalVariables.stopTimeLimit)
        {
            timerText.text = "<mspace=0.8em>" + ((int)currentTime).ToString("D3"); // 000
        }
    }
    private void UpdateSpeedrunTimerUI()
    {
        if (GlobalVariables.SpeedrunMode && speedrunTimerText != null)
        {
            //string timeString = "ERROR!";
            //GlobalVariables.elapsedTime.TryFormat(timeString, 
            speedrunTimerText.text = "<mspace=0.8em>" + GlobalVariables.elapsedTime.ToString(@"m\:ss\.ff");
            // NOTE: the timer will currently appear to reset once it goes past 1 hour
        }
    }

    private static string BestTimeKey(string levelId) => $"BestTimeMs_{levelId}";

    private void UpdateBestTimeRecord()
    {
        // This will be used to store the time you took to complete the level
        // then this value will be used on select level scene to show the best time

        // store milliseconds (easy compare, culture-safe)
        double newMs = GlobalVariables.elapsedTime.TotalMilliseconds;

        string key = BestTimeKey(levelID);
        string prev = PlayerPrefs.GetString(key, "");

        bool hasPrev = double.TryParse(prev, NumberStyles.Float, CultureInfo.InvariantCulture, out double prevMs);

        if (!hasPrev || newMs < prevMs)
        {
            PlayerPrefs.SetString(key, newMs.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
    }

    private void CheckForInfiniteTime()
    {
        if (GlobalVariables.stopTimeLimit)
        {
            timerText.gameObject.SetActive(false);
            infiniteTimeImage.gameObject.SetActive(true);
        }
    }

    private void InitSpeedrunTimer()
    {
        if (speedrunTimerText == null) return;

        UpdateSpeedrunTimerVisiblity();

        GlobalVariables.speedrunTimer.Start();
    }

    private void UpdateScoreUI()
    {
        scoreText.text = "<mspace=0.8em>" + GlobalVariables.score.ToString("D9"); // 000000000
        
        if (WinScreenGameObject != null && WinScreenGameObject.activeInHierarchy && scoreWinScreenText != null)
            scoreWinScreenText.text = GlobalVariables.score.ToString("D9");
    }

    private void UpdateHighScoreUI()
    {
        highScoreText.text = highScore.ToString("D9"); // 000000000 | Display the high score
    }

    public void UpdateMobileControls()
    {
        if (onScreenControls != null)
        {
            onScreenControls.SetActive(GlobalVariables.OnScreenControls);
            onScreenControls.GetComponent<MobileControls>().UpdateButtonPosScaleOpacity();
        }
    }

    public void UpdateMobileOpacity(float buttonPressedOpacity, float buttonUnpressedOpacity)
    {
        Instance.onScreenControls.GetComponent<MobileControls>().UpdateButtonOpacity(buttonPressedOpacity, buttonUnpressedOpacity);
    }

    public void UpdateSpeedrunTimerVisiblity()
    {
        if (speedrunTimerText != null)
        {
            speedrunTimerText.gameObject.SetActive(GlobalVariables.SpeedrunMode);
        }
    }

    #endregion

    public void AddCoin(int coinValue)
    {
        GlobalVariables.coinCount += coinValue;
        GlobalVariables.score += coinValue * 100;

        if (GlobalVariables.coinCount > 99)
        {
            GlobalVariables.coinCount -= 100;
            AddLives();
        }

        UpdateCoinsUI();
        UpdateScoreUI();
    }

    public void RemoveCoins(int coins)
    {
        GlobalVariables.coinCount -= coins;
        UpdateCoinsUI();
    }

    public void SetCoinCount(int coinCount)
    {
        GlobalVariables.coinCount = coinCount;
        UpdateCoinsUI();
    }

    public void CollectGreenCoin(GameObject greenCoin)
    {
        AddScorePoints(2000);
        collectedGreenCoinsInRun.Add(greenCoin);    // This assumes you can't collect the same green coin twice in the same run

        // Uncomment this and remove the code in WinScreenStats() if you want to show green coins collected IN THE RUN instead of ALL green coins ever collected
        // Image uiImageWin = greenCoinUIWin[Array.IndexOf(greenCoins, greenCoin)];
        // uiImageWin.sprite = collectedSprite;

        // Check if the green coin is uncollected
        if (!collectedGreenCoins.Contains(greenCoin))
        {
            Debug.Log("Collecting green coin: " + greenCoin.name);

            Image uiImage = greenCoinUIImages[Array.IndexOf(greenCoins, greenCoin)];
            collectedGreenCoins.Add(greenCoin);
            if (uiImage != null)
            {
                uiImage.sprite = collectedSprite;
            }

            // Change the alpha of the sprite renderer to indicate it's collected
            SpriteRenderer coinRenderer = greenCoin.GetComponent<SpriteRenderer>();
            Color coinColor = coinRenderer.color;
            coinColor.a = 0.5f;
            coinRenderer.color = coinColor;
        }
    }

    #region TotalCoins
    // Get the totalCoins based on coins and block that contains blocks
    // TODO: Remove this because we are not tracking total coins anymore
    public void GetTotalCoins()
    {
        totalCoins = 0; // Reset totalCoins before counting

        // Find all objects with QuestionBlock script
        QuestionBlock[] questionBlocks = FindObjectsOfType<QuestionBlock>();

        foreach (QuestionBlock questionBlock in questionBlocks)
        {
            // Check the condition for adding coins
            if (questionBlock.spawnableItems.Length == 0 && !questionBlock.brickBlock)
            {
                // Add coins to the total count
                totalCoins++;
            }
        }

        // Find all object with Coin script
        Coin[] coins = FindObjectsOfType<Coin>();

        foreach (Coin coin in coins)
        {
            // Check if the coin type is not green
            if (coin.type != Coin.Amount.green)
            {
                // Sum the coin value to the total coins based on the GetCoinValue method which gets the coin value and sum it to totalCoins
                totalCoins += coin.GetCoinValue();
            }
        }

        // Find all objects with the "Coin" tag and add their count to totalCoins
        /*GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        totalCoins += coins.Length;*/
    }

    // Retrieve the totalCoins value if needed
    public int ShowTotalCoins()
    {
        totalCoinsText.text = ((int)totalCoins).ToString("D3");
        return totalCoins;
    }
    #endregion

    void CheckGameCompletion()
    {
        if (collectedGreenCoins.Count == greenCoins.Length)
        {
            Debug.Log("All green coins collected");
        }

        if (!GlobalVariables.infiniteLivesMode && !GlobalVariables.enableCheckpoints && !GlobalVariables.stopTimeLimit)
        {
            Debug.Log("You complete the level without advantages, Congrats! You did it! Yay :D!");
        }

        if (collectedGreenCoins.Count == greenCoins.Length && !GlobalVariables.infiniteLivesMode && !GlobalVariables.enableCheckpoints && !GlobalVariables.stopTimeLimit)
        {
            Debug.Log("Level completed perfect");
            PlayerPrefs.SetInt("LevelPerfect_" + levelID, 1);
        }
    }

    public void AddScorePoints(int pointsToAdd)
    {
        GlobalVariables.score += pointsToAdd;
        UpdateScoreUI();
    }

    // Stops both the level timer and the speedrun timer
    public void StopTimer()
    {
        stopTimer = true;
        GlobalVariables.speedrunTimer.Stop();
    }

    // Function to toggle the game between paused and resumed states.
    public void TogglePauseGame()
    {
        if (!isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    // Save the Level Progress
    public void SaveProgress()
    {
        // Save level progress if reached a checkpoint
        if (GlobalVariables.checkpoint != -1)
        {
            // Save the current number of lives to PlayerPrefs
            PlayerPrefs.SetInt("SavedLives", GlobalVariables.lives);

            // Save the current number of coins to PlayerPrefs
            PlayerPrefs.SetInt("SavedCoins", GlobalVariables.coinCount);

            // Save the green coins collected to PlayerPrefs
            // But in a different way than the other PlayerPrefs so that they aren't permanently saved even if you exit the level
            for (int i = 0; i < greenCoins.Length; i++)
            {
                if (collectedGreenCoinsInRun.Contains(greenCoins[i]))
                {
                    PlayerPrefs.SetInt("SavedGreenCoin" + i, 1);
                }
                else
                {
                    PlayerPrefs.SetInt("SavedGreenCoin" + i, 0);
                }
            }

            // Save the current checkpoint to PlayerPrefs
            PlayerPrefs.SetInt("SavedCheckpoint", GlobalVariables.checkpoint);

            // Save current speedrun time to PlayerPrefs
            PlayerPrefs.SetString("SavedSpeedrunTime", GlobalVariables.elapsedTime.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture));

            PlayerPrefs.SetString("SavedLevel", levelID);
        }
    }
    public void RemoveProgress()
    {
        PlayerPrefs.DeleteKey("SavedLives");
        PlayerPrefs.DeleteKey("SavedCoins");
        PlayerPrefs.DeleteKey("SavedCheckpoint");
        PlayerPrefs.DeleteKey("SavedLevel");
        for (int i = 0; i < greenCoins.Length; i++)
        {
            PlayerPrefs.DeleteKey("SavedGreenCoin" + i);
        }
    }

    // Quit Level
    public void QuitLevel()
    {
        PlayerPrefs.Save();

        // Destroy all music objects
        foreach (GameObject musicObj in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            Destroy(musicObj);
        }
        ResumeGame();
        CursorHelper.ShowCursor();
        pauseable = false;
        FadeInOutScene.Instance.LoadSceneWithFade("SelectLevel");
    }

    #region pausableobjects
    // Function to add objects with PauseableMovement scripts to the list.
    public void RegisterPauseableObject(PauseableObject pauseableObject)
    {
        pauseableObjects.Add(pauseableObject);
        //Debug.Log("Registered " + pauseableObject.gameObject.name + " to Pauseable Objects list.");
    }

    // Function to remove objects from the list.
    public void UnregisterPauseableObject(PauseableObject pauseableObject)
    {
        pauseableObjects.Remove(pauseableObject);
        //Debug.Log(pauseableObject.gameObject.name + " removed on Pauseable Objects list.");
    }

    // Function to pause all pauseable objects.
    public void PausePauseableObjects()
    {
        // Pause all pauseable objects if the list is not null.
        if (pauseableObjects != null)
        {
            foreach (PauseableObject obj in pauseableObjects)
            {
                obj.Pause();
            }
        }
    }

    // Function to resume all pauseable objects.
    public void ResumePauseableObjects()
    {
        // Resume all pauseable objects if the list is not null.
        if (pauseableObjects != null)
        {
            foreach (PauseableObject obj in pauseableObjects)
            {
                obj.Resume();
            }
        }
    }

    // for making objects fall straight down after the player touches the axe
    public void FallPauseableObjects()
    {
        // Fall all pauseable objects if the list is not null.
        if (pauseableObjects != null)
        {
            foreach (PauseableObject obj in pauseableObjects)
            {
                obj.FallStraightDown();
            }
        }
    }
    #endregion

    #region ActivatePlushies
    private void ActivatePlushieObjects()
    {
        GameObject[] plushieObjects = GameObject.FindGameObjectsWithTag("Plushie");
        foreach (var plushieObject in plushieObjects)
        {
            plushieObject.SetActive(true);
        }
    }

    private void DeactivatePlushieObjects()
    {
        GameObject[] plushieObjects = GameObject.FindGameObjectsWithTag("Plushie");
        foreach (var plushieObject in plushieObjects)
        {
            plushieObject.SetActive(false);
        }
    }

    private void OnApplicationQuit()
    {
        // Set enablePlushies to false when the game exits.
        GlobalVariables.cheatPlushies = false;
    }
    #endregion

    public void PauseGame()
    {
        if (pausemenu == null) return;
        if (!pauseable) return;

        isPaused = true;
        Time.timeScale = 0f; // Set time scale to 0 (pause)
        GlobalVariables.speedrunTimer.Stop(); // Stop speedrun timer

        originalVolume = MusicManager.Instance.GetCurrentVolume();
        MusicManager.Instance.SetCurrentVolume(originalVolume * 0.25f);

        CursorHelper.ShowCursor();

        foreach (MarioMovement player in players)
        {
            player.DisableInputs();
        }

        if (pausemenu != null)
        {
            // Activate the pause menu
            pausemenu.SetActive(true);
        }

        if (!isOptionsMenuLevel)
        {
            mainPauseMenu.SetActive(true);
            ResetPopUp.SetActive(false);
            optionsPauseMenu.SetActive(false);

            resumeButton.Select();  // Select the resume button by default
        }
        else
        {
            // Enable UI
            GetComponent<OptionsGameManager>().OnPause();
        }

    }

    public void ResumeGame()
    {
        isPaused = false;

        Time.timeScale = 1f; // Set time scale to normal (unpause)

        if (!stopTimer)
        {
            GlobalVariables.speedrunTimer.Start(); // Resume speedrun timer
        }

        MusicManager.Instance.SetCurrentVolume(originalVolume);

            
        if (hideCursor)
        {
            CursorHelper.HideCursor();
        }

        /*foreach (MarioMovement player in players)
        {
            player.EnableInputs();

            // Stupid workaround for Mario's input actions not updating when you rebind them in the options menu
            // Remove this if we ever update Unity and the issue is fixed
            player.GetComponent<PlayerInput>().actions = playerInputActions;
        }*/

        foreach (MarioMovement player in ExistingPlayers())
        {
            if (player == null) // extra safety
                continue;

            player.EnableInputs();

            // Workaround for input actions not updating on rebind
            var input = player.GetComponent<PlayerInput>();
            if (input != null)
            {
                input.actions = playerInputActions;
            }
        }

        if (pausemenu != null)
        {
            pausemenu.SetActive(false);
        }

        // Deactivate the pause menu
        if (!isOptionsMenuLevel)
        {
            mainPauseMenu.SetActive(true);
            ResetPopUp.SetActive(false);
            optionsPauseMenu.SetActive(false);
            foreach (GameObject menu in disablingMenusOnResume)
            {
                menu.SetActive(false);
            }
        }
        else
        {
            // Disable UI
            GetComponent<OptionsGameManager>().OnResume();
        }
    }

    public void GoToMainPauseMenu()
    {
        mainPauseMenu.SetActive(true);
        optionsPauseMenu.SetActive(false);

        optionsButton.Select();
    }

    public void OpenOptionsMenu()
    {
        mainPauseMenu.SetActive(false);
        optionsPauseMenu.SetActive(true);

        masterSlider.Select();
    }

    public void GoToResetPopUp()
    {
        restartFromBeginningButton.Select();
        ResetPopUp.SetActive(true);
    }

    public void GoToMainMenufromReset()
    {
        restartButton.Select();
        ResetPopUp.SetActive(false);
    }

    public void WinScreenStats()
    {
        // Win screen
        // Display total coins on the level
        ShowTotalCoins();

        // Time when you get to the end
        int timeToShow = (finishTimeSnapshot >= 0) ? finishTimeSnapshot : (int)currentTime;
        timerFinishText.text = timeToShow.ToString("D3");

        // Collected coins
        collectedCoinsText.text = GlobalVariables.coinCount.ToString("D2");

        // Removing this for now since you can get extra coins after dying
        // if (GlobalVariables.coinCount == totalCoins) { // If the amount of coins collected match the total coins on the level
        //     totalCoinsText.color = Color.yellow; // The total coins text will change to yellow
        // }

        // Score amount achieved
        scoreWinScreenText.text = GlobalVariables.score.ToString("D9");

        if (GlobalVariables.score > highScore)
        { // If the GlobalVariables.score is higher than highScore  on the level
            NewHighScoreText.SetActive(true); // A text saying "New HighScore!" will appear
        }
        else
        {
            NewHighScoreText.SetActive(false); // The text won't appear
        }

        // Green Coins
        foreach (GameObject greenCoin in collectedGreenCoins)
        {
            Image uiImageWin = greenCoinUIWin[Array.IndexOf(greenCoins, greenCoin)];
            uiImageWin.sprite = collectedSprite;
        }

        // Speedrun Time
        speedrunTimeFinishBox.SetActive(GlobalVariables.SpeedrunMode);
        if (GlobalVariables.SpeedrunMode)
        {
            speedrunTimeFinishText.text = GlobalVariables.elapsedTime.ToString(@"m\:ss\.ff");
        }

        // Save the highest rank to PlayerPrefs if the current rank is higher than the saved rank
        if (currentRank > highestRank)
        {
            highestRank = currentRank;
            SaveHighestRank(currentRank);

            if (highestRank != PlayerRank.Default) // You got a rank that isn't the question mark?
                NewBestRankText.SetActive(true); // A text saying "New Best!" will appear
        }
        else
        {
            NewBestRankText.SetActive(false); // The text won't appear
        }

        // Set the texture for highestRankImage based on the updated highest rank
        if (highestRank != PlayerRank.Default)
        {
            highestRankImage.texture = rankTypes[(int)highestRank - 1].texture;
        }
        // Ensure ObtainedRank matches the currentRank
        ObtainedRank.texture = currentRankImage.texture;
    }

    // TODO: Clean this method up and use it for all other ways of ending the level (flag, axe, others if any) (currently only used for Giant Thwomp)
    public IEnumerator TriggerEndLevelCutscene(PlayableDirector cutscene, float cutsceneDelay, float cutsceneLength, bool destroyPlayersImmediately, bool stopMusicImmediately, bool hideUI = false)
    {
        print("TriggerEndLevelCutscene");
        StopTimer();
        if (destroyPlayersImmediately)
        {
            // Delete all players immediately, without waiting for the cutscene to start
            foreach (MarioMovement player in players)
            {
                if (player != null)
                {
                    Destroy(player.gameObject);
                }
            }
            players.Clear(); // Clear the players list since all players have been destroyed, without this it causes a bug when trigger ending cutscenes
        }
        print("DestroyPlayersImmediately complete");

        if (stopMusicImmediately)
        {
            MusicManager.Instance.MuteAllMusic();
        }
        if (cutsceneDelay > 0)
        {
            yield return new WaitForSeconds(cutsceneDelay);
            print($"Waited {cutsceneDelay} seconds before starting the cutscene.");
        }

        print("Cutscene start");

        if (!destroyPlayersImmediately)
        {
            // Delete all players as soon as the cutscene starts
            foreach (MarioMovement player in players)
            {
                if (player != null)
                {
                    player.gameObject.SetActive(false);
                }
            }
        }

        if (!stopMusicImmediately)
        {
            MusicManager.Instance.MuteAllMusic();
        }

        cutscene.Play();
        yield return new WaitForSeconds(cutsceneLength);
        print($"Cutscene played for {cutsceneLength} seconds.");

        print("Cutscene end");
        FinishLevel();
    }

    // after level ends, call this (ex: flag cutscene ends)
    public void FinishLevel()
    {
        StartCoroutine(FinishLevelCoroutine());
    }

    private IEnumerator FinishLevelCoroutine()
    {
        pauseable = false;

        finishTimeSnapshot = Mathf.Max(0, Mathf.FloorToInt(currentTime));

        if (awardTimeBonusOnFinish)
        {
            if (animateTimeBonusOnFinish)
            {
                yield return AnimateTimeBonus();
            }
            
            {
                AwardTimeBonusInstant();
            }
        }

        yield return new WaitForSeconds(1.5f); // small delay before showing win screen

        HideUI();
        
        WinScreenStats();
        UpdateBestTimeRecord();

        // Save the high score when the level ends
        UpdateHighScore();

        // Save the collected coin names in PlayerPrefs
        SaveCollectedCoins();

        // Update the rank based on the final score
        UpdateRank();

        // Set Level Completed
        PlayerPrefs.SetInt("LevelCompleted_" + levelID, 1);

        // Remove saved progress
        RemoveProgress();

        CheckGameCompletion();
        ResumeGame();

        // Destroy all music objects
        foreach (GameObject musicObj in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            Destroy(musicObj);
        }
        WinScreenGameObject.SetActive(true);
        restartButtonWinScreen.Select();

        CursorHelper.ShowCursor();

        finishTimeSnapshot = -1;
    }

    public void SetPlayer(MarioMovement player, int playerIndex)
    {
        while (players.Count <= playerIndex) players.Add(null);

        bool nullPrevPlayer = players[playerIndex] == null;
        players[playerIndex] = player;

        if (nullPrevPlayer)
        {
            // Defer start-powerup cheats until after checkpoints have placed Mario
            StartCoroutine(ApplyStartPowerupCheatNextFrame(player));
        }
    }

    private IEnumerator ApplyStartPowerupCheatNextFrame(MarioMovement player)
    {
        // wait one frame so Checkpoint.Start(), then AddCheckpoint() can teleport the player
        yield return null;

        if (player == null) yield break; // player could have been swapped/destroyed

        if (GlobalVariables.cheatStartTiny)
            player.ChangePowerup(tinyMarioPrefab);
        else if (GlobalVariables.cheatStartIce)
            player.ChangePowerup(iceMarioPrefab);
        else if (GlobalVariables.cheatFlamethrower)
            player.ChangePowerup(fireMarioPrefab);
    }

    // Gets rid of all the dead players
    private List<MarioMovement> ExistingPlayers()
    {
        List<MarioMovement> existingPlayers = new List<MarioMovement>();
        foreach (MarioMovement player in players)
        {
            if (player != null)
            {
                existingPlayers.Add(player);
            }
        }
        return existingPlayers;
    }

    public MarioMovement GetPlayer(int playerIndex)
    {
        if (playerIndex >= players.Count)
        {
            return null;
        }
        return players[playerIndex];
    }

    public MarioMovement[] GetPlayers()
    {
        return players.ToArray();
    }

    public GameObject[] GetPlayerObjects()
    {
        return ExistingPlayers().Select(player => player.gameObject).ToArray();
    }

    // Checkpoint managing
    public void AddCheckpoint(Checkpoint checkpoint)
    {
        checkpoints.Add(checkpoint);

        if (GlobalVariables.checkpoint == checkpoint.checkpointID)
        {
            SetMarioPosToCheckpoint(checkpoint);
        }
    }

    // Called when the corresponding checkpoint is added to GameManager to place mario at the checkpoint
    private void SetMarioPosToCheckpoint(Checkpoint checkpoint)
    {
        if (!GlobalVariables.enableCheckpoints) return;

        // Set checkpoint active
        RefreshCheckpoints(checkpoint.checkpointID);

        // Get the player object
        GameObject player = GameObject.FindGameObjectWithTag("Player"); // TODO: Replace when we improve player management

        // Set the player's position to the checkpoint's spawn position
        player.transform.position = checkpoint.SpawnPosition;
    }
    
    public void OnCheckpointActivated(Checkpoint activated)
    {
        if (!GlobalVariables.enableCheckpoints) return;

        // Make only the current checkpoint visually active
        RefreshCheckpoints(GlobalVariables.checkpoint);
    }

    private void RefreshCheckpoints(int currentId)
    {
        foreach (var cp in checkpoints)
        {
            if (cp == null) continue;

            if (cp.checkpointID == currentId)
            {
                cp.SetActive(false); // current looks active (no feedback)
            }
            else if (cp.checkpointID < currentId)
            {
                cp.DisableCheckpoint(); // behind: inactive + collider off
            }
            else
            {
                cp.EnableCheckpoint(); // ahead: available again (passive + collider on)
            }
        }
    }
}