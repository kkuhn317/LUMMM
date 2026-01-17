using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CheatsMenu;

public class CheatButton : MonoBehaviour
{
    [Header("Unlock Visuals")]
    public Sprite unlockedImage;
    public TMP_Text levelsRequiredText;
    public LevelButton[] requiredLevels;

    [Header("Cheat List UI")]
    public CanvasGroup mainCanvasGroup;      // Main level-select UI
    public CanvasGroup cheatListCanvasGroup; // Cheat list window
    public TMP_Text cheatListText;
    public Button closeButton;
    public AudioClip lockedSound;

    [Header("Unlock Sequence")]
    public SwipeController swipeController;  // SwipeController in the level select scene
    public float timeBeforeStartSequence = 1f;
    public int cheatPageIndex = 3;           // 1-based page index where this button lives
    public float pageMoveDelay = 1.5f;       // Total time to scroll from current page to cheat page

    [Header("White Flash Timing")]
    [Tooltip("Time it takes for the white flash to go from 0 -> 1.")]
    [Min(0.01f)]
    public float flashFadeInDuration = 0.5f;

    [Tooltip("Time the flash stays fully white before fading out.")]
    [Min(0.01f)]
    public float flashHoldDuration = 0.5f;

    [Tooltip("Time it takes for the white flash to go from 1 -> 0.")]
    [Min(0.01f)]
    public float flashFadeOutDuration = 0.5f;

    [Header("Background Dim")]
    public CanvasGroup dimBackground;
    [Range(0f, 1f)] public float dimTargetAlpha = 1f;

    [Min(0.01f)]
    public float dimFadeInDuration = 0.5f;

    [Min(0.01f)]
    public float dimFadeOutDuration = 0.5f;

    [Header("Sequence Options")]
    [Tooltip("Extra delay after the flash finishes and the button is revealed, before control is returned.")]
    public float afterFlashDelay = 0.5f;     // Small delay before giving control back
    public bool onlyShowUnlockOnce = true;
    public string unlockPlayerPrefsKey = "CheatButton_Club_Unlocked";

    [Header("Global Input")]
    public GlobalInputBlockerUI inputBlockerUI; // Fullscreen UI blocker

    [Header("Foreground Clone")]
    [Tooltip("Canvas/Panel in front of everything where the CLONE will be instantiated during the unlock sequence.")]
    public RectTransform foregroundCanvasRoot;

    [Tooltip("Root RectTransform of the cheat card (e.g. CheatButton Container). This is what will be CLONED.")]
    public RectTransform cheatButtonRoot;

    // Internal state
    private AudioSource audioSource;
    private Image buttonImage;          // Original button image (in the grid)
    private bool lockAcquired = false;

    private bool shouldRunUnlockSequence = false;
    private bool alreadyUnlocked = false;
    private int beatenLevels = 0;
    private int neededLevels = 0;

    // Clone references (used only during unlock sequence)
    private RectTransform cloneRoot;
    private Image cloneButtonImage;
    private CanvasGroup cloneWhiteFlash;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        buttonImage = GetComponent<Image>();

        // Ensure dim background starts hidden
        if (dimBackground != null)
        {
            dimBackground.alpha = 0f;
            dimBackground.interactable = false;
            dimBackground.blocksRaycasts = false;
            dimBackground.gameObject.SetActive(false);
        }

        // Progress state
        beatenLevels = GetLevelsBeaten();
        neededLevels = requiredLevels != null ? requiredLevels.Length : 0;
        alreadyUnlocked = WasCheatAlreadyUnlocked();

        // Decide whether to play the unlock sequence
        shouldRunUnlockSequence =
            (neededLevels > 0 &&
             beatenLevels >= neededLevels &&
             !alreadyUnlocked);

