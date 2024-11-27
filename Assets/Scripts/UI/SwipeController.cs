using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwipeController : MonoBehaviour, IEndDragHandler
{
    [SerializeField] int maxPage;
    int currentPage;
    Vector3 targetPos;
    [SerializeField] Vector3 pageStep;
    [SerializeField] RectTransform levelPagesRect;

    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;
    float dragThereshould;
    [SerializeField] Button previousBtn, nextBtn;

    [System.Serializable]
    public class BarOption
    {
        public Image barImage;
        public Sprite barClosed, barOpen;
    }
    [SerializeField] BarOption[] barOptions;

    [SerializeField] List<CanvasGroup> pageCanvasGroups; // Add CanvasGroups for each page

    private void Awake()
    {
        currentPage = 1;
        targetPos = levelPagesRect.localPosition;
        dragThereshould = Screen.width / 15;
        UpdateBar();
        UpdateArrowButton();
        UpdateCanvasGroups();
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
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
    }

    void MovePage()
    {
        levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
        UpdateBar();
        UpdateArrowButton();
        UpdateCanvasGroups();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThereshould)
        {
            if (eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();
        }
        else
        {
            MovePage();
        }
    }

    void UpdateBar()
    {
        foreach (var option in barOptions)
        {
            option.barImage.sprite = option.barClosed;
        }
        barOptions[currentPage - 1].barImage.sprite = barOptions[currentPage - 1].barOpen;
    }

    void UpdateArrowButton()
    {
        nextBtn.interactable = true;
        previousBtn.interactable = true;
        if (currentPage == 1) previousBtn.interactable = false;
        else if (currentPage == maxPage) nextBtn.interactable = false;
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
}