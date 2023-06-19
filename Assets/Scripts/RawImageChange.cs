using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RawImageChange : MonoBehaviour
{
    public GameObject items;
    public List<Texture2D> images;
    private int currentImageIndex = -1; //Check to ensure that the new random index is different from the current index of the image

    public void ChangeImageTexture()
    {
        int randomIndex = currentImageIndex;
        while (randomIndex == currentImageIndex)
        {
            randomIndex = Random.Range(0, images.Count);
        }
        currentImageIndex = randomIndex;
        GetComponent<RawImage>().texture = images[randomIndex];

        for (int i = 0; i < items.transform.childCount; i++)
        {
            items.transform.GetChild(i).GetComponent<RawImage>().texture = images[randomIndex];
        }
    }
}
