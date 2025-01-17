using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class IntroLevel : MonoBehaviour
{
    public GameObject normalMarioObject;
    public GameObject tinyMarioObject;
    public GameObject nesMarioObject;
    public GameObject conditionTextBox;
    public Image xImage;
    public TMP_Text lives;
    public TMP_Text conditionText;
    public TMP_Text livesText;
    public Image conditionIconImage;

    // Move Objects (UI elements that show available moves)
    public GameObject capeMove;
    public GameObject spinMove;
    public GameObject wallJumpMove;
    public GameObject groundPoundMove;
    public GameObject crawlMove;

    [System.Serializable]
    public class TargetData
    {
        public RectTransform target; // The UI element to move
        public Vector3 targetPosition; // The specific target position for this UI element
        public LeanTweenType easingType = LeanTweenType.linear; // Easing type for the move
        public bool shouldMoveIfNoConditionText = true;
    }

    public List<TargetData> targetDataList; // List of targets and their respective transition settings
    public float moveDuration = 1f; // Global duration of the move
    public float delayBeforeMove = 1f; // Global delay before starting the movement
    public float delayBeforeSceneTransition = 5f;
    public Color lineColor = Color.green; // Color of the debug lines
    public UnityEvent OnMoveStart; // Event triggered when a move starts
    public UnityEvent OnTargetReached; // Event triggered when all targets finish moving

    private int completedTweens = 0;
    public AudioSource audioSource;

    private void Start()
    {
        // Load level data before doing anything else
        SetLevelData();

        // Reset LeanTween to avoid stale tweens
        LeanTween.reset();

        // Force layout update to ensure consistent UI positions
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        // If MarioMoves is None, skip the animation and go directly to the scene transition
        if (GlobalVariables.levelInfo.marioMoves == MarioMoves.None)
        {
            Debug.Log("MarioMoves is None. Skipping LeanTween animations.");
            StartCoroutine(DelayedSceneTransition(GlobalVariables.levelInfo.levelScene));
            return;
        }

        if (targetDataList == null || targetDataList.Count == 0)
        {
            Debug.LogError("No target data assigned for IntroLevel.");
            return;
        }

        completedTweens = 0;
        bool hasConditionText = conditionText != null && !string.IsNullOrEmpty(GlobalVariables.levelInfo.condition);

        // Filter out objects that shouldn't move
        List<TargetData> filteredTargets = targetDataList.FindAll(data =>
            data.target != null && (data.shouldMoveIfNoConditionText || hasConditionText));

        if (filteredTargets.Count == 0)
        {
            Debug.Log("No valid targets left to animate.");
            StartCoroutine(DelayedSceneTransition(GlobalVariables.levelInfo.levelScene));
            return;
        }

        foreach (TargetData data in filteredTargets)
        {
            Debug.Log($"Preparing to move {data.target.name} to {data.targetPosition} with easing {data.easingType}");

            LeanTween.move(data.target, data.targetPosition, moveDuration)
                .setDelay(delayBeforeMove) // Use global delay
                .setEase(data.easingType) // Use easing type from TargetData
                .setOnStart(() =>
                {
                    Debug.Log($"Started moving {data.target.name}");
                    OnMoveStart?.Invoke(); // Trigger OnMoveStart event
                })
                .setOnComplete(() =>
                {
                    Debug.Log($"Completed moving {data.target.name} to {data.targetPosition}");
                    completedTweens++;

                    if (completedTweens == filteredTargets.Count)
                    {
                        Debug.Log("All targets have reached their positions.");
                        OnTargetReached?.Invoke();
                        StartCoroutine(DelayedSceneTransition(GlobalVariables.levelInfo.levelScene));
                    }
                });
        }
    }

    private void EnableCorrectMarioObject(MarioType type)
    {
        // Disable all Mario objects
        if (normalMarioObject != null) normalMarioObject.SetActive(false);
        if (tinyMarioObject != null) tinyMarioObject.SetActive(false);
        if (nesMarioObject != null) nesMarioObject.SetActive(false);

        // Enable the correct Mario object based on type
        switch (type)
        {
            case MarioType.Normal:
                if (normalMarioObject != null) normalMarioObject.SetActive(true); audioSource.pitch = 1f;
                break;
            case MarioType.Tiny:
                if (tinyMarioObject != null) tinyMarioObject.SetActive(true); audioSource.pitch = 1.5f;
                break;
            case MarioType.NES:
                if (nesMarioObject != null) nesMarioObject.SetActive(true); audioSource.pitch = 1f;
                break;
        }
    }

    private void ApplyMoveSettings(MarioMoves moves)
    {
        // Enable or disable objects based on the moves available
        if (capeMove != null) capeMove.SetActive(moves.HasFlag(MarioMoves.Cape));
        if (spinMove != null) spinMove.SetActive(moves.HasFlag(MarioMoves.Spin));
        if (wallJumpMove != null) wallJumpMove.SetActive(moves.HasFlag(MarioMoves.WallJump));
        if (groundPoundMove != null) groundPoundMove.SetActive(moves.HasFlag(MarioMoves.GroundPound));
        if (crawlMove != null) crawlMove.SetActive(moves.HasFlag(MarioMoves.Crawl));
    }

    private IEnumerator DelayedSceneTransition(string nextScene)
    {
        yield return new WaitForSeconds(delayBeforeSceneTransition);
        if (FadeInOutScene.Instance != null)
        {
            if (GlobalVariables.levelInfo.transitionAudio != null){
                audioSource.PlayOneShot(GlobalVariables.levelInfo.transitionAudio);
            }
            FadeInOutScene.Instance.LoadSceneWithFade(nextScene); // Transition to the actual level
        }
    }

    private void SetLevelData()
    {
        LevelInfo levelInfo = GlobalVariables.levelInfo;

        if (levelInfo == null)
        {
            Debug.LogError("No level info found in GlobalVariables!");
            return;
        }

        // Enable the correct Mario object
        EnableCorrectMarioObject(levelInfo.marioType);

        // Set X Sprite (or special object)
        if (xImage != null && levelInfo.xImage != null)
        {
            xImage.sprite = levelInfo.xImage;
            xImage.enabled = true;
        }
        else if (xImage != null)
        {
            xImage.enabled = false;
        }

        // Set Condition Text
        if (conditionText != null && !string.IsNullOrEmpty(levelInfo.condition))
        {
            conditionText.text = levelInfo.condition;
        } 
        else 
        {
            conditionTextBox.SetActive(false);
        }

        // Set Condition Icon
        if (conditionIconImage != null && levelInfo.conditionIconImage != null)
        {
            conditionIconImage.sprite = levelInfo.conditionIconImage;
            conditionIconImage.enabled = true;
        }
        else if (conditionIconImage != null)
        {
            conditionIconImage.enabled = false;
        }

        // Set Lives
        if (livesText != null)
        {
            livesText.text = levelInfo.lives.ToString();
        }

        // Apply Move Settings
        ApplyMoveSettings(levelInfo.marioMoves);
    }

    private void OnDrawGizmos()
    {
        if (targetDataList == null || targetDataList.Count == 0)
            return;

        foreach (TargetData data in targetDataList)
        {
            if (data.target == null) continue;

            // Convert UI positions to world space for visualization
            Vector3 worldStart = data.target.position;
            Vector3 worldEnd = data.target.parent.TransformPoint(data.targetPosition);

            // Use Debug.DrawLine to visualize paths in world space
            Debug.DrawLine(worldStart, worldEnd, lineColor);
        }
    }
}