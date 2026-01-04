using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Selectable))]
public class ButtonDoubleSubmitListener : MonoBehaviour,
    ISubmitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Timing")]
    [Tooltip("Max time between two Submit presses to count as a double-submit.")]
    [Range(0.05f, 1.0f)]
    [SerializeField] private float doubleSubmitWindow = 0.4f;

    [Header("Rules")]
    [Tooltip("If true, only triggers when the Selectable is interactable.")]
    [SerializeField] private bool requireInteractable = true;

    [Tooltip("If true, reset the first press if this object loses selection.")]
    [SerializeField] private bool resetOnDeselect = true;

    [Header("Events")]
    public UnityEvent onFirstSubmit;
    public UnityEvent onDoubleSubmit;

    private Selectable selectable;
    private float lastSubmitTime = -999f;
    private bool armed;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ResetState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (resetOnDeselect)
            ResetState();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (requireInteractable && (selectable == null || !selectable.IsInteractable()))
            return;

        float now = Time.unscaledTime;

        // First press arms, second press confirms if within window
        if (!armed || (now - lastSubmitTime) > doubleSubmitWindow)
        {
            armed = true;
            lastSubmitTime = now;
            onFirstSubmit?.Invoke();
            return;
        }

        // Double submit confirmed
        ResetState();
        onDoubleSubmit?.Invoke();
    }

    private void ResetState()
    {
        armed = false;
        lastSubmitTime = -999f;
    }
}