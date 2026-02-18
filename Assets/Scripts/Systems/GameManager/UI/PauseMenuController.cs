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

public class PauseMenuController : MonoBehaviour
{
    public enum PauseMenuMode
    {
        InGamePauseMenu,
        StandaloneOptionsMenu
    }

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject resetConfirmPanel;

    [Header("Button References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private Button restartFromBeginningButton;
    [SerializeField] private Button restartFromCheckpointButton;

    [Header("Pause Settings")]
    [SerializeField] private float pauseMusicVolumeMultiplier = 0.25f;
    [SerializeField] private bool canPause = true;

    [Header("Input")]
    [SerializeField] private InputActionReference pauseActionRef;
    [SerializeField] private InputActionReference cancelActionRef;

    [Header("Legacy Parity")]
    [SerializeField] private GameObject[] disablingMenusOnResume;
    [SerializeField] private bool hideCursorOnResume = true;

    [Header("UI Blocking While Paused")]
    [Tooltip("CanvasGroups to make non-interactable while paused (also blocks raycasts so clicks don't pass through).")]
    [SerializeField] private CanvasGroup[] blockWhilePaused;

    [Header("Shared Player Actions (Optional)")]
    [SerializeField] private InputActionAsset sharedPlayerActionsAsset;

    [Header("Mode")]
    [SerializeField] private PauseMenuMode mode = PauseMenuMode.InGamePauseMenu;

    [Header("External Pause Handler (Optional)")]
    [Tooltip("If assigned, this behaviour can react to Pause/Resume and can gate pause toggling (implements IOptionsPauseHandler and/or IPauseToggleGate).")]
    [SerializeField] private MonoBehaviour optionsPauseHandlerBehaviour; // may implement IOptionsPauseHandler / IPauseToggleGate

    private IOptionsPauseHandler optionsPauseHandler;
    private IPauseToggleGate pauseToggleGate; // optional single gate from handler

    private bool isPaused = false;
    private float originalMusicVolume = 1f;
    private float previousTimeScale = 1f;
    private int toggleGuardFrame = -1;
    private InputAction pauseAction;
    private InputAction cancelAction;

    private enum MenuState
    {
        Main,
        Options,
        ResetConfirm
    }

    private MenuState currentMenuState = MenuState.Main;

    private void Awake()
    {
        if (pauseActionRef != null) pauseAction = pauseActionRef.action;
        if (cancelActionRef != null) cancelAction = cancelActionRef.action;

        if (pauseAction == null)
            Debug.LogWarning("Pause Action Reference is not assigned in PauseMenuController");

        if (MusicManager.Instance != null)
            originalMusicVolume = MusicManager.Instance.GetCurrentVolume();

        ResolveExternalHandler();
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed += OnPausePerformed;
            pauseAction.Enable();
        }

        if (cancelAction != null)
        {
            cancelAction.performed += OnCancelPerformed;
            cancelAction.Enable();
        }

        GameEvents.OnGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }

        if (cancelAction != null)
        {
            cancelAction.performed -= OnCancelPerformed;
            cancelAction.Disable();
        }

        GameEvents.OnGameOver -= OnGameOver;
    }

