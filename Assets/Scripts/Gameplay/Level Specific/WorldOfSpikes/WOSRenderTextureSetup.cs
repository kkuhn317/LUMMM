using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the World of Spikes camera RenderTexture synchronized with the current
/// browser canvas size. WebGL can change Screen.width/height after entering or
/// leaving fullscreen, especially when the player exits with Esc.
/// </summary>
public class WOSRenderTextureSetup : MonoBehaviour
{
    private RenderTexture rt;
    private int currentWidth;
    private int currentHeight;

    public Camera cam;
    public RawImage[] rawImages;

    private void OnEnable()
    {
        RebuildRenderTexture(force: true);
    }

    private void Update()
    {
        RebuildRenderTexture(force: false);
    }

    private void RebuildRenderTexture(bool force)
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);

        if (!force && rt != null && width == currentWidth && height == currentHeight)
            return;

        ReleaseRenderTexture();

        currentWidth = width;
        currentHeight = height;

        rt = new RenderTexture(width, height, 24)
        {
            name = $"WOS_{width}x{height}"
        };
        rt.Create();

        if (cam != null)
            cam.targetTexture = rt;

        if (rawImages == null) return;

        foreach (RawImage rawImage in rawImages)
        {
            if (rawImage != null)
                rawImage.texture = rt;
        }
    }

    private void OnDisable()
    {
        ReleaseRenderTexture();
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    private void ReleaseRenderTexture()
    {
        if (rt == null) return;

        if (cam != null && cam.targetTexture == rt)
            cam.targetTexture = null;

        if (rawImages != null)
        {
            foreach (RawImage rawImage in rawImages)
            {
                if (rawImage != null && rawImage.texture == rt)
                    rawImage.texture = null;
            }
        }

        rt.Release();
        Destroy(rt);
        rt = null;
    }
}