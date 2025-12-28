using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WOSRenderTextureSetup : MonoBehaviour
{
    private RenderTexture rt;
    public Camera cam;
    public RawImage[] rawImages;

    // Start is called before the first frame update
    void Start()
    {
        // Instantiate a new RenderTexture with the dimensions of the screen.
        rt = new RenderTexture(Screen.width, Screen.height, 24);
        // Set the RenderTexture as the active one.
        RenderTexture.active = rt;
        // Set the camera's target texture to the RenderTexture.
        cam.targetTexture = rt;

        // Set the RawImages' textures to the RenderTexture.
        foreach (RawImage rawImage in rawImages)
        {
            rawImage.texture = rt;
        }
    }
}
