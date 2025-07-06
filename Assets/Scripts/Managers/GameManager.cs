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
    //It allows me to access other scripts
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
    private bool timesRunning = true;
    public static bool isPaused = false;
    private static bool pauseable = true; // turn off when win screen shows up or when fading to another scene
    private bool isTimeUp = false;
    private bool stopTimer = false;
    public AudioClip timeWarning;
    [SerializeField] protected TMP_Text timerText;
    public Image infiniteTimeImage;
    [SerializeField] protected TMP_Text speedrunTimerText;

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
    private Checkpoint[] checkpoints;

    #region GreenCoindata
    private List<GameObject> collectedGreenCoins = new List<GameObject>();  // ALL green coins ever collected
    private List<GameObject> collectedGreenCoinsInRun = new List<GameObject>(); // Green coins collected in the current run

    [Header("Players")]
    private List<MarioMovement> players = new();  // The players will tell the game manager who they are on start or when the player changes

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
        for(int i = 0; i < greenCoins.Length; i++)
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
            if (levelID != PlayerPrefs.GetString("SavedLevel", "none")) {
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

    [Header("Music")]
    public GameObject music;
    private List<GameObject> musicOverrides = new List<GameObject>(); // add to this list when music override added, remove when music override removed
    private GameObject currentlyPlayingMusic;

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
        if (hideCursor) {
            CursorHelper.HideCursor();
        }

        if (isOptionsMenuLevel)
        {
            // Options menu-specific settings
            ApplyOptionsMenuSettings();
        }

        if (music) {
            print("Music found");
            currentlyPlayingMusic = music;
        }
        currentTime = startingTime;

        if (GlobalVariables.levelInfo == null) {
            GlobalVariables.levelInfo = TestLevelInfo();
        }

        levelID = GlobalVariables.levelInfo.levelID;
        Debug.Log("Current level ID: " + levelID);

        if (!isOptionsMenuLevel) {
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
        if (!isOptionsMenuLevel) {
            LoadCollectedCoins(); // Load collected coins data from PlayerPrefs
            UpdateHighScoreUI();
            UpdateLivesUI();
            UpdateCoinsUI();
            UpdateScoreUI();
            InitSpeedrunTimer();
        }
        ToggleCheckpoints();
        SetMarioPosition();
        CheckForInfiniteTime();
    }

    void OnEnable() {
        pauseAction.Enable();
    }

    void OnDisable() {
        pauseAction.Disable();
    }

    // So no error when running starting in the level scene
    LevelInfo TestLevelInfo() {
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
        if (levelNameText != null) {
            levelNameText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Level_" + levelID);
        }

        if (CheckpointIndicator != null) {
            if (GlobalVariables.checkpoint != -1) {
                CheckpointIndicator.SetActive(true);
            } else {
                CheckpointIndicator.SetActive(false);
            }
        }

        // Check for pause input only if the game is not over
        if (!isTimeUp)
        {
            UpdateRank();
            UpdateSpeedrunTimerUI();

            if (pauseAction.WasPressedThisFrame()) {
                if (!isOptionsMenuLevel || GetComponent<OptionsGameManager>().CanTogglePause()) {
                    TogglePauseGame();
                }
            }

            // Check if enablePlushies is true, then activate "Plushie" objects.
            if (GlobalVariables.enablePlushies) {
                ActivatePlushieObjects();
            } else {
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

                    if (currentTime <= 100 && timesRunning)
                    {
                        audioSource.clip = timeWarning;
                        audioSource.PlayOneShot(timeWarning);
                        timesRunning = false;
                    }
                    if (currentTime <= 0 && !isTimeUp)
                    {
                        currentTime = 0;
                        isTimeUp = true;
                        // Debug.Log("Stop music!");
                        StopAllMusic();
                        // Debug.Log("The time has run out!");
                        // DecrementLives();
                        ResumeMusic(music);
                    }
                }
            }
        }
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
        if(GlobalVariables.checkpoint != -1) {
            GoToResetPopUp();
        } else {
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
        RemoveAllMusicOverrides();

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
        RemoveAllMusicOverrides();

        // Reloads the level
        ReloadSceneWithFade();

        // Unpause the game
        ResumeGame();
    }
    
    public void RestartLevelFromCheckpoint()
    {
        // turn off all music overrides
        RemoveAllMusicOverrides();

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

    public void HideUI(){
        if (levelUI != null)
        {
            levelUI.SetActive(false);
        }
    }

    public virtual void DecrementLives()
    {
        // turn off all music overrides
        RemoveAllMusicOverrides();

        if (!GlobalVariables.infiniteLivesMode)        // Check if the player is not in infinite lives mode
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
                if (!FadeInOutScene.Instance.transitioning)
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
            if (!FadeInOutScene.Instance.transitioning)
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
    private void UpdateSpeedrunTimerUI() {
        if (GlobalVariables.SpeedrunMode && speedrunTimerText != null)
        {
            //string timeString = "ERROR!";
            //GlobalVariables.elapsedTime.TryFormat(timeString, 
            speedrunTimerText.text = "<mspace=0.8em>" + GlobalVariables.elapsedTime.ToString(@"m\:ss\.ff");
            // NOTE: the timer will currently appear to reset once it goes past 1 hour
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
        AddScorePoints(4000);

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
            if (uiImage != null) {
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

    public void SetNewMainMusic(GameObject music) {
        // is currentlyPlayingMusic the main music?
        if (currentlyPlayingMusic == this.music) {
            currentlyPlayingMusic = music;
        }
        this.music = music;
    }

    public void OverrideMusic(GameObject musicOverride)
    {
        // turn off music
        if (currentlyPlayingMusic != null)
            currentlyPlayingMusic.GetComponent<AudioSource>().mute = true;

        // add to list
        musicOverrides.Add(musicOverride);
        // set as current music
        currentlyPlayingMusic = musicOverride;
    }

    public void ResumeMusic(GameObject musicOverride)
    {
        // are we in the list? if not then do nothing
        if (!musicOverrides.Contains(musicOverride))
        {
            return;
        }

        // remove from list
        musicOverrides.Remove(musicOverride);

        // are we the current music?
        if (currentlyPlayingMusic == musicOverride)
        {
            // are there any other overrides?
            if (musicOverrides.Count > 0)
            {
                // play the last override
                currentlyPlayingMusic = musicOverrides[musicOverrides.Count - 1];
                currentlyPlayingMusic.GetComponent<AudioSource>().mute = false;
            }
            else
            {
                // play the original music
                if (music) {
                    currentlyPlayingMusic = music;
                    currentlyPlayingMusic.GetComponent<AudioSource>().mute = false;
                } else {
                    currentlyPlayingMusic = null;
                }
            }
        }
    }

    public void RemoveAllMusicOverrides()
    {
        StopAllMusic();
       
        if (currentlyPlayingMusic != null){
            currentlyPlayingMusic.GetComponent<AudioSource>().mute = false;
        }   
    }

    public void RestartMusic()
    {
        currentlyPlayingMusic.GetComponent<AudioSource>().Play();
    }

    public void StopAllMusic()
    {
        // turn off currentlyPlayingMusic (everything else should be muted)
        if (currentlyPlayingMusic != null)
            currentlyPlayingMusic.GetComponent<AudioSource>().mute = true;

        // clear list of overrides
        musicOverrides.Clear();

        if (music) {
            currentlyPlayingMusic = music;
        } else {
            currentlyPlayingMusic = null;
        }
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
        if (!isPaused) {
            PauseGame();
        } else {
            ResumeGame();
        }
    }

    public void ToggleCheckpoints()
    {
        if (!GlobalVariables.enableCheckpoints)
        {
            Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();
            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.DisableCheckpoint();
                Debug.Log("All checkpoints have been disabled");
            }
        }
        else
        {
            checkpoints = FindObjectsOfType<Checkpoint>();
            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.EnableCheckpoint();
                Debug.Log("All checkpoints have been enabled");
            }
        }
    }

    public int GetCheckpointID(Checkpoint checkpoint)
    {
        return Array.IndexOf(checkpoints, checkpoint);
    }

    // Called when scene is loaded to place mario at the checkpoint
    private void SetMarioPosition()
    {
        if (!GlobalVariables.enableCheckpoints) return;
        if (GlobalVariables.checkpoint < 0 || GlobalVariables.checkpoint >= checkpoints.Length) return;
        // Note: currently, if you exit a level after reaching a checkpoint, then go to the rebind menu in the options scene,
        // The saved checkpoint will still be in GlobalVariables.checkpoint, so that's why we need to check if it's less than the length of the array

        // Get the checkpoint object
        Checkpoint checkpoint = checkpoints[GlobalVariables.checkpoint];

        // Set it active
        checkpoint.SetActive();

        // Get the player object
        GameObject player = GameObject.FindGameObjectWithTag("Player"); // TODO: Replace when we improve player management

        // Set the player's position to the checkpoint's spawn position
        player.transform.position = checkpoint.SpawnPosition;
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
        GlobalVariables.enablePlushies = false;
    }
    #endregion

    public void PauseGame()
    {
        if (pausemenu == null) return;
        if (!pauseable) return;

        isPaused = true;
        Time.timeScale = 0f;  // Set time scale to 0 (pause)
        GlobalVariables.speedrunTimer.Stop();   // Stop speedrun timer

        if (currentlyPlayingMusic != null) {
            originalVolume = currentlyPlayingMusic.GetComponent<AudioSource>().volume;
            currentlyPlayingMusic.GetComponent<AudioSource>().volume = originalVolume * 0.25f;
        }

        CursorHelper.ShowCursor();

        foreach (MarioMovement player in players)
        {
            player.DisableInputs();
        }

        if (pausemenu != null) {
            // Activate the pause menu
            pausemenu.SetActive(true);
        }      

        if (!isOptionsMenuLevel) {
            mainPauseMenu.SetActive(true);
            ResetPopUp.SetActive(false);
            optionsPauseMenu.SetActive(false);

            resumeButton.Select();  // Select the resume button by default
        } else {
            // Enable UI
            GetComponent<OptionsGameManager>().OnPause();
        }
        
    }

    public void ResumeGame()
    {
        isPaused = false;
        
        Time.timeScale = 1f; // Set time scale to normal (unpause)

        if (!stopTimer) {
            GlobalVariables.speedrunTimer.Start();  // Resume speedrun timer
        }

        if(currentlyPlayingMusic != null) 
        { 
            currentlyPlayingMusic.GetComponent<AudioSource>().volume = originalVolume;
        }

        if (hideCursor)
        {
            CursorHelper.HideCursor();  
        }

        foreach (MarioMovement player in players)
        {
            player.EnableInputs();
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
        } else {
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
        timerFinishText.text = ((int)currentTime).ToString("D3");

        // Collected coins
        collectedCoinsText.text = GlobalVariables.coinCount.ToString("D2");

        // Removing this for now since you can get extra coins after dying
        // if (GlobalVariables.coinCount == totalCoins) { // If the amount of coins collected match the total coins on the level
        //     totalCoinsText.color = Color.yellow; // The total coins text will change to yellow
        // }

        // Score amount achieved
        scoreWinScreenText.text = GlobalVariables.score.ToString("D9");

        if (GlobalVariables.score > highScore) { // If the GlobalVariables.score is higher than highScore  on the level
            NewHighScoreText.SetActive(true); // A text saying "New HighScore!" will appear
        } else {
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
        if (GlobalVariables.SpeedrunMode) {
            speedrunTimeFinishText.text = GlobalVariables.elapsedTime.ToString(@"m\:ss\.ff");
        }

        // Save the highest rank to PlayerPrefs if the current rank is higher than the saved rank
        if (currentRank > highestRank) {
            highestRank = currentRank;
            SaveHighestRank(currentRank);

            if (highestRank != PlayerRank.Default) // You got a rank that isn't the question mark?
            NewBestRankText.SetActive(true); // A text saying "New Best!" will appear
        } else {
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
                if (player != null){
                    Destroy(player.gameObject);
                }   
            }
        }
        print("DestroyPlayersImmediately complete");

        if (stopMusicImmediately)
        {
            StopAllMusic();
        }
        if (cutsceneDelay > 0) {
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
            StopAllMusic();
        }
        
        if (hideUI)
        {
            HideUI();
            print("UI hidden");
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
        pauseable = false;

        WinScreenStats();

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
    }

    public void SetPlayer(MarioMovement player, int playerIndex)
    {
        //print("Setting player " + playerIndex + " to " + player.name);
        while (players.Count <= playerIndex)
        {
            players.Add(null);
        }
        players[playerIndex] = player;
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
}