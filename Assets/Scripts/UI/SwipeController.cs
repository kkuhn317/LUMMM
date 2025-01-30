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
        public Image barImage;
        public Sprite barClosed, barOpen;
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
        UpdateButtonStates(); // Update button states after navigation
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
        UpdateButtonStates(); // Update button states after navigation
    }

    public void GoToPage(int targetPage) // Start from 1
    {
        if (targetPage == currentPage) return; 
        
        if (targetPage < 1 || targetPage > maxPage)
        {
            Debug.LogWarning($"Invalid page number: {targetPage}. Must be between 1 and {maxPage}.");
            return;
        }

        // Update the current page
        currentPage = targetPage;

        // Calculate the absolute position of the target page
        targetPos = initialPosition + (targetPage - 1) * pageStep;

        Debug.Log($"GoToPage: TargetPage = {targetPage}, TargetPos = {targetPos}, InitialPos = {initialPosition}, PageStep = {pageStep}");

        // Move to the new page
        MovePage();
    }

        // Add a variable to control selection behavior
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
        UpdateButtonStates(); // Ensure buttons are updated after page change
    }

    public void OnDrag(PointerEventData eventData)
    {
        float dragAmount = eventData.position.x - eventData.pressPosition.x;

        // Predict the next page based on drag distance
        int predictedPage = currentPage;

        if (Mathf.Abs(dragAmount) > dragThreshold)
        {
            predictedPage = (dragAmount > 0) ? Mathf.Max(1, currentPage - 1) : Mathf.Min(maxPage, currentPage + 1);
        }

        // Only update button states if the page prediction changes
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
        UpdateButtonStates(); // Ensure buttons are updated after dragging
    }

    void UpdateBar()
    {
        if (barOptions == null || barOptions.Length == 0 || currentPage - 1 >= barOptions.Length) return;

        foreach (var option in barOptions)
        {
            option.barImage.sprite = option.barClosed;
        }
        barOptions[currentPage - 1].barImage.sprite = barOptions[currentPage - 1].barOpen;
    }

    void UpdateCanvasGroups()
    {
        for (int i = 0; i < pageCanvasGroups.Count; i++)
        {
            if (i == currentPage - 1)
            {
                pageCanvasGroups[i].interactable = true;
                pageCanvasGroups[i].blocksRaycasts = true;
            }
            else
            {
                pageCanvasGroups[i].interactable = false;
                pageCanvasGroups[i].blocksRaycasts = false;
            }
        }
    }

    void UpdateButtonStates(int predictedPage = -1)
    {
        // Log for debugging
        Debug.Log($"Updating button states: currentPage = {currentPage}, maxPage = {maxPage}");

        // Update button interactivity
        if (predictedPage == -1) predictedPage = currentPage;

        previousBtn.interactable = predictedPage > 1;
        nextBtn.interactable = predictedPage < maxPage;

        // Only auto-select if preventSelection is false
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
            /*else
            {
                EventSystem.current.SetSelectedGameObject(nextBtn.gameObject);
            }*/
        }

        // Debugging: Log the currently selected GameObject
        Debug.Log($"Selected Button: {EventSystem.current.currentSelectedGameObject?.name}");
    }
}