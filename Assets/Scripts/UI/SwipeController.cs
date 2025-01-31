using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwipeController : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] int maxPage;
    int currentPage;
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
        public List<Image> barImages; // Multiple bar icons
        public List<Sprite> barClosed; // Each icon has a closed state
        public List<Sprite> barOpen;   // Each icon has an open state
    }
    [SerializeField] BarOption[] barOptions;

    [SerializeField] List<CanvasGroup> pageCanvasGroups; // Add CanvasGroups for each page
    private Vector3 initialPosition;

    private void Awake()
    {
        currentPage = 1;
        initialPosition = levelPagesRect.localPosition;
        targetPos = levelPagesRect.localPosition;
        dragThreshold = Screen.width / 15;
        UpdateBar();
        UpdateCanvasGroups();
        UpdateButtonStates(); // Ensure buttons are updated on initialization
        LeanTween.reset(); // https://github.com/dentedpixel/LeanTween/issues/88
    }

    public void Next()
    {
        if (currentPage < maxPage)
        {
            currentPage++;
            targetPos += pageStep;
            MovePage();
        }
        UpdateButtonStates();  // Update button states after navigation
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
        UpdateButtonStates();  // Update button states after navigation
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
}
