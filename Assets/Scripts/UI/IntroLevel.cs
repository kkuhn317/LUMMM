using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using TMPro;
using System.Linq;
using System;

public class IntroLevel : MonoBehaviour
{
    public GameObject normalMarioObject, tinyMarioObject, nesMarioObject, conditionTextBox;
    public Image xImage, conditionIconImage, infiniteImage;
    public TMP_Text lives, conditionText, livesText;
    
    public GameObject capeMove, spinMove, wallJumpMove, groundPoundMove, crawlMove;
    public List<TargetData> targetDataList;
    public float moveDuration = 1f, delayBeforeMove = 1f, delayBeforeSceneTransition = 5f;
    public Color lineColor = Color.green;
    
    public UnityEvent OnMoveStart, OnTargetReached;
    public AudioSource audioSource;
    private int completedTweens = 0;
    private bool canSkip = false;
    private bool startedTransition = false;

    // We want to remove the event listener we install through InputSystem.onAnyButtonPress
    // after we're done so remember it here.
    private IDisposable m_EventListener;

    [System.Serializable]
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
                    startTransition();
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
        SetLevelData();
        LeanTween.reset();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        var hasConditionText = UpdateConditionText();

        if (GlobalVariables.levelInfo.marioMoves == MarioMoves.None)
        {
            Debug.Log("MarioMoves is None. Skipping animations.");
            StartCoroutine(DelayedSceneTransition());
            return;
        }

        if (targetDataList == null || targetDataList.Count == 0)
        {
            Debug.LogError("No target data assigned for IntroLevel.");
            return;
        }

        var filteredTargets = targetDataList.Where(data => data.target != null && (data.shouldMoveIfNoConditionText || hasConditionText)).ToList();
        if (filteredTargets.Count == 0)
        {
            Debug.Log("No valid targets left to animate.");
            StartCoroutine(DelayedSceneTransition());
            return;
        }

        AnimateTargets(filteredTargets);
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        Debug.Log($"Locale changed to: {newLocale.Identifier.Code}");
        UpdateConditionText();
    }

    private bool UpdateConditionText()
    {
        var table = LocalizationSettings.StringDatabase.GetTable("Game Text");
        var entry = table?.GetEntry("ConditionLevel_" + GlobalVariables.levelInfo.levelID);
        bool hasValidConditionText = entry != null && !string.IsNullOrWhiteSpace(entry.LocalizedValue);

        conditionTextBox.SetActive(hasValidConditionText);
        if (hasValidConditionText) conditionText.text = entry.LocalizedValue;

        return hasValidConditionText;
    }

    private void AnimateTargets(List<TargetData> filteredTargets)
    {
        completedTweens = 0;

        foreach (var data in filteredTargets)
        {
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
                        Debug.Log("All targets reached positions.");
                        OnTargetReached?.Invoke();
                        StartCoroutine(DelayedSceneTransition());
                    }
                });
        }
    }

    private void SetLevelData()
    {
        var levelInfo = GlobalVariables.levelInfo;
        if (levelInfo == null)
        {
            Debug.LogError("No level info found!");
            return;
        }

        EnableCorrectMarioObject(levelInfo.marioType);
        xImage?.SetSprite(levelInfo.xImage);
        conditionIconImage?.SetSprite(levelInfo.conditionIconImage);
        livesText?.SetText(levelInfo.lives.ToString());
        ApplyMoveSettings(levelInfo.marioMoves);
        UpdateLivesDisplay();
    }

    private void UpdateLivesDisplay()
    {
        bool infiniteLives = GlobalVariables.infiniteLivesMode;
        livesText.gameObject.SetActive(!infiniteLives);
        infiniteImage.gameObject.SetActive(infiniteLives);

        if (!infiniteLives)
        {
            livesText?.SetText(GlobalVariables.lives.ToString());
        }
    }

    private void EnableCorrectMarioObject(MarioType type)
    {
        normalMarioObject?.SetActive(type == MarioType.Normal);
        tinyMarioObject?.SetActive(type == MarioType.Tiny);
        nesMarioObject?.SetActive(type == MarioType.NES);
    }

    private void ApplyMoveSettings(MarioMoves moves)
    {
        capeMove?.SetActive(moves.HasFlag(MarioMoves.Cape));
        spinMove?.SetActive(moves.HasFlag(MarioMoves.Spin));
        wallJumpMove?.SetActive(moves.HasFlag(MarioMoves.WallJump));
        groundPoundMove?.SetActive(moves.HasFlag(MarioMoves.GroundPound));
        crawlMove?.SetActive(moves.HasFlag(MarioMoves.Crawl));
    }

    private IEnumerator DelayedSceneTransition()
    {
        canSkip = true;
        yield return new WaitForSeconds(delayBeforeSceneTransition);
        startTransition();
    }

    private void startTransition()
    {
        if (startedTransition) return;
        if (FadeInOutScene.Instance == null) {
            Debug.LogError("No FadeInOutScene instance found!");
            return;
        }
        if (FadeInOutScene.Instance.isTransitioning) {
            Debug.Log("Scene transition already in progress (Like fading into the level intro). Fine if called by skip, but not if called by DelayedSceneTransition.");
            return;
        }
        startedTransition = true;
        if (audioSource != null && GlobalVariables.levelInfo.transitionAudio != null) {
            audioSource.PlayOneShot(GlobalVariables.levelInfo.transitionAudio);
        }
        FadeInOutScene.Instance.LoadSceneWithFade(GlobalVariables.levelInfo.levelScene); 
    }

    private void OnDrawGizmos()
    {
        if (targetDataList == null) return;

        foreach (var data in targetDataList.Where(data => data.target != null))
        {
            Vector3 worldStart = data.target.position;
            Vector3 worldEnd = data.target.parent.TransformPoint(data.targetPosition);
            Debug.DrawLine(worldStart, worldEnd, lineColor);
        }
    }
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