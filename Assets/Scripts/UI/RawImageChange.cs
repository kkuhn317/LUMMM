using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RawImageChange : MonoBehaviour
{
    public GameObject items;
    public List<Texture2D> images;
    private int currentImageIndex = -1;
    private Vector3 originalScale;

    private Dictionary<GameObject, Vector3> originalChildScales = new Dictionary<GameObject, Vector3>();

    private void Start()
    {
        // Store the original scale of the main GameObject
        originalScale = transform.localScale;

        // Store the original scale for each child item
        for (int i = 0; i < items.transform.childCount; i++)
        {
            GameObject child = items.transform.GetChild(i).gameObject;
            originalChildScales[child] = child.transform.localScale;
        }
    }

    public void ChangeImageTexture()
    {
        // Revert to the original scale before playing the animation
        transform.localScale = originalScale;

        PlayScaleAnimation(gameObject, new Vector3(0.55f, 0.55f, 0.55f), 0.25f);

        int randomIndex = currentImageIndex;
        while (randomIndex == currentImageIndex)
        {
            randomIndex = Random.Range(0, images.Count);
        }
        currentImageIndex = randomIndex;
        GetComponent<RawImage>().texture = images[randomIndex];

        for (int i = 0; i < items.transform.childCount; i++)
        {
            GameObject child = items.transform.GetChild(i).gameObject;

            // Revert to the original scale before playing the animation
            child.transform.localScale = originalChildScales[child];

            child.GetComponent<RawImage>().texture = images[randomIndex];
            // Play the scale animation on the child
            PlayScaleAnimation(child, new Vector3(0.7f, 0.7f, 0.7f), 0.25f);
        }
    }

    private void PlayScaleAnimation(GameObject targetObject, Vector3 scaleAnimationRange, float animationDuration)
    {
        LeanTween.scale(targetObject, scaleAnimationRange, animationDuration) 
            .setEase(LeanTweenType.easeInOutQuad)
            .setLoopPingPong(1); // Play the animation once forward and once backward
    }
}
