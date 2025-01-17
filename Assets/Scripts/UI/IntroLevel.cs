using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using TMPro;
using System.Linq;

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
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Start()
    {
        SetLevelData();
        LeanTween.reset();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        if (GlobalVariables.levelInfo.marioMoves == MarioMoves.None)
        {
            Debug.Log("MarioMoves is None. Skipping animations.");
            StartCoroutine(DelayedSceneTransition(GlobalVariables.levelInfo.levelScene));
            return;
        }

        if (targetDataList == null || targetDataList.Count == 0)
        {
            Debug.LogError("No target data assigned for IntroLevel.");
            return;
        }

        var hasConditionText = UpdateConditionText();

        var filteredTargets = targetDataList.Where(data => data.target != null && (data.shouldMoveIfNoConditionText || hasConditionText)).ToList();
        if (filteredTargets.Count == 0)
        {
            Debug.Log("No valid targets left to animate.");
            StartCoroutine(DelayedSceneTransition(GlobalVariables.levelInfo.levelScene));
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
                        StartCoroutine(DelayedSceneTransition(GlobalVariables.levelInfo.levelScene));
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

        audioSource.pitch = (type == MarioType.Tiny) ? 1.5f : 1f;
    }

    private void ApplyMoveSettings(MarioMoves moves)
    {
        capeMove?.SetActive(moves.HasFlag(MarioMoves.Cape));
        spinMove?.SetActive(moves.HasFlag(MarioMoves.Spin));
        wallJumpMove?.SetActive(moves.HasFlag(MarioMoves.WallJump));
        groundPoundMove?.SetActive(moves.HasFlag(MarioMoves.GroundPound));
        crawlMove?.SetActive(moves.HasFlag(MarioMoves.Crawl));
    }

    private IEnumerator DelayedSceneTransition(string nextScene)
    {
        yield return new WaitForSeconds(delayBeforeSceneTransition);
        if (FadeInOutScene.Instance != null)
        {
            audioSource?.PlayOneShot(GlobalVariables.levelInfo.transitionAudio);
            FadeInOutScene.Instance.LoadSceneWithFade(nextScene);
        }
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