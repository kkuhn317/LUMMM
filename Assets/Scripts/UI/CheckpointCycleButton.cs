using System;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointCycleUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button cycleButton;
    [SerializeField] private RectTransform optionsContainer;

    [Header("Cycle")]
    [Tooltip("0=Off, 1=Classic, 2=Silent/RespawnNear")]
    [SerializeField] private int optionCount = 3;

    [Tooltip("Distance moved per option (anchoredPosition delta). Usually (0, itemHeight) or (0, -itemHeight).")]
    [SerializeField] private Vector2 step = new Vector2(0f, 106.4f);

    [Header("Tween")]
    [SerializeField] private float tweenTime = 0.18f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeOutQuad;
    [SerializeField] private bool ignoreTimeScale = true;

    public int CurrentMode { get; private set; } = 0;

    /// <summary>
    /// Fired when the UI wants to change mode (ModifiersSettings decides if allowed and then calls SetMode()).
    /// </summary>
    public event Action<int> OnRequestModeChange;

    private int tweenId = -1;
    private Vector2 basePos;

    private void Awake()
    {
        if (optionsContainer != null)
            basePos = optionsContainer.anchoredPosition;

        if (cycleButton != null)
            cycleButton.onClick.AddListener(RequestNext);
    }

    public void SetModeInstant(int mode)
    {
        mode = Mathf.Clamp(mode, 0, optionCount - 1);
        CurrentMode = mode;

        CancelTween();
        if (optionsContainer != null)
            optionsContainer.anchoredPosition = basePos + step * CurrentMode;
    }

    public void SetModeAnimated(int mode)
    {
        mode = Mathf.Clamp(mode, 0, optionCount - 1);
        CurrentMode = mode;

        MoveTo(CurrentMode, instant: false);
    }

    public void RequestNext()
    {
        int next = CurrentMode + 1;
        if (next >= optionCount) next = 0;

        OnRequestModeChange?.Invoke(next);
    }

    private void MoveTo(int mode, bool instant)
    {
        if (optionsContainer == null) return;

        Vector2 target = basePos + step * mode;

        CancelTween();

        if (instant || tweenTime <= 0f)
        {
            optionsContainer.anchoredPosition = target;
            return;
        }

        var t = LeanTween.move(optionsContainer, target, tweenTime).setEase(ease);
        if (ignoreTimeScale) t.setIgnoreTimeScale(true);
        tweenId = t.id;
    }

    private void CancelTween()
    {
        if (tweenId != -1)
        {
            LeanTween.cancel(tweenId);
            tweenId = -1;
        }
    }
}