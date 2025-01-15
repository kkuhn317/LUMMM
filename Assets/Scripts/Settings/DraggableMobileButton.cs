using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableMobileButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public string buttonID;
    private Vector2 defaultPosition;
    private float defaultScale;
    private Vector2 mousePosOffset;
    private Image image;

    // Start is called before the first frame update
    void Start()
    {
        defaultPosition = transform.position;
        defaultScale = transform.localScale.x;

        print("Default Position of " + buttonID + ": " + defaultPosition);
        print("Default Scale of " + buttonID + ": " + defaultScale);

        // Load the position and scale from PlayerPrefs
        if (PlayerPrefs.HasKey(buttonID + SettingsKeys.ButtonPosXKey))
        {
            float x = PlayerPrefs.GetFloat(buttonID + SettingsKeys.ButtonPosXKey);
            float y = PlayerPrefs.GetFloat(buttonID + SettingsKeys.ButtonPosYKey);
            transform.position = new Vector2(x, y);
        }
        if (PlayerPrefs.HasKey(buttonID + SettingsKeys.ButtonScaleKey))
        {
            float scale = PlayerPrefs.GetFloat(buttonID + SettingsKeys.ButtonScaleKey);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        image = GetComponent<Image>();

        MobileControlRebinding.instance.onResetPressed.AddListener(ResetButton);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position + mousePosOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Save the position and scale to PlayerPrefs
        PlayerPrefs.SetFloat(buttonID + SettingsKeys.ButtonPosXKey, transform.position.x);
        PlayerPrefs.SetFloat(buttonID + SettingsKeys.ButtonPosYKey, transform.position.y);
        PlayerPrefs.SetFloat(buttonID + SettingsKeys.ButtonScaleKey, transform.localScale.x);
    }

    public void UnselectButton()
    {
        image.color = new Color(1f, 1f, 1f, 1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mousePosOffset = (Vector2)transform.position - eventData.position;
        MobileControlRebinding.instance.SetSelectedButton(this);
        // Make more red
        image.color = new Color(1f, 0.5f, 0.5f, 1f);
    }

    public void SetScale(float scale) {
        transform.localScale = new Vector3(scale, scale, 1f);
        PlayerPrefs.SetFloat(buttonID + SettingsKeys.ButtonScaleKey, scale);
    }

    public void ResetButton()
    {
        transform.position = defaultPosition;
        transform.localScale = new Vector3(defaultScale, defaultScale, 1f);
        PlayerPrefs.SetFloat(buttonID + SettingsKeys.ButtonPosXKey, defaultPosition.x);
        PlayerPrefs.SetFloat(buttonID + SettingsKeys.ButtonPosYKey, defaultPosition.y);
        PlayerPrefs.SetFloat(buttonID + SettingsKeys.ButtonScaleKey, defaultScale);
    }
}
