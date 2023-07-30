using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scroller : MonoBehaviour
{
    [SerializeField] private RawImage _img;
    [SerializeField] private float _x, _y;
    [SerializeField] private List<Color> _colors;
    [SerializeField] private float _colorChangeDuration;

    private int _currentColorIndex = 0;
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
        //Update the UV rect
        _img.uvRect = new Rect(_img.uvRect.position + new Vector2(_x, _y) * Time.unscaledDeltaTime, _img.uvRect.size);

        //Update the color
        if (_colors.Count > 1) // Check that there are at least 2 colors available
        {
            _colorChangeTimer += Time.deltaTime;
            float t = _colorChangeTimer / _colorChangeDuration;
            _img.color = Color.Lerp(_colors[_currentColorIndex], _colors[(_currentColorIndex + 1) % _colors.Count], t);
            if (t >= 1f)
            {
                //Reset the timer and move to the next color
                _colorChangeTimer = 0f;
                _currentColorIndex = (_currentColorIndex + 1) % _colors.Count;
            }
        }
    }
}
