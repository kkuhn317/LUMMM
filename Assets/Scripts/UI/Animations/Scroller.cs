using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scroller : MonoBehaviour
{
    [SerializeField] private RawImage _img;
    [SerializeField] private float _x, _y;
    [SerializeField] private List<Color> _colors;
    [SerializeField] private float _colorChangeDuration = 1f;

    private int _currentColorIndex;
    private float _colorChangeTimer;
    private Rect _rect;
    private float _invColorDuration;

    void Start()
    {
        var c = _img.color; c.a = 1f; _img.color = c;

        _rect = _img.uvRect;
        _invColorDuration = 1f / _colorChangeDuration;
    }

    void Update()
    {
        // UV scroll
        _rect.x += _x * Time.unscaledDeltaTime;
        _rect.y += _y * Time.unscaledDeltaTime;
        _img.uvRect = _rect;

        // Color interpolation
        if (_colors.Count > 1)
        {
            _colorChangeTimer += Time.unscaledDeltaTime;
            float t = _colorChangeTimer * _invColorDuration;

            _img.color = Color.Lerp(
                _colors[_currentColorIndex],
                _colors[(_currentColorIndex + 1) % _colors.Count],
                t
            );

            if (t >= 1f)
            {
                _colorChangeTimer = 0f;
                _currentColorIndex = (_currentColorIndex + 1) % _colors.Count;
            }
        }
    }
}