using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal game manager that serves only as a central entry point and coordinator.
/// All specific game logic has been delegated to specialized systems.
/// </summary>
public class GameManagerRefactored : MonoBehaviour, IGameManager
{
    public static GameManagerRefactored Instance { get; private set; }

    [Header("Core Flow Controllers (Required)")]
    [SerializeField] private LevelFlowController levelFlowController;
    [SerializeField] private PauseMenuController pauseMenuController;

    [Header("Optional: Inspector Visibility Only")]
    [SerializeField] private bool showSystemReferencesInInspector = false;

    // Inspector-only references (debug visibility)
    [SerializeField, HideInInspector] private TimerManager timerManager;
    [SerializeField, HideInInspector] private ScoreSystem scoreSystem;
    [SerializeField, HideInInspector] private CoinSystem coinSystem;
    [SerializeField, HideInInspector] private LifeSystem lifeSystem;
    [SerializeField, HideInInspector] private CheckpointManager checkpointManager;
    [SerializeField, HideInInspector] private RankSystem rankSystem;
    [SerializeField, HideInInspector] private GreenCoinSystem greenCoinSystem;
    [SerializeField, HideInInspector] private PlayerRegistry playerRegistry;
    [SerializeField, HideInInspector] private HUDController hudController;
    [SerializeField, HideInInspector] private WinScreenController winScreenController;
    [SerializeField, HideInInspector] private GameOverController gameOverController;

    public string CurrentLevelId { get; private set; }
    public bool IsGameActive { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Strict validation for required dependencies in the starting scene
        ValidateRequiredReferences();
        CacheSystemReferencesIfEnabled();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= HandleGameOver;
    }

    private void Start()
    {
        BindLevelContextAndInitialize();
        GameEvents.TriggerGameInitialized();
        GameEvents.TriggerLevelStarted();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;

        // When a new scene loads, rebind required refs strictly:
        // The GameManager persists, but scene-level controllers may be new instances.
        RebindSceneReferencesStrict();

        IsGameActive = true;
        BindLevelContextAndInitialize();
    }

    private void BindLevelContextAndInitialize()
    {
        // Ensure a valid level context exists when starting directly in a test level scene
        if (GlobalVariables.levelInfo == null)
            GlobalVariables.levelInfo = TestLevelInfo();

        CurrentLevelId = GlobalVariables.levelInfo?.levelID ?? SceneManager.GetActiveScene().name;

        // Initialize core flow controllers
        levelFlowController.InitializeForLevel(CurrentLevelId);
        pauseMenuController.InitializeForLevel();

        // This will publish level context event (keep in mind that starting time is taken from TimerManager in-scene)
        var timer = FindObjectOfType<TimerManager>();
        float startingTime = timer != null ? timer.StartingTime : 300f;
        GameEvents.TriggerLevelContextChanged(new GameEvents.LevelContext(CurrentLevelId, startingTime));
    }

    // So no error when running starting in the level scene
    private LevelInfo TestLevelInfo()
    {
        LevelInfo info = ScriptableObject.CreateInstance<LevelInfo>();
        info.levelID = "test";
        info.levelScene = SceneManager.GetActiveScene().name;
        info.lives = 3;
        info.videoYear = "Unknown";
        info.videoLink = "Unknown";
        return info;
    }

    private void HandleGameOver()
    {
        IsGameActive = false;
    }

    private void ValidateRequiredReferences()
    {
        if (levelFlowController == null)
            Debug.LogError($"{nameof(GameManagerRefactored)}: {nameof(levelFlowController)} is required (assign it in the Inspector).");

        if (pauseMenuController == null)
            Debug.LogError($"{nameof(GameManagerRefactored)}: {nameof(pauseMenuController)} is required (assign it in the Inspector).");
    }

