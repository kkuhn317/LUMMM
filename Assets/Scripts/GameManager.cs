using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using static LeanTween;
using System.Linq;

public class GameManager : MonoBehaviour
{
    //It allows me to access other scripts
    public static GameManager Instance { get; private set; }
    private int currentLevelIndex;
    private FadeInOutScene fadeInOutScene;

    [HideInInspector]
    public float currentTime;

    [Header("Timer")]
    public float startingTime;
    private bool timesRunning = true;
    public static bool isPaused = false;
    private bool isTimeUp = false;
    private bool stopTimer = false;
    public AudioClip timeWarning;
    [SerializeField] TMP_Text timerText;

    [Header("Lives")]
    private int maxLives = 99;
    [SerializeField] TMP_Text livesText;
    public Color targetColor = Color.green;

    [Header("Coin System")]
    public AudioClip coin;
    public AudioClip bigCoin;
    [SerializeField] TMP_Text coinText;

    [Header("Green coins")]
    public GameObject[] greenCoins; // Array of green coin GameObjects in the scene
    public List<Image> greenCoinUIImages; // List of UI Image components representing green coins
    public Sprite collectedSprite; // Sprite for the collected state

    #region GreenCoindata
    private List<GameObject> collectedGreenCoins = new List<GameObject>();

    [System.Serializable]
    public class CollectedCoinsData
    {
        public List<string> collectedCoinNames = new List<string>();
    }

    [Header("Checkpoints")]

    private Checkpoint[] checkpoints;

    void SaveCollectedCoins()
    {
        CollectedCoinsData data = new CollectedCoinsData();
        foreach (GameObject coin in collectedGreenCoins)
        {
            data.collectedCoinNames.Add(coin.name);
        }

        // Serialize and save the data to PlayerPrefs using the level index
        string jsonData = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("CollectedCoinsData_" + currentLevelIndex, jsonData);
        PlayerPrefs.Save();
    }

    void LoadCollectedCoins()
    {
        if (PlayerPrefs.HasKey("CollectedCoinsData_" + currentLevelIndex))
        {
            // Retrieve and deserialize the data from PlayerPrefs
            string jsonData = PlayerPrefs.GetString("CollectedCoinsData_" + currentLevelIndex);
            CollectedCoinsData data = JsonUtility.FromJson<CollectedCoinsData>(jsonData);

            // Update UI and collectedGreenCoins list
            foreach (string coinName in data.collectedCoinNames)
            {
                GameObject coinObject = Array.Find(greenCoins, coin => coin.name == coinName);
                if (coinObject != null)
                {
                    collectedGreenCoins.Add(coinObject);

                    // Change the alpha of the sprite renderer to indicate it's collected
                    SpriteRenderer coinRenderer = coinObject.GetComponent<SpriteRenderer>();
                    Color coinColor = coinRenderer.color;
                    coinColor.a = 0.5f;
                    coinRenderer.color = coinColor;

                    // Update UI for the collected coin
                    Image uiImage = greenCoinUIImages[Array.IndexOf(greenCoins, coinObject)];
                    uiImage.sprite = collectedSprite;
                }
            }
        }
    }
    #endregion

    [Header("High Score System")]
    public int highScore;
    [SerializeField] TMP_Text highScoreText;

    [Header("Score System")]
    public int scoreCount;
    public AudioClip extraLife;
    [SerializeField] TMP_Text scoreText;

    [Header("Music")]
    public GameObject music;
    private List<GameObject> musicOverrides = new List<GameObject>(); // add to this list when music override added, remove when music override removed
    private GameObject currentlyPlayingMusic;

    [Header("Pause Menu")]
    public GameObject pausemenu;
    public GameObject mainPauseMenu;
    public GameObject optionsPauseMenu;

    [Header("Rank")]
    public RawImage currentRankImage;
    public RawImage highestRankImage;
    public Sprite questionsprite;
    public Sprite[] rankTypes;

    [Header("Rank conditions")]
    public int scoreForSRank = 10000;
    public int scoreForARank = 9000;
    public int scoreForBRank = 7000;
    public int scoreForCRank = 5000;

    public bool considerAllEnemiesKilled = true;
    private PlayerRank highestRank;
    private PlayerRank currentRank;

    [Header("Rank Change")]
    public float animationDuration = 1.0f;
    public float animationDelay = 0.0f;

    #region RankSystem
    public enum PlayerRank
    {
        Default,
        D,
        C,
        B,
        A,
        S
    }

