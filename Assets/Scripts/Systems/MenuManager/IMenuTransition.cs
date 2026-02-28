/// <summary>
/// Implement this interface on any MonoBehaviour attached to a menu panel
/// to provide custom show/hide transition behaviour.
/// GUIManager will call these instead of SetActive directly.
/// Examples: AnimatorMenuTransition, TweenMenuTransition, etc.
/// </summary>
public interface IMenuTransition
{
    /// <summary>Called when the panel is about to become visible. Invoke onComplete when the transition finishes.</summary>
    void OnShow(System.Action onComplete);

    /// <summary>Called when the panel is about to be hidden. Invoke onComplete when the transition finishes (GUIManager will then call SetActive(false)).</summary>
    void OnHide(System.Action onComplete);

    /// <summary>
    /// Optional: immediately snap the transition to its end state and invoke the pending onComplete.
    /// Called by GUIManager when a new navigation request arrives mid-transition.
    /// If your transition cannot be interrupted, leave this unimplemented — the default is a no-op.
    /// </summary>
    void Interrupt() { }
}