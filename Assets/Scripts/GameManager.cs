using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    //It allows me to access other scripts
    public static GameManager Instance { get; private set; }

    [Header("Timer")]
    float currentTime;
    public float startingTime;
    private bool timesRunning = true;
    private bool isPaused = false;
    private bool isTimeUp = false;
    private bool stopTimer = false;
    public AudioClip timeWarning;
    [SerializeField] TMP_Text timerText;

    [Header("Lives")]
    private int maxLives = 99;
    [SerializeField] TMP_Text livesText;

    [Header("Coin System")]
    public AudioClip coin;
    public AudioClip bigCoin;
    [SerializeField] TMP_Text coinText;

    [Header("High Score")]
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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        audioSource = GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (music)
            currentlyPlayingMusic = music;
        currentTime = startingTime;
        GlobalVariables.levelscene = SceneManager.GetActiveScene().buildIndex;

        // Load the high score from PlayerPrefs, defaulting to 0 if it doesn't exist.
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        ToggleCheckpoints();

        UpdateHighScoreUI();
        UpdateLivesUI();  
    }

    // Update is called once per frame
    void Update()
    {
        // Check for pause input only if the game is not over
        if (!isTimeUp)
        {
            // Toggle pause when the Esc key is pressed
            if (Input.GetButtonDown("Pause"))
            {
                TogglePauseGame();
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
                    DecrementLives();
                    ResumeMusic(music);
                }
            }
        }
    }

    public void ReloadScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void AddLives()
    {
        audioSource.clip = extraLife;
        audioSource.PlayOneShot(extraLife);
        GlobalVariables.lives++;
        UpdateLivesUI();

        // Start the color change coroutine
        StartCoroutine(AnimateTextColor(livesText, Color.green, 0.5f));

        // Save the current number of lives to PlayerPrefs
        PlayerPrefs.SetInt("GlobalVariables.lives", GlobalVariables.lives);
    }

    private void UpdateHighScore()
    {
        if (scoreCount > highScore)
        {
            highScore = scoreCount;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
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

    public void AddScorePoints(int pointsToAdd)
    {
        scoreCount += pointsToAdd;
        UpdateScoreUI();
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

        if (isPaused)
        {
            PauseGame();
        }
        else
        {
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
                checkpoint.DeactivateCheckpoint();
                Debug.Log("All checkpoints have been disabled");
            }
        }
        else
        {
            Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();
            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.ActivateCheckpoint();
                Debug.Log("All checkpoints have been enabled");
            }
        }
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
        SceneManager.LoadScene("SelectLevel");
    }

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

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;  // Set time scale to 0 (pause)

        // Activate the pause menu
        if (pausemenu != null)
            pausemenu.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Set time scale to normal (unpause)

        // Deactivate the pause menu
        if (pausemenu != null)
            pausemenu.SetActive(false);
    }

    // after level ends, call this (ex: flag cutscene ends)
    public void FinishLevel()
    {
        // Save the high score when the level ends
        UpdateHighScore(); 
        ResumeGame();

        // Destroy all music objects
        foreach (GameObject musicObj in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            Destroy(musicObj);
        }
        // This will probably cause a special ending screen to show up, 
        // but for now just go to the main menu
        SceneManager.LoadScene("SelectLevel");
    }
}
