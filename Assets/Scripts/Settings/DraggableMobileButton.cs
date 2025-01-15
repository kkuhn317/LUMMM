using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableMobileButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string buttonID;
    private Vector2 defaultPosition;
    private float defaultScale;

    private Vector2 mousePosOffset;

    // Start is called before the first frame update
    void Start()
    {
        defaultPosition = transform.position;
        defaultScale = transform.localScale.x;

        print("Default Position of " + buttonID + ": " + defaultPosition);
        print("Default Scale of " + buttonID + ": " + defaultScale);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        mousePosOffset = (Vector2)transform.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position + mousePosOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

}
