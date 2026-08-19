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
    private bool darknessMode;
    private Coroutine transitionCoroutine;

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

        Debug.Log($"[CircleTransition] Start called. PlayerRegistry: {(playerRegistry != null ? playerRegistry.name : "null")}");

        // CheatFlags is the authoritative state and is already populated before
        // scene objects run Start, regardless of CheatController execution order.
        ApplyModeSettings(CheatFlags.Darkness);

        // Skip if scene was loaded via FadeInOutScene — it handles the transition
        if (FadeInOutScene.LoadedWithFade && !darknessMode)
        {
            _blackScreen.gameObject.SetActive(false);
            return;
        }

        OpenBlackScreen();
    }

    private void CacheRegistry()
    {
        if (GameManager.Instance != null)
            playerRegistry = GameManager.Instance.GetSystem<PlayerRegistry>();

        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>(true);
    }

    private void LateUpdate()
    {
        // Midnight is a persistent spotlight and must follow Mario. Normal
        // transitions snapshot their center in Open/CloseBlackScreen instead,
        // so Mario moving during the animation cannot drag the circle around.
        if (darknessMode)
            DrawBlackScreen();
    }

    /// <summary>
    /// Called by CheatController when darkness cheat is toggled mid-level.
    /// </summary>
    public void SetDarknessMode(bool enabled)
    {
        ApplyModeSettings(enabled);

        if (_blackScreen == null) return;

        // A completed normal reveal disables the image. Reactivate it when
        // midnight is enabled so the darkness mask can be drawn again.
        _blackScreen.gameObject.SetActive(true);
        DrawBlackScreen();

        float beginRadius = _blackScreen.material.GetFloat(RADIUS);
        StartTransition(beginRadius, currentMaxSize);
    }

    private void ApplyModeSettings(bool enabled)
    {
        darknessMode = enabled;
        currentDuration = enabled ? darknessDuration : normalDuration;
        currentMaxSize = enabled ? darknessMaxSize : normalMaxSize;
    }

    public void OpenBlackScreen()
    {
        _blackScreen.gameObject.SetActive(true);
        // In normal mode this is the one-time starting-position snapshot.
        DrawBlackScreen();
        StartTransition(0, currentMaxSize);
    }

    public void CloseBlackScreen()
    {
        // Closing transitions snapshot Mario's position when closing begins.
        DrawBlackScreen();
        StartTransition(_blackScreen.material.GetFloat(RADIUS), 0);
    }

    private void StartTransition(float beginRadius, float endRadius)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(Transition(beginRadius, endRadius));
    }

    private void DrawBlackScreen()
    {
        MarioCore playerscript = playerRegistry != null ? playerRegistry.GetPlayer(0) : null;
        if (playerscript == null)
            return;

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        float squareValue = Mathf.Max(canvasRect.width, canvasRect.height) + 100f;

        RectTransform blackScreenRect = _blackScreen.rectTransform;
        blackScreenRect.sizeDelta = new Vector2(squareValue, squareValue);

        // Use Mario's physical body center so differently sized powerup forms
        // remain visually centered. Fall back to the prefab root if necessary.
        Vector3 playerWorldPos = playerscript.Collider != null
            ? playerscript.Collider.bounds.center
            : playerscript.transform.position;
        Vector2 playerScreenPos = worldCamera.WorldToScreenPoint(playerWorldPos);

        Camera canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                blackScreenRect, playerScreenPos, canvasCamera, out Vector2 localPoint))
        {
            return;
        }

        // The shader consumes the Image's 0..1 UV coordinates. Converting from
        // its actual RectTransform automatically accounts for CanvasScaler,
        // aspect ratio, pivot, and the square's centered overscan margins.
        Rect imageRect = blackScreenRect.rect;
        _playerCanvasPos = new Vector2(
            Mathf.InverseLerp(imageRect.xMin, imageRect.xMax, localPoint.x),
            Mathf.InverseLerp(imageRect.yMin, imageRect.yMax, localPoint.y));

        var mat = _blackScreen.material;
        mat.SetFloat(CENTER_X, _playerCanvasPos.x);
        mat.SetFloat(CENTER_Y, _playerCanvasPos.y);
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
        transitionCoroutine = null;

        // Normal mode removes the transition canvas after revealing the level.
        // Midnight mode deliberately leaves it active as the gameplay mask.
        if (!darknessMode && endRadius >= currentMaxSize)
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
