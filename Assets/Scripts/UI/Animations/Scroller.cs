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
        if (_img == null)
        {
            Debug.LogError("Scroller: RawImage is not assigned.", this);
            enabled = false;
            return;
        }

        _colorChangeDuration = Mathf.Max(0.01f, _colorChangeDuration);
        _rect = _img.uvRect;
        _invColorDuration = 1f / _colorChangeDuration;
    }

    void Update()
    {
        _rect.x = Mathf.Repeat(_rect.x + _x * Time.unscaledDeltaTime, 1f);
        _rect.y = Mathf.Repeat(_rect.y + _y * Time.unscaledDeltaTime, 1f);
        _img.uvRect = _rect;

        if (_colors != null && _colors.Count > 1)
        {
            _colorChangeTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_colorChangeTimer * _invColorDuration);

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