        // IMPORTANT: we no longer acquire the global lock here.
        // The lock is acquired inside PlayUnlockSequence() instead.
    }

    private void Start()
    {
        BuildCheatListText();

        // Wire up close button
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCheatListClose);

        // Required levels progress text
        if (levelsRequiredText != null)
            levelsRequiredText.text = beatenLevels + "/" + neededLevels;

        // If not all required levels beaten yet
        if (!shouldRunUnlockSequence && neededLevels > 0 && beatenLevels < neededLevels)
        {
            if (alreadyUnlocked && buttonImage != null && unlockedImage != null)
                buttonImage.sprite = unlockedImage;
            return;
        }

        // If cheat was already unlocked in a previous session
        if (!shouldRunUnlockSequence && alreadyUnlocked)
        {
            if (buttonImage != null && unlockedImage != null)
                buttonImage.sprite = unlockedImage;
        }

        // If all required levels are now beaten → play one-time unlock sequence
        if (shouldRunUnlockSequence)
            StartCoroutine(PlayUnlockSequence());
    }

    #region Button Clicks

    public void OnClick()
    {
        bool fullyUnlocked = GetLevelsBeaten() >= (requiredLevels?.Length ?? 0);

        if (fullyUnlocked || WasCheatAlreadyUnlocked())
        {
            if (audioSource != null)
                audioSource.Play();

            // Hide main select UI and show cheat list window
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.interactable   = false;
                mainCanvasGroup.blocksRaycasts = false;
            }

            if (cheatListCanvasGroup != null)
            {
                cheatListCanvasGroup.interactable   = true;
                cheatListCanvasGroup.blocksRaycasts = true;
                cheatListCanvasGroup.gameObject.SetActive(true);
            }

            if (EventSystem.current != null && closeButton != null)
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
        else
        {
            // Play locked sound if not unlocked yet
            if (audioSource != null && lockedSound != null)
                audioSource.PlayOneShot(lockedSound);
        }
    }

    public void OnCheatListClose()
    {
        // Restore main UI and hide cheat list
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.interactable   = true;
            mainCanvasGroup.blocksRaycasts = true;
        }

        if (cheatListCanvasGroup != null)
        {
            cheatListCanvasGroup.interactable   = false;
            cheatListCanvasGroup.blocksRaycasts = false;
            cheatListCanvasGroup.gameObject.SetActive(false);
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    #endregion

    #region Cheat List Text

    private void BuildCheatListText()
    {
        CheatBinding[] cheats = cheatBindings;

        if (cheatListText == null || cheats == null)
            return;

        cheatListText.text = "";

        foreach (var cheat in cheats)
        {
            // This button is specifically for the "club" cheat, so we skip it in the list
            if (cheat.code == "club")
                continue;

            cheatListText.text += $"{cheat.code} - {GetCheatDescription(cheat.code)}\n";
        }
    }

    #endregion

    #region Progress & Unlock State

    public int GetLevelsBeaten()
    {
        int count = 0;
        if (requiredLevels == null) return 0;

        foreach (LevelButton level in requiredLevels)
        {
            if (level != null && level.beaten)
                count++;
        }
        return count;
    }

    private bool WasCheatAlreadyUnlocked()
    {
        if (!onlyShowUnlockOnce) return false;
        return PlayerPrefs.GetInt(unlockPlayerPrefsKey, 0) == 1;
    }

    private void MarkCheatUnlocked()
    {
        if (!onlyShowUnlockOnce) return;

        PlayerPrefs.SetInt(unlockPlayerPrefsKey, 1);
        PlayerPrefs.Save();
    }

    #endregion

    #region Global Lock Helpers

    private void AcquireGlobalLockIfNeeded()
    {
        if (lockAcquired) return;

        GlobalInputLock.PushLock();
        lockAcquired = true;

        if (inputBlockerUI != null)
            inputBlockerUI.SetBlocked(true);
    }

    private void ReleaseGlobalLockIfHeld()
    {
        if (!lockAcquired) return;

        if (inputBlockerUI != null)
            inputBlockerUI.SetBlocked(false);

        GlobalInputLock.PopLock();
        lockAcquired = false;
    }

    #endregion

    #region Foreground Clone Helpers

    /// <summary>
    /// Creates a visual-only clone of the cheat card in the foreground canvas.
    /// The original stays in the grid, so its layout and position never change.
    /// </summary>
    private void CreateForegroundClone()
    {
        Debug.Log($"BEFORE CLONE - dimBackground.alpha: {dimBackground?.alpha}, active: {dimBackground?.gameObject.activeSelf}, in hierarchy: {dimBackground?.gameObject.transform.parent?.name}");
    
        if (foregroundCanvasRoot == null || cheatButtonRoot == null)
            return;

        // Instanciar el clon
        GameObject cloneGO = Instantiate(cheatButtonRoot.gameObject, foregroundCanvasRoot);
        
        // DESTRUIR INMEDIATAMENTE el componente CheatButton en el clon ANTES de que haga nada
        CheatButton[] cheatButtonsInClone = cloneGO.GetComponentsInChildren<CheatButton>(true);
        foreach (var cheatBtn in cheatButtonsInClone)
        {
            Debug.Log($"Destroying CheatButton component immediately on: {cheatBtn.gameObject.name}");
            DestroyImmediate(cheatBtn); // Use DestroyImmediate to make it instantaneous
        }
        
        // RESTAURAR el dimBackground inmediatamente
        if (dimBackground != null)
        {
            dimBackground.alpha = dimTargetAlpha;
            dimBackground.gameObject.SetActive(true);
            Debug.Log($"Restored dimBackground - alpha: {dimBackground.alpha}, active: {dimBackground.gameObject.activeSelf}");
        }
        
        Debug.Log($"AFTER INSTANTIATE - dimBackground.alpha: {dimBackground?.alpha}, active: {dimBackground?.gameObject.activeSelf}");
        
        // Check if any components on the clone might be causing this
        var allComponents = cloneGO.GetComponentsInChildren<MonoBehaviour>(true);
        Debug.Log($"Clone has {allComponents.Length} components total");
        foreach (var comp in allComponents)
        {
            Debug.Log($"  Component: {comp.GetType().Name} on {comp.gameObject.name}");
        }
        
        Debug.Log($"AFTER INSTANTIATE - dimBackground.alpha: {dimBackground?.alpha}, active: {dimBackground?.gameObject.activeSelf}");
        cloneRoot = cloneGO.GetComponent<RectTransform>();

        Debug.Log($"AFTER INSTANTIATE - dimBackground.alpha: {dimBackground?.alpha}");

        // Place it exactly where the original is on screen
        cloneRoot.position = cheatButtonRoot.position;
        cloneRoot.rotation = cheatButtonRoot.rotation;
        cloneRoot.localScale = cheatButtonRoot.localScale;

        // 1- Remove CheatButton logic from the clone so it never runs twice
        foreach (var cb in cloneGO.GetComponentsInChildren<CheatButton>(true))
        {
            Debug.Log($"Destroying CheatButton component on clone: {cb.gameObject.name}");
            Destroy(cb);
        }

        Debug.Log($"AFTER DESTROYING COMPONENTS - dimBackground.alpha: {dimBackground?.alpha}");

        // 2- Make the clone visual-only: no input, no raycasts
        CanvasGroup rootCg = cloneGO.GetComponent<CanvasGroup>();
        if (rootCg == null)
            rootCg = cloneGO.AddComponent<CanvasGroup>();

        rootCg.interactable   = false;
        rootCg.blocksRaycasts = false;

        foreach (var selectable in cloneGO.GetComponentsInChildren<Selectable>(true))
            selectable.interactable = false;

        // 3- Find the button image and the white-flash CanvasGroup on the clone
        cloneButtonImage = cloneGO.GetComponentInChildren<Image>(true);

        // There may be several CanvasGroups (root + overlay). We want the overlay one.
        cloneWhiteFlash = null;
        CanvasGroup[] groups = cloneGO.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var g in groups)
        {
            if (g == rootCg) continue; // skip the root we just added
            cloneWhiteFlash = g;
            break;
        }

        if (cloneWhiteFlash != null)
        {
            cloneWhiteFlash.alpha = 0f;
            cloneWhiteFlash.gameObject.SetActive(false);
        }
    }

    private void DestroyForegroundClone()
    {
        if (cloneRoot != null)
        {
            Destroy(cloneRoot.gameObject);
            cloneRoot = null;
            cloneButtonImage = null;
            cloneWhiteFlash  = null;
        }
    }

    #endregion

    #region Unlock Sequence

    private IEnumerator PlayUnlockSequence()
    {
        // Acquire global lock & blocker at the start of the sequence
        AcquireGlobalLockIfNeeded();

        // Disable UI navigation during the sequence
        bool previousNavState = true;
        if (EventSystem.current != null)
        {
            previousNavState = EventSystem.current.sendNavigationEvents;
            EventSystem.current.sendNavigationEvents = false;
        }

        if (timeBeforeStartSequence > 0f)
            yield return new WaitForSeconds(timeBeforeStartSequence);

        // 0 - Fade in background dim
        if (dimBackground != null)
        {
            dimBackground.gameObject.SetActive(true);
            dimBackground.blocksRaycasts = true;
            dimBackground.interactable = false;

            float startAlpha = dimBackground.alpha;
            yield return StartCoroutine(
                FadeCanvasGroup(dimBackground, startAlpha, dimTargetAlpha, dimFadeInDuration)
            );
            
            // Check alpha immediately after fade in
            Debug.Log($"IMMEDIATELY AFTER FADE IN - dimBackground.alpha: {dimBackground.alpha}");
        }

        // 1 - Scroll to the page where this button lives
        if (swipeController != null && pageMoveDelay > 0f)
        {
            Debug.Log($"BEFORE PAGE SCROLL - dimBackground.alpha: {dimBackground?.alpha}");
            swipeController.GoToPageSequentialFixed(cheatPageIndex, pageMoveDelay);
            yield return new WaitForSeconds(pageMoveDelay);
            Debug.Log($"AFTER PAGE SCROLL - dimBackground.alpha: {dimBackground?.alpha}");
        }

        // 2 - Create the foreground clone
        CreateForegroundClone();
        Debug.Log($"AFTER CLONE CREATION - dimBackground.alpha: {dimBackground?.alpha}");

        // 3 - Play white flash on the clone
        if (cloneRoot != null)
        {
            Debug.Log($"BEFORE WHITE FLASH - dimBackground.alpha: {dimBackground?.alpha}");
            yield return StartCoroutine(PlayWhiteFlashOnClone());
            Debug.Log($"AFTER WHITE FLASH - dimBackground.alpha: {dimBackground?.alpha}");
        }

        // Mark as unlocked for future sessions
        MarkCheatUnlocked();

        // Swap sprite on the ORIGINAL button in the list
        if (buttonImage != null && unlockedImage != null)
            buttonImage.sprite = unlockedImage;

        // Give the player a moment to SEE the unlocked sprite
        if (afterFlashDelay > 0f)
            yield return new WaitForSeconds(afterFlashDelay);

        // 4 - Fade out background dim, THEN deactivate
        if (dimBackground != null)
        {
            Debug.Log("CheatButton: START fade OUT on dimBackground");

            // Make sure it's active during fade
            if (!dimBackground.gameObject.activeSelf)
                dimBackground.gameObject.SetActive(true);

            // FORCE the alpha back since CreateForegroundClone() reset it
            dimBackground.alpha = dimTargetAlpha;
            
            Debug.Log($"Forced alpha to {dimTargetAlpha} before fade out");
            
            yield return StartCoroutine(
                FadeCanvasGroup(dimBackground, dimTargetAlpha, 0f, dimFadeOutDuration)
            );

            Debug.Log("CheatButton: FINISHED fade OUT, now disabling dimBackground");

            dimBackground.blocksRaycasts = false;
            dimBackground.interactable = false;
            dimBackground.gameObject.SetActive(false); // deactivate AFTER fade
        }

        // 5 - Destroy foreground clone
        DestroyForegroundClone();

        // Restore UI navigation
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = previousNavState;

        // Release global input lock and blocker
        ReleaseGlobalLockIfHeld();
    }

    private IEnumerator PlayWhiteFlashOnClone()
    {
        if (cloneWhiteFlash == null)
            yield break;

        // Ensure flash overlay is active
        cloneWhiteFlash.gameObject.SetActive(true);

        // Fade in 0 -> 1
        cloneWhiteFlash.alpha = 0f;
        float t = 0f;
        while (t < flashFadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / flashFadeInDuration);
            cloneWhiteFlash.alpha = lerp;
            yield return null;
        }
        cloneWhiteFlash.alpha = 1f;

        // Hold at full white
        if (flashHoldDuration > 0f)
            yield return new WaitForSeconds(flashHoldDuration);

        // Swap the clone's sprite while covered
        if (cloneButtonImage != null && unlockedImage != null)
            cloneButtonImage.sprite = unlockedImage;

        // Fade out 1 -> 0
        t = 0f;
        while (t < flashFadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / flashFadeOutDuration);
            cloneWhiteFlash.alpha = 1f - lerp;
            yield return null;
        }
        cloneWhiteFlash.alpha = 0f;
        cloneWhiteFlash.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

        // Safety: never allow a 0 or negative duration; that would "jump" instead of fade.
        if (duration <= 0f)
            duration = 0.01f;

        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // unscaled in case you pause timeScale
            float lerp = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, lerp);
            Debug.Log($"Alpha: {cg.alpha}");
            yield return null;
        }

        cg.alpha = to;
    }

    #endregion
}