using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum NavigationDirection { Up, Down, Left, Right }

[System.Serializable]
public class NavigationRule
{
    public int page;
    public GameObject currentButton;
    public NavigationDirection direction;
    public GameObject targetButton;
}

public class SwipeController : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] int maxPage;
    public int currentPage;
    Vector3 targetPos;
    [SerializeField] Vector3 pageStep;
    [SerializeField] RectTransform levelPagesRect;

    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;
    float dragThreshold;
    [SerializeField] Button previousBtn, nextBtn;

    [System.Serializable]
    public class BarOption
    {
        public List<Image> barImages;
        public List<Sprite> barClosed;
        public List<Sprite> barOpen;
    }
    [SerializeField] BarOption[] barOptions;

    [SerializeField] List<CanvasGroup> pageCanvasGroups;
    private Vector3 initialPosition;

    public delegate void PageChangedHandler(int newPage);
    public event PageChangedHandler OnPageChanged;

    [SerializeField] private List<NavigationRule> navigationRules = new List<NavigationRule>();
    private Dictionary<(int, GameObject, string), GameObject> navigationMap;

    [SerializeField] private InputActionReference navigateAction;

    private void Awake()
    {
        currentPage = 1;
        initialPosition = levelPagesRect.localPosition;
        targetPos = levelPagesRect.localPosition;
        dragThreshold = Screen.width / 15;
        UpdateBar();
        UpdateCanvasGroups();
        UpdateButtonStates();
        LeanTween.reset();

        navigationMap = new Dictionary<(int, GameObject, string), GameObject>();
        foreach (var rule in navigationRules)
        {
            if (rule.currentButton != null && rule.targetButton != null)
            {
                navigationMap[(rule.page, rule.currentButton, rule.direction.ToString().ToLower())] = rule.targetButton;
            }
        }
    }

    private void OnEnable()
    {
        if (navigateAction != null)
            navigateAction.action.performed += OnNavigate;
    }

    private void OnDisable()
    {
        if (navigateAction != null)
            navigateAction.action.performed -= OnNavigate;
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if (input == Vector2.zero) return;

        if (Mathf.Abs(input.y) > Mathf.Abs(input.x))
        {
            if (input.y > 0.5f)
                NavigateFromCurrent("up");
            else if (input.y < -0.5f)
                NavigateFromCurrent("down");
        }
        else
        {
            if (input.x > 0.5f)
                NavigateFromCurrent("right");
            else if (input.x < -0.5f)
                NavigateFromCurrent("left");
        }
    }

    public void Next()
    {
        if (currentPage < maxPage)
        {
            currentPage++;
            targetPos += pageStep;
            MovePage();
        }
        UpdateButtonStates();
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
        UpdateButtonStates();
    }

    public void GoToPage(int targetPage)
    {
        if (targetPage == currentPage) return;

        if (targetPage < 1 || targetPage > maxPage)
        {
            Debug.LogWarning($"Invalid page number: {targetPage}. Must be between 1 and {maxPage}.");
            return;
        }

        currentPage = targetPage;
        targetPos = initialPosition + (targetPage - 1) * pageStep;
        MovePage();
    }

    private bool preventSelection = false;

    public void PreventAutoSelection(bool state)
    {
        preventSelection = state;
    }

    void MovePage()
    {
        levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType).setIgnoreTimeScale(true);
        UpdateBar();
        UpdateCanvasGroups();
        UpdateButtonStates();

        OnPageChanged?.Invoke(currentPage);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float dragAmount = eventData.position.x - eventData.pressPosition.x;
        int predictedPage = currentPage;

        if (Mathf.Abs(dragAmount) > dragThreshold)
        {
            predictedPage = (dragAmount > 0) ? Mathf.Max(1, currentPage - 1) : Mathf.Min(maxPage, currentPage + 1);
        }

        if (predictedPage != currentPage)
        {
            UpdateButtonStates(predictedPage);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
        {
            if (eventData.position.x > eventData.pressPosition.x)
                Previous();
            else
                Next();
        }
        else
        {
            MovePage();
        }
        UpdateButtonStates();
    }

    void UpdateBar()
    {
        if (barOptions == null || barOptions.Length == 0) return;

        foreach (var option in barOptions)
        {
            if (option.barImages.Count != option.barClosed.Count || option.barImages.Count != option.barOpen.Count)
            {
                Debug.LogWarning("Mismatch in BarOption: Ensure barImages, barClosed, and barOpen have the same count.");
                continue;
            }

            for (int i = 0; i < option.barImages.Count; i++)
            {
                option.barImages[i].sprite = option.barClosed[i];
            }
        }

        int index = Mathf.Clamp(currentPage - 1, 0, barOptions.Length - 1);

        if (index < barOptions.Length)
        {
            BarOption selectedOption = barOptions[index];

            for (int i = 0; i < selectedOption.barImages.Count; i++)
            {
                selectedOption.barImages[i].sprite = selectedOption.barOpen[i];
            }
        }
    }

    void UpdateCanvasGroups()
    {
        for (int i = 0; i < pageCanvasGroups.Count; i++)
        {
            pageCanvasGroups[i].interactable = (i == currentPage - 1);
            pageCanvasGroups[i].blocksRaycasts = (i == currentPage - 1);
        }
    }

    void UpdateButtonStates(int predictedPage = -1)
    {
        if (predictedPage == -1) predictedPage = currentPage;

        previousBtn.interactable = predictedPage > 1;
        nextBtn.interactable = predictedPage < maxPage;

        if (!preventSelection)
        {
            if (predictedPage == 1)
            {
                EventSystem.current.SetSelectedGameObject(nextBtn.gameObject);
            }
            else if (predictedPage == maxPage)
            {
                EventSystem.current.SetSelectedGameObject(previousBtn.gameObject);
            }
        }
    }

    public void NavigateFromCurrent(string direction)
    {
        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;

        var key = (currentPage, current, direction.ToLower());
        if (navigationMap.TryGetValue(key, out var target))
        {
            DisableNavigationEventsNextFrame();
            EventSystem.current.SetSelectedGameObject(target);
        }
        else
        {
            Debug.Log($"No navigation rule from '{current.name}' with direction '{direction}' on page {currentPage}");
        }
    }

    private void DisableNavigationEventsNextFrame()
    {
        StartCoroutine(DisableEventsCoroutine());
    }

    private IEnumerator DisableEventsCoroutine()
    {
        EventSystem.current.sendNavigationEvents = false;
        yield return null;
        yield return null; // Deactivate for two frames
        EventSystem.current.sendNavigationEvents = true;
    }
}