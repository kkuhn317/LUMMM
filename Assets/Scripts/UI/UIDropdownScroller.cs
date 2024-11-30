using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDropdownScroller : MonoBehaviour, ISelectHandler
{
    private ScrollRect scrollRect;
    private float scrollPosition = 1;

    // Start is called before the first frame update
    void Start()
    {
        scrollRect = GetComponentInParent<ScrollRect>(true);

        int childCount = scrollRect.content.transform.childCount - 1;
        int childIndex = transform.GetSiblingIndex();

        childIndex = Mathf.Clamp(childIndex, 0, childCount - 1);

        scrollPosition = 1 - ((float)childIndex / childCount);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // If this is caused by the mouse, don't scroll
        // Note: this solution doesnt work... find some other way
        if (eventData is PointerEventData)
        {
            return;
        }


        if (scrollRect)
            scrollRect.verticalScrollbar.value = scrollPosition;
    }
}
