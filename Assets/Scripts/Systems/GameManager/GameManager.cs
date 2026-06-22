using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal game manager that serves only as a central entry point and coordinator.
/// All specific game logic has been delegated to specialized systems.
/// </summary>
public class GameManager : MonoBehaviour, IGameManager
{
    public static GameManager Instance { get; private set; }

    [Header("Core Flow Controllers (Required)")]
    [SerializeField] private LevelFlowController levelFlowController;
    [SerializeField] private PauseMenuController pauseMenuController;

    [Header("Optional: Inspector Visibility Only")]
    [SerializeField] private bool showSystemReferencesInInspector = false;

    // Inspector-only references
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
        if (pauseMenuController?.Mode != PauseMenuController.PauseMenuMode.StandaloneOptionsMenu)
            CursorHelper.HideCursor();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;

        RebindSceneReferencesStrict();

        IsGameActive = true;
        BindLevelContextAndInitialize();
    }

    private void BindLevelContextAndInitialize()
    {
        if (GlobalVariables.levelInfo == null)
            GlobalVariables.levelInfo = TestLevelInfo();

        CurrentLevelId = GlobalVariables.levelInfo?.levelID ?? SceneManager.GetActiveScene().name;

        levelFlowController.InitializeForLevel(CurrentLevelId);
        pauseMenuController.InitializeForLevel();

        var timer = FindObjectOfType<TimerManager>();
        float startingTime = timer != null ? timer.StartingTime : 300f;
        GameEvents.TriggerLevelContextChanged(new GameEvents.LevelContext(CurrentLevelId, startingTime));
    }

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
            Debug.LogError($"{nameof(GameManager)}: {nameof(levelFlowController)} is required (assign it in the Inspector).");

        if (pauseMenuController == null)
            Debug.LogError($"{nameof(GameManager)}: {nameof(pauseMenuController)} is required (assign it in the Inspector).");
    }

    private void RebindSceneReferencesStrict()
    {
        levelFlowController = FindObjectOfType<LevelFlowController>();
        pauseMenuController = FindObjectOfType<PauseMenuController>();

        if (levelFlowController == null)
            Debug.LogError($"{nameof(GameManager)}: {nameof(LevelFlowController)} was not found in the loaded scene.");

        if (pauseMenuController == null)
            Debug.LogError($"{nameof(GameManager)}: {nameof(PauseMenuController)} was not found in the loaded scene.");

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

    public void RestartLevelFromBeginning()
    {
        Debug.Log("GameManager: Restarting level from beginning");
        RestartLevelInternal(fromCheckpoint: false);
    }

    public void RestartLevelFromCheckpoint()
    {
        Debug.Log("GameManager: Restarting level from checkpoint");
        RestartLevelInternal(fromCheckpoint: true);
    }

    public void QuitLevel()
    {
        Debug.Log("GameManager: Quitting level");
        
        pauseMenuController.PreparePausedSceneTransition(true);
        GlobalEventHandler.TriggerExitRequested();

        StopAllLevelMusic();
        CursorHelper.ShowCursor();

        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithScreenFade("SelectLevel");
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
            FadeInOutScene.Instance.LoadSceneWithScreenFade(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    #endregion

    #region Private Helpers

    private void RestartLevelInternal(bool fromCheckpoint)
    {
        pauseMenuController.PreparePausedSceneTransition(true);
        timerManager?.StopAllTimers();

        if (!fromCheckpoint)
        {
            GlobalVariables.ResetForLevel();

            var cp = FindObjectOfType<CheckpointManager>();
            if (cp == null)
                Debug.LogError($"{nameof(GameManager)}: {nameof(CheckpointManager)} not found when restarting from beginning.");
            else
                cp.ClearCheckpoint();
        } else
        {
            // Save speedrun time so it doesnt go backward when you load from checkpoint
            checkpointManager.SaveCheckpointBeforeManualRestart();
        }

        StopAllLevelMusic();
        CursorHelper.ShowCursor();

        string sceneName = SceneManager.GetActiveScene().name;

        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.RestartSceneWithFadeToBlack(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    private void StopAllLevelMusic()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.ClearAllOverrides(MusicManager.MusicStartMode.Continue);

        foreach (GameObject musicObj in GameObject.FindGameObjectsWithTag("GameMusic"))
            Destroy(musicObj);
    }

    #endregion

    #region System Access (Use sparingly)

    public T GetSystem<T>() where T : Component
    {
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