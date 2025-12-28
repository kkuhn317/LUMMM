using System.Collections.Generic;
using UnityEngine;

public class BopAnimation : MonoBehaviour
{
    public Transform itemParent; // Reference for the parent of items
    public Vector3 bopAnimationScale = new(0.6f, 0.6f, 0.7f);
    // Timer control for animation duration
    public float animationDuration = 0.25f; // How long the animation will take
    private Dictionary<GameObject, Vector3> originalChildScales = new();

    // Descriptive variable for the child bop animation scale
    private Vector3 childBopAnimationScale = new(0.7f, 0.7f, 0.7f); // Child scale used in the bop animation

    private void Start()
    {
        // If itemParent is not assigned, use the GameObject with this script as the parent
        if (itemParent == null)
        {
            itemParent = transform;
        }

        // Store the original scale for the GameObject with this script
        originalChildScales[gameObject] = transform.localScale;

        // Store the original scale for each child in itemParent
        for (int i = 0; i < itemParent.childCount; i++)
        {
            GameObject child = itemParent.GetChild(i).gameObject;
            originalChildScales[child] = child.transform.localScale;
        }
    }

    public void PlayBopAnimation()
    {
        // Apply the bop animation to the GameObject with this script
        if (originalChildScales.TryGetValue(gameObject, out Vector3 originalScale))
        {
            // Reset scale before starting animation
            transform.localScale = originalScale;

            // Cancel any active LeanTween animation on the object
            LeanTween.cancel(gameObject);

            // Apply the scale animation
            LeanTween.scale(gameObject, bopAnimationScale, animationDuration)
                .setEase(LeanTweenType.easeInOutQuad)
                .setLoopPingPong(1)
                .setOnComplete(() => transform.localScale = originalScale); // Reset after animation
        }

        // Apply the bop animation to each child of itemParent
        for (int i = 0; i < itemParent.childCount; i++)
        {
            GameObject child = itemParent.GetChild(i).gameObject;

            // Reset child scale to original before starting animation
            if (originalChildScales.TryGetValue(child, out Vector3 childOriginalScale))
            {
                child.transform.localScale = childOriginalScale;
            }

            // Cancel any active LeanTween animation on the child
            LeanTween.cancel(child);

            // Apply the scale animation to the child
            LeanTween.scale(child, childBopAnimationScale, animationDuration)
                .setEase(LeanTweenType.easeInOutQuad)
                .setLoopPingPong(1)
                .setOnComplete(() => child.transform.localScale = childOriginalScale); // Reset after animation
        }
    }
}
