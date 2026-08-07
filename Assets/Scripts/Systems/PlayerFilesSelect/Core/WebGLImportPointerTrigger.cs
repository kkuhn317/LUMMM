using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on each save-slot card's clickable child (the same GameObject that
/// carries the Button / Selectable), alongside the existing SaveFileUI wiring.
///
/// Why pointer-down and not the Button's OnClick: OnClick fires on pointer-UP,
/// by which time the browser's mouseup has already been dispatched and there is
/// no pending gesture left to hang the file dialog on. Arming during
/// OnPointerDown means the jslib's document.onmouseup handler is registered
/// before the user releases the button, so input.click() runs inside a genuine
/// user gesture.
///
/// The card's normal OnClick still runs afterwards, but SaveSlotManager's
/// isFileDialogOpen guard in PlayFocusedSlot swallows it — see
/// BeginWebGLImport in INTEGRATION.md.
///
/// Non-WebGL builds compile this to a no-op and keep the existing desktop flow.
/// </summary>
[DisallowMultipleComponent]
public sealed class WebGLImportPointerTrigger : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("The SaveSlotManager driving this file-select screen.")]
    [SerializeField]
    private SaveSlotManager manager;

    [Tooltip("The SaveFileUI card this trigger belongs to. Supplies slotIndex.")]
    [SerializeField]
    private SaveFileUI card;

    private void Awake()
    {
        if (manager == null)
            manager = GetComponentInParent<SaveSlotManager>();

        if (card == null)
            card = GetComponentInParent<SaveFileUI>();

#if UNITY_WEBGL && !UNITY_EDITOR
        if (manager == null || card == null)
        {
            Debug.LogError(
                "WebGLImportPointerTrigger: manager or card is unassigned. " +
                "WebGL import will not work on this slot.",
                this
            );
        }
#endif
    }

    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (manager == null || card == null)
            return;

        // Only hijack the press while the player is actually choosing an import
        // destination. In every other mode this component does nothing, so
        // Delete / Copy / Rename / Play keep their normal behaviour.
        if (manager.CurrentMode != SaveSlotManager.InteractionMode.ImportSelectDestination)
            return;

        manager.BeginWebGLImport(card.slotIndex);
#endif
    }
}
