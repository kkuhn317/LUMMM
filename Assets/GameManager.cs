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
    public AudioClip timeWarning;
    [SerializeField] TMP_Text timerText;

    [Header("Lives")]
    private int maxLives = 99;
    [SerializeField] TMP_Text livesText;

    [Header("Coin System")]
    public AudioClip coin;
    public AudioClip bigCoin;
    [SerializeField] TMP_Text coinText;

    [Header("Score System")]
    public int scoreCount;
    public AudioClip extraLife;
    [SerializeField] TMP_Text scoreText;

    [Header("Game Over & Lose Life")]
    // Name of the Game Over scene
    public string gameOverSceneName;
    // Name of the Lose Life scene
    public string loseLifeSceneName;

    [Header("Key System")]
    public List<GameObject> keys = new List<GameObject>();

    private AudioSource audioSource;

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
        currentTime = startingTime;
        GlobalVariables.levelscene = SceneManager.GetActiveScene().buildIndex;
        UpdateLivesUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPaused)
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
                // Debug.Log("The time has run out!");
                DecrementLives();
            }
        }
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
        GlobalVariables.lives--;
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

    private void UpdateLivesUI()
    {
        livesText.text = GlobalVariables.lives.ToString("D2"); // 00
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

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }
}
