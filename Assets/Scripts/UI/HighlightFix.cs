using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(Selectable))]
public class HighlightFix : MonoBehaviour, IPointerEnterHandler, IDeselectHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!EventSystem.current.alreadySelecting)
            EventSystem.current.SetSelectedGameObject(this.gameObject);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        this.GetComponent<Selectable>().OnPointerExit(null);
    }
}