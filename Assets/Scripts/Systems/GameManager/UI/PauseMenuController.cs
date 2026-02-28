using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public interface IOptionsPauseHandler
{
    void OnPause();
    void OnResume();
}

public interface IPauseToggleGate
{
    bool CanTogglePause { get; }
}

public interface IPausableGameplay
{
    void SetPaused(bool paused);
}

public class PauseMenuController : MonoBehaviour
{
    public enum PauseMenuMode
    {
        InGamePauseMenu,
        StandaloneOptionsMenu
    }

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Button References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Reset Confirm")]
    [SerializeField] private string resetConfirmMenuId = "ResetConfirmMenu";

    [Header("Pause Settings")]
    [SerializeField] private float pauseMusicVolumeMultiplier = 0.25f;
    [SerializeField] private bool canPause = true;

    [Header("UI Blocking While Paused")]
    [SerializeField] private CanvasGroup[] blockWhilePaused;

    [Header("Shared Player Actions (Optional)")]
    [SerializeField] private InputActionAsset sharedPlayerActionsAsset;

    [Header("Mode")]
    [SerializeField] private PauseMenuMode mode = PauseMenuMode.InGamePauseMenu;

    [Header("External Pause Handler (Optional)")]
    [SerializeField] private MonoBehaviour optionsPauseHandlerBehaviour;

    [Header("GUIManager Integration")]
    [SerializeField] private GUIManager guiManager;

    [Header("Cancel Routing (Optional)")]
    [SerializeField] private UICancelRouter cancelRouter;

    [Tooltip("MenuId used by GUIManager for the pause menu root.")]
    [SerializeField] private string pauseMenuId;

    [Tooltip("If true, Pause will open pause root via GUIManager.")]
    [SerializeField] private bool useGUIManagerForNavigation = true;

    [Header("Action Map Switching")]
    [SerializeField] private string gameplayActionMap = "Mariomove";
    [SerializeField] private string uiActionMap = "UI";

    [Header("Standalone Options Owner (Optional)")]
    [SerializeField] private PlayerInput standaloneOwner;

    private IOptionsPauseHandler optionsPauseHandler;
    private IPauseToggleGate pauseToggleGate;

    private bool isPaused = false;
    private float originalMusicVolume = 1f;
    private float previousTimeScale = 1f;
    private int toggleGuardFrame = -1;
    private PlayerInput pauseOwner;

    public PlayerInput PauseOwner => pauseOwner;
    public bool IsPaused => isPaused;
    public PauseMenuMode Mode => mode;

    private int cancelConsumedFrame = -1;

    // FIX: Cambiado el nombre para coincidir con lo que UICancelRouter llama
    public void NotifyCancelConsumed() => cancelConsumedFrame = Time.frameCount;
    public void MarkCancelConsumedThisFrame() => cancelConsumedFrame = Time.frameCount;
    
    public bool WasCancelConsumedThisFrame() => cancelConsumedFrame == Time.frameCount;

    private void Awake()
    {
        if (MusicManager.Instance != null)
            originalMusicVolume = MusicManager.Instance.GetCurrentVolume();

        ResolveExternalHandler();

        if (mode != PauseMenuMode.StandaloneOptionsMenu)
            CursorHelper.HideCursor();
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += OnGameOver;
        GlobalEventHandler.OnExitRequested += HandleExitRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= OnGameOver;
        GlobalEventHandler.OnExitRequested -= HandleExitRequested;
    }