    private void RebindSceneReferencesStrict()
    {
        // If you prefer DI, replace this with a scene installer that assigns these refs.
        levelFlowController = FindObjectOfType<LevelFlowController>();
        pauseMenuController = FindObjectOfType<PauseMenuController>();

        if (levelFlowController == null)
            Debug.LogError($"{nameof(GameManagerRefactored)}: {nameof(LevelFlowController)} was not found in the loaded scene.");

        if (pauseMenuController == null)
            Debug.LogError($"{nameof(GameManagerRefactored)}: {nameof(PauseMenuController)} was not found in the loaded scene.");

        CacheSystemReferencesIfEnabled();
    }

    private void CacheSystemReferencesIfEnabled()
    {
        if (!showSystemReferencesInInspector) return;

        timerManager = FindObjectOfType<TimerManager>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        coinSystem = FindObjectOfType<CoinSystem>();
        lifeSystem = FindObjectOfType<LifeSystem>();
        checkpointManager = FindObjectOfType<CheckpointManager>();
        rankSystem = FindObjectOfType<RankSystem>();
        greenCoinSystem = FindObjectOfType<GreenCoinSystem>();
        playerRegistry = FindObjectOfType<PlayerRegistry>();
        hudController = FindObjectOfType<HUDController>();
        winScreenController = FindObjectOfType<WinScreenController>();
        gameOverController = FindObjectOfType<GameOverController>();
    }

    #region Public API - Level Flow Control

    // Used for Pause Menu and Game Over screen
    public void RestartLevelFromBeginning()
    {
        Debug.Log("GameManager: Restarting level from beginning");

        GlobalVariables.ResetForLevel();

        // Keep in mind that CheckpointManager must exist in the scene if checkpoints are enabled
        var cp = FindObjectOfType<CheckpointManager>();
        if (cp == null)
            Debug.LogError($"{nameof(GameManagerRefactored)}: {nameof(CheckpointManager)} not found when restarting from beginning.");
        else
            cp.ClearCheckpoint();

        StopAllLevelMusic();
        ReloadScene();
    }

    public void RestartLevelFromCheckpoint()
    {
        Debug.Log("GameManager: Restarting level from checkpoint");

        StopAllLevelMusic();
        ReloadScene();
    }

    public void RestartLevelFromBeginningWithFadeOut()
    {
        Debug.Log("GameManager: Restarting level from beginning (with fade)");

        Time.timeScale = 1f;

        GlobalVariables.ResetForLevel();

        var cp = FindObjectOfType<CheckpointManager>();
        if (cp == null)
            Debug.LogError($"{nameof(GameManagerRefactored)}: {nameof(CheckpointManager)} not found when restarting from beginning.");
        else
            cp.ClearCheckpoint();

        StopAllLevelMusic();
        CursorHelper.ShowCursor();
        
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitLevel()
    {
        Debug.Log("GameManager: Quitting level");

        StopAllLevelMusic();
        CursorHelper.ShowCursor();

        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithFade("SelectLevel");
        else
            SceneManager.LoadScene("SelectLevel");
    }

    #endregion

    #region Public API - Scene Management

    public void ReloadScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithFade(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    #endregion

    #region Private Helpers

    private void StopAllLevelMusic()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ClearMusicOverrides(MusicManager.MusicStartMode.Continue);
        }

        // If you're refactoring hard, consider removing tag-based cleanup and managing music via a single MusicSystem.
        foreach (GameObject musicObj in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            Destroy(musicObj);
        }
    }

    #endregion

    #region System Access (Use sparingly)

    public T GetSystem<T>() where T : Component
    {
        // Service locator pattern: keep only if you intentionally accept this tradeoff.
        // Prefer explicit dependencies instead.
        return FindObjectOfType<T>();
    }

    public bool TryGetSystem<T>(out T system) where T : Component
    {
        system = FindObjectOfType<T>();
        return system != null;
    }

    #endregion
}

public interface IGameManager
{
    string CurrentLevelId { get; }
    bool IsGameActive { get; }

    void RestartLevelFromBeginning();
    void RestartLevelFromCheckpoint();
    void QuitLevel();

    void ReloadScene();
    void LoadScene(string sceneName);

    T GetSystem<T>() where T : Component;
    bool TryGetSystem<T>(out T system) where T : Component;
}