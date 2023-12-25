using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSelection : MonoBehaviour
{
    [SerializeField] RectTransform[] menuBtn;
    [SerializeField] RectTransform indicator;

    [SerializeField] Vector2 indicatorOffset;

    int lastSelected = -1;

    bool firstFrame = true;

    void LateUpdate() {
        if (firstFrame) {
            firstFrame = false;
        }
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
        if (firstFrame) {
            StartCoroutine(MoveIndicatorLaterCoroutine(b));
            return;
        }

        print(menuBtn[b].position);
        if (b < 0 || b >= menuBtn.Length)
        {
            // make cursor invisible
            indicator.gameObject.SetActive(false);
            return;
        }
        indicator.gameObject.SetActive(true);
        indicator.position = menuBtn[b].position + (Vector3)indicatorOffset;
    }

    // Fix for weird issue where the button position is wrong on the first frame
    IEnumerator MoveIndicatorLaterCoroutine(int b)
    {
        yield return null;
        MoveIndicator(b);
    }

}