    private bool AllEnemiesKilled()
    {
        // Check if there are any objects with the tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        return enemies.Length == 0; // True if there are no enemies, false otherwise
    }

    private void UpdateRank()
    {
        PlayerRank currentRank;
        bool allEnemiesKilledRequirementMet = !considerAllEnemiesKilled || AllEnemiesKilled();

        if (scoreCount >= scoreForSRank && allEnemiesKilledRequirementMet)
        {
            currentRankImage.texture = rankTypes[4].texture; // S Rank
            currentRank = PlayerRank.S;
        }
        else if (scoreCount >= scoreForARank && allEnemiesKilledRequirementMet)
        {
            currentRankImage.texture = rankTypes[3].texture; // A Rank
            currentRank = PlayerRank.A;
        }
        else if (scoreCount >= scoreForBRank)
        {
            currentRankImage.texture = rankTypes[2].texture; // B Rank
            currentRank = PlayerRank.B;
        }
        else if (scoreCount >= scoreForCRank)
        {
            currentRankImage.texture = rankTypes[1].texture; // C Rank
            currentRank = PlayerRank.C;
        }
        else
        {
            if (highestRank == PlayerRank.Default)
            {
                currentRankImage.texture = questionsprite.texture; // Default
            }
            else
            {
                currentRankImage.texture = rankTypes[0].texture; // D Rank
            }
            currentRank = PlayerRank.D;
        }

        // Check if the current rank is higher than the stored highest rank
        if (currentRank > highestRank)
        {
            highestRank = currentRank;

            // Save the highest rank to PlayerPrefs
            SaveHighestRank(highestRank);

            SetCurrentRank(currentRank);
        }
    }

    private void SetCurrentRank(PlayerRank newRank)
    {
        currentRank = newRank;

        // Trigger the scale animation
        PlayScaleAnimation(currentRankImage.gameObject);
    }

    private void SaveHighestRank(PlayerRank rank)
    {
        // Save the highest rank to PlayerPrefs
        PlayerPrefs.SetInt("HighestPlayerRank_" + currentLevelIndex, (int)rank);
        PlayerPrefs.Save();
    }

    private PlayerRank LoadHighestRank()
    {
        // Load the highest rank from PlayerPrefs, defaulting to "Default" if it doesn't exist.
        return (PlayerRank)PlayerPrefs.GetInt("HighestPlayerRank_" + currentLevelIndex, (int)PlayerRank.Default);
    }

    private void ResetCurrentRank()
    {
        currentRank = PlayerRank.Default;
        currentRankImage.texture = questionsprite.texture; // Set currentRankImage to the default texture
    }

    private void PlayScaleAnimation(GameObject targetObject)
    {
        // Example: Scale from 0.25 to 0.35 and back over 0.25 second
        LeanTween.scale(targetObject, new Vector3(0.35f, 0.35f, 0.35f), 0.25f)
            .setEase(LeanTweenType.easeInOutQuad)
            .setLoopPingPong(1);  // Play the animation once forward and once backward
    }
    #endregion

    [Header("Game Over & Lose Life")]
    // Name of the Game Over scene
    public string gameOverSceneName;
    // Name of the Lose Life scene
    public string loseLifeSceneName;

    [Header("Key System")]
    public List<GameObject> keys = new List<GameObject>();

    private AudioSource audioSource;
    // List to keep track of all PauseableObject scripts.
    private List<PauseableObject> pauseableObjects = new List<PauseableObject>();
    private float originalVolume;

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