    private void Start()
    {
        if (guiManager == null) guiManager = FindObjectOfType<GUIManager>(true);
        if (cancelRouter == null) cancelRouter = FindObjectOfType<UICancelRouter>(true);

        if (pauseMenu != null && mode == PauseMenuMode.InGamePauseMenu)
            pauseMenu.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartPressed);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitLevel);
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            RestoreMusicVolume();
            Time.timeScale = previousTimeScale;
        }
    }

    public bool IsAtPauseRoot()
    {
        if (guiManager == null) return false;
        var top = guiManager.GetTopMenuObject();
        string topName = guiManager.GetMenuName(top);
        return string.Equals(topName, pauseMenuId);
    }

    public bool IsMenuOpen(string menuId)
    {
        if (guiManager == null || string.IsNullOrEmpty(menuId)) return false;
        var top = guiManager.GetTopMenuObject();
        return string.Equals(guiManager.GetMenuName(top), menuId);
    }

    public void InitializeForLevel()
    {
        ResolveExternalHandler();
        if (isPaused) ResumeGame();

        if (MusicManager.Instance != null)
            originalMusicVolume = MusicManager.Instance.GetCurrentVolume();

        canPause = true;
        isPaused = false;
        previousTimeScale = 1f;
        Time.timeScale = 1f;
        pauseOwner = null;

        if (mode == PauseMenuMode.StandaloneOptionsMenu)
        {
            if (pauseMenu != null) pauseMenu.SetActive(true);
            PauseGameInternal(owner: GetStandaloneOwner());
            if (useGUIManagerForNavigation && guiManager != null)
            {
                if (!IsAtPauseRoot())
                    guiManager.OpenMenu(pauseMenuId, false);
            }
        }
        else
        {
            if (pauseMenu != null) pauseMenu.SetActive(false);
        }
    }

    private void ResolveExternalHandler()
    {
        optionsPauseHandler = optionsPauseHandlerBehaviour as IOptionsPauseHandler;
        pauseToggleGate     = optionsPauseHandlerBehaviour as IPauseToggleGate;
    }

    public void RequestTogglePause(PlayerInput requester)
    {
        if (!CanTogglePauseNow() || !canPause || toggleGuardFrame == Time.frameCount) return;
        toggleGuardFrame = Time.frameCount;

        if (!isPaused)
        {
            pauseOwner = requester != null ? requester : FindObjectOfType<PlayerInput>(true);
            PauseGameInternal(pauseOwner);

            if (mode == PauseMenuMode.InGamePauseMenu && useGUIManagerForNavigation && guiManager != null)
                guiManager.OpenMenu(pauseMenuId, hidePrevious: false);
            return;
        }

        if (pauseOwner != null && requester != null && requester != pauseOwner) return;
        ResumeGame();
    }

    private bool CanTogglePauseNow()
    {
        if (pauseToggleGate != null && !pauseToggleGate.CanTogglePause) return false;
        var behaviours = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in behaviours)
        {
            if (mb is IPauseToggleGate gate && mb.isActiveAndEnabled && !gate.CanTogglePause) return false;
        }
        return true;
    }

    private void PauseGameInternal(PlayerInput owner)
    {
        if (!canPause || isPaused) return;
        
        isPaused = true;
        pauseOwner = owner ?? pauseOwner ?? FindObjectOfType<PlayerInput>(true);
        
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        ReduceMusicVolume();
        CursorHelper.ShowCursor();

        if (blockWhilePaused != null)
        {
            foreach (var cg in blockWhilePaused)
            {
                if (cg == null) continue;
                cg.interactable   = false;
                cg.blocksRaycasts = true;
            }
        }

        if (pauseMenu != null) pauseMenu.SetActive(true);
        
        if (cancelRouter != null) cancelRouter.SetInputSource(pauseOwner);
        if (guiManager != null) guiManager.SetOwner(pauseOwner);

        optionsPauseHandler?.OnPause();
        SetGameplayControllersPaused(true);
        SwitchOwnerToUIMap(pauseOwner);

        if (useGUIManagerForNavigation && guiManager != null)
        {
            if (!IsAtPauseRoot())
                guiManager.OpenMenu(pauseMenuId, hidePrevious: false);
        }

        GameEvents.TriggerGamePaused();
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = previousTimeScale;
        RestoreMusicVolume();

        if (blockWhilePaused != null)
        {
            foreach (var cg in blockWhilePaused)
            {
                if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }
            }
        }

        optionsPauseHandler?.OnResume();

        if (mode == PauseMenuMode.InGamePauseMenu && useGUIManagerForNavigation && guiManager != null)
        {
            guiManager.CloseAllMenus();
        }

        pauseMenu.SetActive(false);

        SetGameplayControllersPaused(false);
        CursorHelper.HideCursor();
        
        var freshInput = ResolveFreshPlayerInput(pauseOwner);
        SwitchOwnerToGameplayMap(freshInput);

        if (cancelRouter != null) cancelRouter.SetInputSource(null);
        if (guiManager != null) guiManager.SetOwner(null);

        pauseOwner = null;
        GameEvents.TriggerGameResumed();
    }

    private void OnGameOver() { if (isPaused) ResumeGame(); canPause = false; }
    private void HandleExitRequested() { if (isPaused) ResumeGame(); }

    private void OnRestartPressed()
    {
        if (HasCheckpointSaved() && useGUIManagerForNavigation && guiManager != null && !string.IsNullOrEmpty(resetConfirmMenuId))
            guiManager.OpenMenu(resetConfirmMenuId, hidePrevious: false);
        else
            RestartFromBeginning();
    }

    private bool HasCheckpointSaved() => FindObjectOfType<CheckpointManager>(true)?.HasCheckpoint ?? false;

    public void RestartFromBeginning() { ResumeGame(); GameManager.Instance?.RestartLevelFromBeginning(); }
    public void RestartFromCheckpoint() { ResumeGame(); GameManager.Instance?.RestartLevelFromCheckpoint(); }
    public void QuitLevel() { ResumeGame(); GlobalEventHandler.TriggerExitRequested(); GameManager.Instance?.QuitLevel(); }

    private void ReduceMusicVolume()
    {
        if (MusicManager.Instance != null)
        {
            originalMusicVolume = MusicManager.Instance.GetCurrentVolume();
            MusicManager.Instance.SetCurrentVolume(originalMusicVolume * pauseMusicVolumeMultiplier);
        }
    }

    private void RestoreMusicVolume() { MusicManager.Instance?.SetCurrentVolume(originalMusicVolume); }

    private PlayerInput ResolveFreshPlayerInput(PlayerInput reference)
    {
        if (reference == null) return null;
        int targetIndex = reference.playerIndex;
        var registry = FindObjectOfType<PlayerRegistry>(true);
        if (registry != null)
        {
            foreach (var player in registry.GetAllPlayers())
            {
                if (player == null) continue;
                var pi = player.GetComponent<PlayerInput>();
                if (pi != null && pi.playerIndex == targetIndex) return pi;
            }
        }
        return reference;
    }

    private void SwitchOwnerToUIMap(PlayerInput owner)
    {
        if (owner == null || string.IsNullOrEmpty(uiActionMap)) return;
        owner.SwitchCurrentActionMap(uiActionMap);
    }

    private void SwitchOwnerToGameplayMap(PlayerInput owner)
    {
        if (owner == null || string.IsNullOrEmpty(gameplayActionMap)) return;
        owner.SwitchCurrentActionMap(gameplayActionMap);
    }

    private void SetGameplayControllersPaused(bool paused)
    {
        var registry = FindObjectOfType<PlayerRegistry>(true);
        if (registry == null) return;
        foreach (var player in registry.GetAllPlayers())
        {
            if (player == null) continue;
            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is IPausableGameplay pg) pg.SetPaused(paused);
            }
            var mario = player.GetComponentInChildren<MarioMovement>(true);
            if (mario != null) mario.enabled = !paused;
        }
    }

    private PlayerInput GetStandaloneOwner() => standaloneOwner != null ? standaloneOwner : FindObjectOfType<PlayerInput>(true);

    public void InvokeResumeButton()
    {
        if (resumeButton != null) resumeButton.onClick.Invoke();
        else ResumeGame();
    }

    public void SetPauseEnabled(bool enabled) => canPause = enabled;
}