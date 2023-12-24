using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSelection : MonoBehaviour
{
    [SerializeField] RectTransform[] menuBtn;
    [SerializeField] RectTransform indicator;

    [SerializeField] Vector2 indicatorOffset;

    int lastSelected = -1;

    void Start() {
        //EventSystem.current.SetSelectedGameObject(menuBtn[0].gameObject);
        // indicator.position = menuBtn[0].position + (Vector3)indicatorOffset;
    }

    public void PointerEnter(int b)
    {
        //print("enter: " + b);
        MoveIndicator(b);
    }

    public void PointerExit(int b)
    {
        //print("exit: " + b);
        MoveIndicator(lastSelected);
    }

    public void ButtonSelected(int b)
    {
        //print("selected: " + b);
        lastSelected = b;
        MoveIndicator(b);
    }

    // Move the indicator based on keyboard input or mouse input
    public void MoveIndicator(int b)
    {
        if (b < 0 || b >= menuBtn.Length)
        {
            // make cursor invisible
            indicator.gameObject.SetActive(false);
            return;
        }
        indicator.gameObject.SetActive(true);
        indicator.position = menuBtn[b].position + (Vector3)indicatorOffset;
    }
}