        // Store the original audio volume
        originalVolume = AudioListener.volume;
    }

    // Start is called before the first frame update
    void Start()
    {
        fadeInOutScene = FindObjectOfType<FadeInOutScene>();

        if (music) {
            print("Music found");
            currentlyPlayingMusic = music;
        }
        currentTime = startingTime;
        GlobalVariables.levelscene = SceneManager.GetActiveScene().buildIndex;
        currentLevelIndex = GlobalVariables.levelscene;
        Debug.Log("Current level ID: " + currentLevelIndex);

        // Load the high score from PlayerPrefs, defaulting to 0 if it doesn't exist.
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        // Load the highest rank from PlayerPrefs
        highestRank = LoadHighestRank();

        // Set the texture for highestRankImage based on the loaded highest rank
        if (highestRank != PlayerRank.Default)
            highestRankImage.texture = rankTypes[(int)highestRank - 1].texture;

        ResetCurrentRank();

        LoadCollectedCoins(); // Load collected coins data from PlayerPrefs
        ToggleCheckpoints();
        SetMarioPosition();
        UpdateHighScoreUI();
        UpdateLivesUI();
        UpdateCoinsUI();
    }

    // Update is called once per frame
    void Update()
    {
        // Check for pause input only if the game is not over
        if (!isTimeUp)
        {
            UpdateRank();

            // Toggle pause when the Esc key is pressed
            if (Input.GetButtonDown("Pause")) {
                TogglePauseGame();
            }

            // Check if enablePlushies is true, then activate "Plushie" objects.
            if (GlobalVariables.enablePlushies) {
                ActivatePlushieObjects();
            } else {
                DeactivatePlushieObjects();
            }

            if (!isPaused && !stopTimer)
            {
                // Timer 
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

    public void RestartLevelFromBeginning()
    {
        // reset lives, checkpoint, etc
        GlobalVariables.ResetForLevel(GlobalVariables.startLives);

        // turn off all music overrides
        RemoveAllMusicOverrides();

        ReloadScene();
    }
    
    public void RestartLevelFromCheckpoint()
    {
        // turn off all music overrides
        RemoveAllMusicOverrides();

        ReloadScene();
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(currentLevelIndex);
    }

    public void DecrementLives()
    {
        // turn off all music overrides
        RemoveAllMusicOverrides();

        // Check if the player is not in infinite lives mode
        if (!GlobalVariables.infiniteLivesMode)
        {
            GlobalVariables.lives--;

            // Check if the player has run out of lives
            if (GlobalVariables.lives <= 0)
            {
                // Load the Game Over scene
                SceneManager.LoadScene(gameOverSceneName);
                GlobalVariables.lives = 3;
            }
            else
            {
                // Load the LoseLife scene and restart the current level
                PlayerPrefs.SetInt("GlobalVariables.lives", GlobalVariables.lives);
                SceneManager.LoadScene(loseLifeSceneName);
            }
        }
        else
        {
            // Reload the current scene when the player dies in infinite lives mode
            ReloadScene();
        }
    }

    public void AddLives()
    {
        audioSource.clip = extraLife;
        audioSource.PlayOneShot(extraLife);
        GlobalVariables.lives++;
        UpdateLivesUI();

        // Start the color change coroutine
        StartCoroutine(AnimateTextColor(livesText, targetColor, 0.5f));

        // Save the current number of lives to PlayerPrefs
        PlayerPrefs.SetInt("GlobalVariables.lives", GlobalVariables.lives);
    }

    IEnumerator AnimateTextColor(TMP_Text text, Color targetColor, float duration)
    {
        Color initialColor = text.color;

        // Fade the text color to the target color over the specified duration
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            text.color = Color.Lerp(initialColor, targetColor, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Wait for a short period
        yield return new WaitForSeconds(0.1f);

        // Fade the text color back to the original color over the specified duration
        timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            text.color = Color.Lerp(targetColor, initialColor, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Set the text color back to the original color
        text.color = initialColor;
    }

    
    #region updateUI
    private void UpdateHighScore()
    {
        if (scoreCount > highScore)
        {
            highScore = scoreCount;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    private void UpdateLivesUI()
    {
        if (GlobalVariables.infiniteLivesMode)
        {
            livesText.text = "INF!";
        }
        else
        {
            livesText.text = GlobalVariables.lives.ToString("D2"); // 00
        }
    }

    private void UpdateCoinsUI()
    {
        coinText.text = GlobalVariables.coinCount.ToString("D2"); // 00
    }
    private void UpdateTimerUI()
    {
        timerText.text = ((int)currentTime).ToString("D3"); // 000
    }

    private void UpdateScoreUI()
    {
        scoreText.text = scoreCount.ToString("D9"); // 000000000

    }

    private void UpdateHighScoreUI()
    {
        highScoreText.text = highScore.ToString("D9"); // 000000000 | Display the high score
    }
    #endregion

    public void AddCoin(int coinValue)
    {
        AudioClip coinSound;
        switch (coinValue)
        {
            case 1:
                coinSound = coin;
                break;
            case 10:
                coinSound = bigCoin;
                break;
            case 30:
                coinSound = bigCoin;
                break;
            case 50:
                coinSound = bigCoin;
                break;
            default:
                coinSound = coin;
                break;
        }

        audioSource.clip = coinSound;
        audioSource.PlayOneShot(coinSound);
        GlobalVariables.coinCount += coinValue;
        scoreCount += coinValue * 100;

        if (GlobalVariables.coinCount > 99)
        {
            GlobalVariables.coinCount -= 100;
            AddLives();
        }

        UpdateCoinsUI();
        UpdateScoreUI();
    }

    public void CollectGreenCoin(GameObject greenCoin)
    {
        AddScorePoints(4000);
        audioSource.PlayOneShot(coin);

        // Check if the green coin is uncollected
        if (!collectedGreenCoins.Contains(greenCoin))
        {         
            Debug.Log("Collecting green coin: " + greenCoin.name);

            Image uiImage = greenCoinUIImages[Array.IndexOf(greenCoins, greenCoin)];
            collectedGreenCoins.Add(greenCoin);
            uiImage.sprite = collectedSprite;

            // Change the alpha of the sprite renderer to indicate it's collected
            SpriteRenderer coinRenderer = greenCoin.GetComponent<SpriteRenderer>();
            Color coinColor = coinRenderer.color;
            coinColor.a = 0.5f;
            coinRenderer.color = coinColor;
        }
    }

    void CheckGameCompletion()
    {
        if (collectedGreenCoins.Count == greenCoins.Length)
        {
            Debug.Log("All green coins collected");
        }

        if (GlobalVariables.infiniteLivesMode && GlobalVariables.enableCheckpoints)
        {
            Debug.Log("You complete the level without advantages, Congrats! You did it! Yay :D!");
        }

        if (collectedGreenCoins.Count == greenCoins.Length && GlobalVariables.infiniteLivesMode && GlobalVariables.enableCheckpoints)
        {
            Debug.Log("Level completed perfect");
        }
    }

    public void AddScorePoints(int pointsToAdd)
    {
        scoreCount += pointsToAdd;
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
        currentlyPlayingMusic.GetComponent<AudioSource>().mute = false;
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
    public void StopTimer()
    {
        stopTimer = true;
    }

    // Function to toggle the game between paused and resumed states.
    public void TogglePauseGame()
    {
        isPaused = !isPaused;

        if (isPaused) {
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
        if (GlobalVariables.checkpoint == -1) return;

        // Get the checkpoint object
        Checkpoint checkpoint = checkpoints[GlobalVariables.checkpoint];

        // Set it active
        checkpoint.SetActive();

        // Get the player object
        GameObject player = GameObject.FindGameObjectWithTag("Player"); // TODO: Replace when we improve player management

        // Set the player's position to the checkpoint's spawn position
        player.transform.position = checkpoint.SpawnPosition;
    }

    // Quit Level
    public void QuitLevel()
    {
        // Destroy all music objects
        foreach (GameObject musicObj in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            Destroy(musicObj);
        }
        ResumeGame();
        fadeInOutScene.LoadSceneWithFade("SelectLevel");
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
        isPaused = true;

        // Lower the audio volume when the game is paused
        AudioListener.volume = originalVolume * 0.25f;
        Time.timeScale = 0f;  // Set time scale to 0 (pause)

        // Activate the pause menu
        if (pausemenu != null)
            pausemenu.SetActive(true);
            mainPauseMenu.SetActive(true);
            optionsPauseMenu.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;

        // Raise the audio volume back to its original level
        AudioListener.volume = originalVolume;
        Time.timeScale = 1f; // Set time scale to normal (unpause)

        // Deactivate the pause menu
        if (pausemenu != null)
            pausemenu.SetActive(false);
            mainPauseMenu.SetActive(true);
            optionsPauseMenu.SetActive(false);
    }

    // after level ends, call this (ex: flag cutscene ends)
    public void FinishLevel()
    {
        // Save the high score when the level ends
        UpdateHighScore();
        // Save the collected coin names in PlayerPrefs
        SaveCollectedCoins();

        // Update the rank based on the final score
        UpdateRank();
        // Set the texture for highestRankImage based on the updated highest rank
        highestRankImage.texture = rankTypes[(int)highestRank - 1].texture;

        CheckGameCompletion();
        ResumeGame();

        // Destroy all music objects
        foreach (GameObject musicObj in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            Destroy(musicObj);
        }
        // This will probably cause a special ending screen to show up, 
        // but for now just go to the select level menu
        fadeInOutScene.LoadSceneWithFade("SelectLevel");
    }
}