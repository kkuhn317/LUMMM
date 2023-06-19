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

    //Update is called once per frame
    void Update()
    {
        if (moveTimer < moveDelay)
        {
            moveTimer += Time.deltaTime;
        }

        //Handle keyboard input
        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (moveTimer >= moveDelay)
            {
                if (indicatorPos < menuBtn.Length - 1)
                {
                    indicatorPos++;
                }
                else
                {
                    indicatorPos = 0;
                }
                moveTimer = 0;
            }
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            if (moveTimer >= moveDelay)
            {
                if (indicatorPos > 0)
                {
                    indicatorPos--;
                }
                else
                {
                    indicatorPos = menuBtn.Length - 1;
                }
                moveTimer = 0;
            }
        }

        //Handle mouse input
        if (Input.GetMouseButton(0))
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

        if (indicatorPos >= 0 && indicatorPos < menuBtn.Length)
        {
            indicator.localPosition = menuBtn[indicatorPos].localPosition;
        }
    }

    public void MoveOnButton(int btnPos)
    {
        indicatorPos = btnPos;
    }
}
