using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour, IPointerUpHandler
{
    private Button button;
    private Animator animator;
    private float pressAnimationDuration;
    private bool isAnimating = false; // Track if the button animation is currently running

    private void Awake()
    {
        button = GetComponent<Button>();
        animator = GetComponent<Animator>();

        // Get the "Pressed" animation clip length if it exists
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (var clip in clips)
        {
            if (clip.name == "Pressed")  // Replace "Pressed" with the actual name of your press animation
            {
                pressAnimationDuration = clip.length;
                break;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Start the deselect coroutine only if the animation is not already running
        if (!isAnimating)
        {
            isAnimating = true;
            StartCoroutine(DeselectAfterPress());
        }
    }

    private IEnumerator DeselectAfterPress()
    {
        // Wait for the animation to finish playing fully
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Pressed") &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null; // Wait until the next frame
        }

        // Deselect the button to remove highlight after animation completes
        EventSystem.current.SetSelectedGameObject(null);

        // Reset the isAnimating flag for future interactions
        isAnimating = false;
    }
}
