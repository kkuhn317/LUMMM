using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RainbowImage : MonoBehaviour
{
    [SerializeField] private Image _img;
    [SerializeField] private float cycleDuration;
    
    private float _colorChangeTimer;

    void Start()
    {
        //Set the alpha value of the image to 1
        Color imgColor = _img.color;
        imgColor.a = 1f;
        _img.color = imgColor;
    }

    void Update()
    {
        // Use HSV color space to cycle through colors
        _colorChangeTimer += Time.deltaTime;
        float hue = (_colorChangeTimer / cycleDuration) % 1f; // Cycle hue over the duration
        Color newColor = Color.HSVToRGB(hue, 1f, 1f);
        newColor.a = _img.color.a; // Preserve original alpha
        _img.color = newColor;
    }
}
