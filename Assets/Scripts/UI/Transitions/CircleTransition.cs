using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CircleTransition : MonoBehaviour
{
    //public Transform player;

    private Canvas _canvas;
    private Image _blackScreen;

    private Vector2 _playerCanvasPos;

    [Header("Transition Settings")]
    [SerializeField] private float normalDuration = 4f;
    [SerializeField] private float normalMaxSize = 2f;
    
    [Header("Darkness Mode Settings")]
    [SerializeField] private float darknessDuration = 0.5f;
    [SerializeField] private float darknessMaxSize = 0.1f;

    private float currentDuration;
    private float currentMaxSize;

    private static readonly int RADIUS = Shader.PropertyToID("_Radius");
    private static readonly int CENTER_X = Shader.PropertyToID("_CenterX");
    private static readonly int CENTER_Y = Shader.PropertyToID("_CenterY");

    private PlayerRegistry playerRegistry;
    
    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _blackScreen = GetComponentInChildren<Image>();
    }

    private void Start()
    {
        CacheRegistry();

        // Set parameters based on darkness cheat
        currentDuration = GlobalVariables.cheatDarkness ? darknessDuration : normalDuration;
        currentMaxSize = GlobalVariables.cheatDarkness ? darknessMaxSize : normalMaxSize;
        
        OpenBlackScreen();
    }

    private void CacheRegistry()
    {
        if (GameManagerRefactored.Instance != null)
            playerRegistry = GameManagerRefactored.Instance.GetSystem<PlayerRegistry>();

        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>(true);
    }

    private void Update()
    {
        DrawBlackScreen();
    }

    /// <summary>
    /// Called by CheatController when darkness cheat is toggled mid-level.
    /// </summary>
    public void SetDarknessMode(bool enabled)
    {
        currentDuration = enabled ? darknessDuration : normalDuration;
        currentMaxSize = enabled ? darknessMaxSize : normalMaxSize;
        
        // If the black screen is currently active, update its target size
        if (_blackScreen != null && _blackScreen.gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(Transition(0, currentMaxSize));
        }
    }

    public void OpenBlackScreen()
    {
        _blackScreen.gameObject.SetActive(true);
        DrawBlackScreen();
        StartCoroutine(Transition(0, currentMaxSize));
    }

    public void CloseBlackScreen()
    {
        DrawBlackScreen();
        StartCoroutine(Transition(currentMaxSize, 0));
    }

    private void DrawBlackScreen()
    {
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;

        // MarioMovement playerscript = GameManager.Instance.GetPlayer(0);
        MarioMovement playerscript = playerRegistry != null ? playerRegistry.GetPlayer(0) : null;
        if (playerscript == null)
            return;
            
        Transform player = playerscript.transform;
        var playerScreenPos = Camera.main.WorldToScreenPoint(player.position);

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var canvasWidth = canvasRect.width + 100;
        var canvasHeight = canvasRect.height + 100;

        _playerCanvasPos = new Vector2
        {
            x = (playerScreenPos.x / screenWidth) * canvasWidth,
            y = (playerScreenPos.y / screenHeight) * canvasHeight,
        };

        var squareValue = 0f;
        if (canvasWidth > canvasHeight)
        {
            squareValue = canvasWidth;
            _playerCanvasPos.y += (canvasWidth - canvasHeight) * 0.5f;
        }
        else
        {
            squareValue = canvasHeight;
            _playerCanvasPos.x += (canvasHeight - canvasWidth) * 0.5f;
        }

        _playerCanvasPos /= squareValue;

        var mat = _blackScreen.material;
        mat.SetFloat(CENTER_X, _playerCanvasPos.x);
        mat.SetFloat(CENTER_Y, _playerCanvasPos.y);

        _blackScreen.rectTransform.sizeDelta = new Vector2(squareValue, squareValue);
    }

    private IEnumerator Transition(float beginRadius, float endRadius)
    {
        var mat = _blackScreen.material;
        var time = 0f;
        
        while (time <= currentDuration)
        {
            time += Time.deltaTime;
            var t = time / currentDuration;
            var radius = Mathf.Lerp(beginRadius, endRadius, t);
            mat.SetFloat(RADIUS, radius);
            yield return null;
        }

        mat.SetFloat(RADIUS, endRadius);

        if (endRadius >= currentMaxSize)
        {
            _blackScreen.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        var mat = _blackScreen.material;
        mat.SetFloat(RADIUS, 1f);
        mat.SetFloat(CENTER_X, 0.5f);
        mat.SetFloat(CENTER_Y, 0.5f);
    }
}