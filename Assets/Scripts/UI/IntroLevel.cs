using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.Utilities;

public class IntroLevel : MonoBehaviour
{
    [Header("Mario Variants")]
    [SerializeField] private GameObject normalMarioObject;
    [SerializeField] private GameObject tinyMarioObject;
    [SerializeField] private GameObject nesMarioObject;

    [Header("Condition UI")]
    [SerializeField] private GameObject conditionTextBox;
    [SerializeField] private Image xImage;
    [SerializeField] private Image conditionIconImage;
    [SerializeField] private Image infiniteImage;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text conditionText;

    [Header("Moves UI")]
    [SerializeField] private MarioMovesDisplay movesDisplay;

    [Header("Intro Animation Targets")]
    [SerializeField] private List<TargetData> targetDataList = new List<TargetData>();
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float delayBeforeMove = 1f;
    [SerializeField] private float delayBeforeSceneTransition = 5f;
    [SerializeField] private Color lineColor = Color.green;

    [Header("Events")]
    public UnityEvent OnMoveStart;
    public UnityEvent OnTargetReached;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    // Runtime state
    private int completedTweens = 0;
    private bool canSkip = false;
    private bool startedTransition = false;
    private IDisposable m_EventListener;

    [Serializable]
    public class TargetData
    {
        public RectTransform target;
        public Vector3 targetPosition;
        public LeanTweenType easingType = LeanTweenType.linear;
        public bool shouldMoveIfNoConditionText = true;
    }
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // Subscribe to global button presses
        m_EventListener = InputSystem.onAnyButtonPress
            .Call(ctrl =>
            {
                if (canSkip && !startedTransition && this != null)
                {
                    StartTransition();
                }
            });
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        // Unsubscribe from global button presses
        m_EventListener?.Dispose();
        m_EventListener = null;
    }

    private void Start()
    {
        var levelInfo = GlobalVariables.levelInfo;
        if (levelInfo == null)
        {
            Debug.LogError($"{nameof(IntroLevel)}: No level info found!");
            return;
        }

        // Basic level UI setup
        SetLevelData(levelInfo);

        // Reset LeanTween (to ensure clean state) and layout
        LeanTween.reset();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        // Condition text (localized) and animation decisions
        bool hasConditionText = UpdateConditionText(levelInfo);

        // If Mario has no moves in this level, skip the intro animation entirely
        if (levelInfo.marioMoves == MarioMoves.None)
        {
            Debug.Log("MarioMoves is None. Skipping intro animations.");
            StartCoroutine(DelayedSceneTransition());
            return;
        }

        // Filter targets to animate based on condition text availability
        var filteredTargets = GetFilteredTargets(hasConditionText);
        if (filteredTargets.Count == 0)
        {
            Debug.Log("No valid targets left to animate. Skipping animations.");
            StartCoroutine(DelayedSceneTransition());
            return;
        }

        AnimateTargets(filteredTargets);
    }

    #region Localization & Condition
    private void OnLocaleChanged(Locale newLocale)
    {
        Debug.Log($"Locale changed to: {newLocale.Identifier.Code}");

        // Refresh condition text whenever the language changes
        if (GlobalVariables.levelInfo != null)
        {
            UpdateConditionText(GlobalVariables.levelInfo);
        }
    }

    /// <summary>
    /// Updates the condition text using the "Game Text" string table and the current level ID.
    /// Returns true if a valid condition string exists.
    /// </summary>
    private bool UpdateConditionText(LevelInfo levelInfo)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("Game Text");
        var entryKey = "ConditionLevel_" + levelInfo.levelID;
        var entry = table?.GetEntry(entryKey);

        bool hasValidConditionText = entry != null && !string.IsNullOrWhiteSpace(entry.LocalizedValue);

        if (conditionTextBox != null)
        {
            conditionTextBox.SetActive(hasValidConditionText);
        }

        if (hasValidConditionText && conditionText != null)
        {
            conditionText.text = entry.LocalizedValue;
        }

        return hasValidConditionText;
    }

    #endregion

    #region Intro Animation
    private List<TargetData> GetFilteredTargets(bool hasConditionText)
    {
        if (targetDataList == null)
            return new List<TargetData>();

        return targetDataList
            .Where(data =>
                data != null &&
                data.target != null &&
                (data.shouldMoveIfNoConditionText || hasConditionText))
            .ToList();
    }

    private void AnimateTargets(List<TargetData> filteredTargets)
    {
        completedTweens = 0;

        foreach (var data in filteredTargets)
        {
            if (data?.target == null)
                continue;

            Debug.Log($"Moving {data.target.name} to {data.targetPosition} with easing {data.easingType}");

            LeanTween.move(data.target, data.targetPosition, moveDuration)
                .setDelay(delayBeforeMove)
                .setEase(data.easingType)
                .setOnStart(() =>
                {
                    Debug.Log($"Started moving {data.target.name}");
                    OnMoveStart?.Invoke();
                })
                .setOnComplete(() =>
                {
                    Debug.Log($"Completed moving {data.target.name}");

                    if (++completedTweens == filteredTargets.Count)
                    {
                        Debug.Log("All intro targets reached positions.");
                        OnTargetReached?.Invoke();
                        StartCoroutine(DelayedSceneTransition());
                    }
                });
        }
    }

    private IEnumerator DelayedSceneTransition()
    {
        canSkip = true;
        yield return new WaitForSeconds(delayBeforeSceneTransition);
        StartTransition();
    }

    private void StartTransition()
    {
        if (startedTransition)
            return;

        if (FadeInOutScene.Instance == null)
        {
            Debug.LogError("No FadeInOutScene instance found!");
            return;
        }

        if (FadeInOutScene.Instance.isTransitioning)
        {
            Debug.Log("Scene transition already in progress. Ignoring duplicate call.");
            return;
        }

        startedTransition = true;

        var levelInfo = GlobalVariables.levelInfo;

        if (audioSource != null && levelInfo != null && levelInfo.transitionAudio != null)
        {
            audioSource.PlayOneShot(levelInfo.transitionAudio);
        }

        if (levelInfo != null)
        {
            FadeInOutScene.Instance.LoadSceneWithFade(levelInfo.levelScene);
        }
        else
        {
            Debug.LogError("Cannot load level: GlobalVariables.levelInfo is null.");
        }
    }

    #endregion

    #region Level Data & Lives
    private void SetLevelData(LevelInfo levelInfo)
    {
        // Mario variant
        EnableCorrectMarioObject(levelInfo.marioType);

        // Static UI images
        xImage?.SetSprite(levelInfo.xImage);
        conditionIconImage?.SetSprite(levelInfo.conditionIconImage);

        // Lives for this level (initial display)
        if (livesText != null)
        {
            livesText.SetText(levelInfo.lives.ToString());
        }

        // Lives display mode (infinite / normal)
        UpdateLivesDisplay();

        // Ability UI (delegated to MarioMovesDisplay)
        if (movesDisplay != null)
        {
            movesDisplay.RefreshFromGlobalLevelInfo();
        }
    }

    private void UpdateLivesDisplay()
    {
        bool infiniteLives = GlobalVariables.infiniteLivesMode;

        if (livesText != null)
        {
            livesText.gameObject.SetActive(!infiniteLives);
        }

        if (infiniteImage != null)
        {
            infiniteImage.gameObject.SetActive(infiniteLives);
        }

        if (!infiniteLives && livesText != null)
        {
            livesText.SetText(GlobalVariables.lives.ToString());
        }
    }

    private void EnableCorrectMarioObject(MarioType type)
    {
        if (normalMarioObject != null)
            normalMarioObject.SetActive(type == MarioType.Normal);

        if (tinyMarioObject != null)
            tinyMarioObject.SetActive(type == MarioType.Tiny);

        if (nesMarioObject != null)
            nesMarioObject.SetActive(type == MarioType.NES);
    }

    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (targetDataList == null)
            return;

        foreach (var data in targetDataList.Where(d => d != null && d.target != null))
        {
            Vector3 worldStart = data.target.position;
            Vector3 worldEnd = data.target.parent != null
                ? data.target.parent.TransformPoint(data.targetPosition)
                : data.targetPosition;

            Debug.DrawLine(worldStart, worldEnd, lineColor);
        }
    }

    #endregion
}

// Helper Extension Methods
public static class UIExtensions
{
    public static void SetSprite(this Image image, Sprite sprite)
    {
        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }

    public static void SetText(this TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }
}
