using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSfx : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
{
    [Header("SFX")]
    [SerializeField] private AudioClip selectSfx;
    [SerializeField] private AudioClip pressSfx;

    public void OnSelect(BaseEventData eventData)
    {
        // While a dropdown is expanded, we don't want global select ticks
        if (UISfxGate.IsSelectSfxSuppressed)
            return;

        // Also ignore the next select if it was set by code (menu open / reselect)
        if (UISfxGate.ConsumeSuppressNextSelectSfx())
            return;

        if (selectSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(selectSfx, SoundCategory.SFX);
    }

    // Gamepad/Keyboard submit
    public void OnSubmit(BaseEventData eventData) => PlayPress();

    // Mouse click / touch
    public void OnPointerClick(PointerEventData eventData) => PlayPress();

    private void PlayPress()
    {
        if (pressSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(pressSfx, SoundCategory.SFX);
    }
}