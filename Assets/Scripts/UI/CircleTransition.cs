using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CircleTransition : MonoBehaviour
{
    public Transform player;

    private Canvas _canvas;
    private Image _blackScreen;

    private Vector2 _playerCanvasPos;

    private readonly float duration = 4f;   // How long the transition should take

    private static readonly int RADIUS = Shader.PropertyToID("_Radius");
    private static readonly int CENTER_X = Shader.PropertyToID("_CenterX");
    private static readonly int CENTER_Y = Shader.PropertyToID("_CenterY");

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _blackScreen = GetComponentInChildren<Image>();
    }

    private void Start()
    {
        OpenBlackScreen();
    }

    private void Update()
    {
        DrawBlackScreen();
    }

    public void OpenBlackScreen()
    {
        DrawBlackScreen();
        StartCoroutine(Transition(0, 2));
    }

    public void CloseBlackScreen()
    {
        DrawBlackScreen();
        StartCoroutine(Transition(2, 0));
    }

    private void DrawBlackScreen()
    {
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        // Need a target
        if (player == null)
        {
            return;
        }
        var playerScreenPos = Camera.main.WorldToScreenPoint(player.position);

        // To Draw to Image to Full Screen, we get the Canvas Rect size
        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var canvasWidth = canvasRect.width + 100;
        var canvasHeight = canvasRect.height + 100;

        // But because the Black Screen is now square (different to Screen). So we much added the different of width/height to it
        // Now we convert Screen Pos to Canvas Pos
        _playerCanvasPos = new Vector2
        {
            x = (playerScreenPos.x / screenWidth) * canvasWidth,
            y = (playerScreenPos.y / screenHeight) * canvasHeight,
        };

        var squareValue = 0f;
        if (canvasWidth > canvasHeight)
        {
            // Landscape
            squareValue = canvasWidth;
            _playerCanvasPos.y += (canvasWidth - canvasHeight) * 0.5f;
        }
        else
        {
            // Portrait            
            squareValue = canvasHeight;
            _playerCanvasPos.x += (canvasHeight - canvasWidth) * 0.5f;
        }

        _playerCanvasPos /= squareValue;

        var mat = _blackScreen.material;
        mat.SetFloat(CENTER_X, _playerCanvasPos.x);
        mat.SetFloat(CENTER_Y, _playerCanvasPos.y);

        _blackScreen.rectTransform.sizeDelta = new Vector2(squareValue, squareValue);

        // Now we want the circle to follow the player position
        // So First, we must get the player world position, convert it to screen position, and normalize it (0 -> 1)
        // And input into the shader
    }

    private IEnumerator Transition(float beginRadius, float endRadius)
    {
        var mat = _blackScreen.material;
        var time = 0f;
        while (time <= duration)
        {
            time += Time.deltaTime;
            var t = time / duration;
            var radius = Mathf.Lerp(beginRadius, endRadius, t);

            mat.SetFloat(RADIUS, radius);

            yield return null;
        }
    }
}