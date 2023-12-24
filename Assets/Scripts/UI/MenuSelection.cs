using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSelection : MonoBehaviour
{
    [SerializeField] RectTransform[] menuBtn;
    [SerializeField] RectTransform indicator;
    [SerializeField] float moveDelay;

    int indicatorPos;
    float moveTimer;

    // Update is called once per frame
    void Update()
    {
        if (moveTimer < moveDelay)
        {
            moveTimer += Time.deltaTime;
        }

        // Handle keyboard input
        if (Input.GetKey(KeyCode.DownArrow))
        {
            MoveIndicator(1);
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            MoveIndicator(-1);
        }

        // Handle mouse input
        if (Input.GetMouseButton(0))
        {
            UpdateIndicatorWithMouse();
        }

        if (indicatorPos >= 0 && indicatorPos < menuBtn.Length)
        {
            indicator.localPosition = menuBtn[indicatorPos].localPosition;
        }
    }

    // Move the indicator based on keyboard input or mouse input
    void MoveIndicator(int direction)
    {
        if (moveTimer >= moveDelay)
        {
            indicatorPos = (indicatorPos + direction + menuBtn.Length) % menuBtn.Length;
            moveTimer = 0;
        }
    }

    // Update the indicator position based on mouse input
    void UpdateIndicatorWithMouse()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 localPos = transform.InverseTransformPoint(mousePos);

        int closestButtonIndex = -1;
        float closestButtonDistance = float.MaxValue;
        for (int i = 0; i < menuBtn.Length; i++)
        {
            Vector3 buttonWorldPos = menuBtn[i].TransformPoint(menuBtn[i].localPosition);
            float distance = Vector2.Distance(localPos, transform.InverseTransformPoint(buttonWorldPos));
            if (distance < closestButtonDistance)
            {
                closestButtonIndex = i;
                closestButtonDistance = distance;
            }
        }
        if (closestButtonIndex >= 0 && closestButtonIndex < menuBtn.Length)
        {
            indicatorPos = closestButtonIndex;
        }
    }

    public void MoveOnButton(int btnPos)
    {
        indicatorPos = btnPos;
    }
}
