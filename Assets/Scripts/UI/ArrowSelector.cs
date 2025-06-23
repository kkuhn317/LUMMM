using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ArrowSelector : MonoBehaviour
{
    [System.Serializable]
    public struct ButtonData
    {
        public RectTransform button;
        public Vector2 arrowOffset;
    }

    public bool showDebugLinesOnlyOnActiveObjects = true;
    [SerializeField] private AudioClip navigateSound;
    [SerializeField] ButtonData[] buttons;
    [SerializeField] RectTransform arrowIndicator;
    [HideInInspector] public bool isSelectingOption = false;

    [HideInInspector] public int lastSelected = -1;
    bool firstFrame = true;
    private bool isChangingPage = false;
    private bool suppressFirstSound = true;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        StartCoroutine(SuppressFirstAutoSelect());
    }

    IEnumerator SuppressFirstAutoSelect()
    {
        yield return null;

        GameObject selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (selected != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].button != null && buttons[i].button.gameObject == selected)
                {
                    lastSelected = i;
                    suppressFirstSound = true;
                    MoveIndicator(i);
                    yield return null;
                    suppressFirstSound = false;
                    yield break;
                }
            }
        }

        suppressFirstSound = false;
    }

    void LateUpdate()
    {
        if (firstFrame)
        {
            firstFrame = false;
        }

        if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null || isSelectingOption)
        {
            arrowIndicator.gameObject.SetActive(false);
        }
    }

    public void PointerEnter(int b)
    {
        // MoveIndicator(b);
    }

    public void PointerExit(int b)
    {
        // MoveIndicator(lastSelected);
    }

    public void ButtonSelected(int b)
    {
        lastSelected = b;
        MoveIndicator(b);
        Debug.Log($"suppressFirstSound: {suppressFirstSound}");

        // This is to avoid playing the audio when the scene loads, which it's the first time it is automatically selected
        if (suppressFirstSound)
        {
            suppressFirstSound = false;
            Debug.Log("First sound suppressed");
            return;
        }

        if (!isChangingPage && navigateSound != null)
        {
            
            audioSource.PlayOneShot(navigateSound);
            Debug.Log("Audio plays, you're not changing pages or it's not the first time time scene has loaded");
        }
        else
        {
            Debug.LogWarning("Either a navigateSound wasn't referenced or the AudioManager isn't on the scene");
        }
    }

    public void MoveIndicator(int b)
    {
        if (isSelectingOption || firstFrame)
        {
            StartCoroutine(MoveIndicatorLaterCoroutine(b));
            return;
        }

        if (b < 0 || b >= buttons.Length || buttons[b].button == null)
        {
            arrowIndicator.gameObject.SetActive(false);
            return;
        }

        // Delay arrow update by one frame to ensure layout is correct
        StartCoroutine(DelayedUpdatePosition(b));
    }

    IEnumerator DelayedUpdatePosition(int b)
    {
        // Wait two frames to ensure resolution and layout are fully updated after screen/border changes
        yield return null;
        yield return null; // a little robust but whatever it works

        // Forzamos reconstrucción de layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttons[b].button);

        // Doble verificación
        yield return null;

        Vector3 worldPos = buttons[b].button.TransformPoint((Vector3)buttons[b].arrowOffset);
        arrowIndicator.position = worldPos;
        arrowIndicator.gameObject.SetActive(true);
    }

    IEnumerator MoveIndicatorLaterCoroutine(int b)
    {
        yield return null;
        MoveIndicator(b);
    }

    public void SetChangingPage(bool value)
    {
        isChangingPage = value;
    }

    public void SetSelecting(bool value)
    {
        isSelectingOption = value;
    }

    void OnDrawGizmos()
    {
        if (buttons == null || buttons.Length == 0) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rect = buttons[i].button;
            if (rect == null) continue;
            if (showDebugLinesOnlyOnActiveObjects && !rect.gameObject.activeInHierarchy) continue;

            // button base position (local space converted to world)
            Vector3 buttonWorldPos = rect.TransformPoint(Vector3.zero);

            // local space offset position converted to world
            Vector3 offsetWorldPos = rect.TransformPoint(buttons[i].arrowOffset);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(buttonWorldPos, 1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(offsetWorldPos, 1.25f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(buttonWorldPos, offsetWorldPos);
        }

        if (lastSelected >= 0 && lastSelected < buttons.Length && buttons[lastSelected].button != null)
        {
            RectTransform selectedRect = buttons[lastSelected].button;
            Vector3 selectedWorldPos = selectedRect.TransformPoint(buttons[lastSelected].arrowOffset);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(selectedWorldPos, 1.5f);
        }
    }
}