    private void Start()
    {
        SetupButtonListeners();

        // In-game pause menu starts hidden.
        if (pauseMenu != null && mode == PauseMenuMode.InGamePauseMenu)
            pauseMenu.SetActive(false);
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            RestoreMusicVolume();
            Time.timeScale = previousTimeScale;
        }
    }

    public void InitializeForLevel()
    {
        ResolveExternalHandler();

        // Normalize state on init
        if (isPaused)
            ResumeGame();

        if (MusicManager.Instance != null)
            originalMusicVolume = MusicManager.Instance.GetCurrentVolume();

        canPause = true;
        isPaused = false;
        previousTimeScale = 1f;
        Time.timeScale = 1f;
        currentMenuState = MenuState.Main;

        if (mode == PauseMenuMode.StandaloneOptionsMenu)
        {
            // Standalone options scene starts paused, but the UI should remain visible always.
            if (pauseMenu != null) pauseMenu.SetActive(true);
            if (!isPaused) PauseGame();
        }
        else
        {
            if (pauseMenu != null) pauseMenu.SetActive(false);
        }
    }

    private void ResolveExternalHandler()
    {
        optionsPauseHandler = optionsPauseHandlerBehaviour as IOptionsPauseHandler;
        pauseToggleGate = optionsPauseHandlerBehaviour as IPauseToggleGate;
    }

    private bool ToggleAlreadyHandledThisFrame() => toggleGuardFrame == Time.frameCount;
    private void MarkToggleHandledThisFrame() => toggleGuardFrame = Time.frameCount;

    private void SetupButtonListeners()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(ShowOptions);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartPressed);
        if (quitButton != null) quitButton.onClick.AddListener(QuitLevel);
        if (optionsBackButton != null) optionsBackButton.onClick.AddListener(ShowMainMenu);

        if (restartFromBeginningButton != null)
            restartFromBeginningButton.onClick.AddListener(RestartFromBeginning);

        if (restartFromCheckpointButton != null)
            restartFromCheckpointButton.onClick.AddListener(RestartFromCheckpoint);
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (!CanTogglePauseNow())
            return;

        if (ToggleAlreadyHandledThisFrame()) return;
        MarkToggleHandledThisFrame();

        TogglePause();
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (!CanTogglePauseNow())
            return;

        if (!isPaused) return;

        if (ToggleAlreadyHandledThisFrame()) return;
        MarkToggleHandledThisFrame();

        switch (currentMenuState)
        {
            case MenuState.Options:
            case MenuState.ResetConfirm:
                ShowMainMenu();
                break;

            case MenuState.Main:
                // In standalone options scene, Cancel should not "resume" and hide the menu.
                if (mode == PauseMenuMode.InGamePauseMenu)
                    ResumeGame();
                break;
        }
    }

    private void OnGameOver()
    {
        if (isPaused)
            ResumeGame();

        canPause = false;
    }

    public void TogglePause()
    {
        if (!canPause) return;

        if (isPaused) ResumeGame();
        else PauseGame();
    }

    private bool CanTogglePauseNow()
    {
        // First: allow a single handler gate if provided (optional)
        if (pauseToggleGate != null && !pauseToggleGate.CanTogglePause)
            return false;

        // Then: scan any active/enabled gates in the scene (RebindSettings, etc.)
        var behaviours = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in behaviours)
        {
            if (!(mb is IPauseToggleGate gate)) continue;

            // Only treat the gate as blocking if the component is active & enabled
            if (!mb.isActiveAndEnabled) continue;

            if (!gate.CanTogglePause)
                return false;
        }

        return true;
    }

    public void PauseGame()
    {
        if (!canPause || isPaused) return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        ReduceMusicVolume();
        CursorHelper.ShowCursor();

        if (blockWhilePaused != null)
        {
            foreach (var cg in blockWhilePaused)
            {
                if (cg == null) continue;
                cg.interactable = false;
                cg.blocksRaycasts = true;
            }
        }

        // Pause menu should be visible in both modes when paused.
        if (pauseMenu != null) pauseMenu.SetActive(true);

        optionsPauseHandler?.OnPause();

        ShowMainMenu();

        if (resumeButton != null && resumeButton.gameObject.activeInHierarchy)
            resumeButton.Select();

        DisablePlayerInputs();
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
                if (cg == null) continue;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        optionsPauseHandler?.OnResume();

        // IMPORTANT:
        // In StandaloneOptionsMenu, pauseMenu is the whole UI and must NOT be hidden on resume.
        if (pauseMenu != null && mode == PauseMenuMode.InGamePauseMenu)
            pauseMenu.SetActive(false);

        if (disablingMenusOnResume != null)
        {
            foreach (var go in disablingMenusOnResume)
                if (go != null) go.SetActive(false);
        }

        EnablePlayerInputs();
        ReassignPlayerActionsIfNeeded();

        if (hideCursorOnResume)
            CursorHelper.HideCursor();

        GameEvents.TriggerGameResumed();
    }

    private void ReduceMusicVolume()
    {
        if (MusicManager.Instance != null)
        {
            originalMusicVolume = MusicManager.Instance.GetCurrentVolume();
            MusicManager.Instance.SetCurrentVolume(originalMusicVolume * pauseMusicVolumeMultiplier);
        }
    }

    private void RestoreMusicVolume()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetCurrentVolume(originalMusicVolume);
    }

    private void DisablePlayerInputs()
    {
        var registry = FindObjectOfType<PlayerRegistry>();
        if (registry != null)
        {
            foreach (var player in registry.GetAllPlayers())
                player?.DisableInputs();
        }
    }

    private void EnablePlayerInputs()
    {
        var registry = FindObjectOfType<PlayerRegistry>();
        if (registry != null)
        {
            foreach (var player in registry.GetAllPlayers())
                player?.EnableInputs();
        }
    }

    private void ReassignPlayerActionsIfNeeded()
    {
        if (sharedPlayerActionsAsset == null) return;

        var registry = FindObjectOfType<PlayerRegistry>();
        if (registry == null) return;

        foreach (var player in registry.GetAllPlayers())
        {
            if (player == null) continue;

            var pi = player.GetComponent<PlayerInput>();
            if (pi != null)
                pi.actions = sharedPlayerActionsAsset;
        }
    }

    #region Menu Navigation

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (resetConfirmPanel != null) resetConfirmPanel.SetActive(false);

        currentMenuState = MenuState.Main;

        if (resumeButton != null && resumeButton.gameObject.activeInHierarchy)
            resumeButton.Select();
    }

    public void ShowOptions()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (resetConfirmPanel != null) resetConfirmPanel.SetActive(false);

        currentMenuState = MenuState.Options;

        if (optionsBackButton != null && optionsBackButton.gameObject.activeInHierarchy)
            optionsBackButton.Select();
    }

    private void OnRestartPressed()
    {
        if (HasCheckpointSaved())
            ShowResetConfirm();
        else
            RestartFromBeginning();
    }

    private bool HasCheckpointSaved()
    {
        var checkpointManager = FindObjectOfType<CheckpointManager>();
        if (checkpointManager == null) return false;

        return checkpointManager.HasCheckpoint;
    }

    public void ShowResetConfirm()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (resetConfirmPanel != null) resetConfirmPanel.SetActive(true);

        currentMenuState = MenuState.ResetConfirm;

        var cancelButton = resetConfirmPanel.GetComponentInChildren<Button>(true);
        if (cancelButton != null)
            cancelButton.Select();
    }

    #endregion

    #region Level Control

    public void RestartFromBeginning()
    {
        ResumeGame();
        GameManagerRefactored.Instance?.RestartLevelFromBeginning();
    }

    public void RestartFromCheckpoint()
    {
        ResumeGame();
        GameManagerRefactored.Instance?.RestartLevelFromCheckpoint();
    }

    public void QuitLevel()
    {
        ResumeGame();
        GameManagerRefactored.Instance?.QuitLevel();
    }

    #endregion

    public void SetPauseEnabled(bool enabled) => canPause = enabled;

    public bool IsPaused => isPaused;
}