using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomImage : MonoBehaviour
{
    public GameObject items;
    public List<Sprite> images;
    private int currentImageIndex = -1; //Check to ensure that the new random index is different from the current index of the image

    public void ChangeImage()
    {
        int randomIndex = currentImageIndex;
        while (randomIndex == currentImageIndex)
        {
            randomIndex = Random.Range(0, images.Count);
        }
        currentImageIndex = randomIndex;
        GetComponent<Image>().sprite = images[randomIndex];

        for (int i = 0; i < items.transform.childCount; i++)
        {
            items.transform.GetChild(i).GetComponent<Image>().sprite = images[randomIndex];
        }
    }
}
