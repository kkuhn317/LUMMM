using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;

public class DraggableMobileButton : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public string buttonID;
    private Vector2 defaultPosition;
    private float defaultScale;
    private Vector2 mousePosOffset;
    private Image image;
    private RectTransform rectTransform;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();

        MobileControlRebinding.instance.onResetPressed.AddListener(ResetButton);
        MobileControlRebinding.instance.AddButton(this);
    }

    void OnEnable() {
        // Check if rectTransform is not set yet so that this is only done one time
        if (!rectTransform) {
            rectTransform = GetComponent<RectTransform>();
            defaultPosition = rectTransform.anchoredPosition;
            defaultScale = transform.localScale.x;

            print("Default Position of " + buttonID + ": " + defaultPosition);
            print("Default Scale of " + buttonID + ": " + defaultScale);
        }

        // Load saved position and scale
        MobileRebindingData mobileData = GlobalVariables.currentLayout.mobileData;
        if (mobileData.buttonData.ContainsKey(buttonID))
        {
            print("Loading saved position of button");
            rectTransform.anchoredPosition = mobileData.buttonData[buttonID].position;
            float scale = mobileData.buttonData[buttonID].scale;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public MobileRebindingData.MobileButtonData GetData() {
        return new MobileRebindingData.MobileButtonData {
            position = rectTransform.anchoredPosition,
            scale = transform.localScale.x
        };
    }   

    public void OnPointerDown(PointerEventData eventData)
    {
        mousePosOffset = (Vector2)transform.position - eventData.position;
        MobileControlRebinding.instance.SetSelectedButton(this);
        // Make more red
        image.color = new Color(1f, 0.5f, 0.5f, 1f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position + mousePosOffset;
    }

    public void UnselectButton()
    {
        image.color = new Color(1f, 1f, 1f, 1f);
    }

    public void SetScale(float scale) {
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void ResetButton()
    {
        rectTransform.anchoredPosition = defaultPosition;
        transform.localScale = new Vector3(defaultScale, defaultScale, 1f);
    }
}
