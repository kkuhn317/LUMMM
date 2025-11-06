using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeInOutScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 0.5f; // Made shorter as requested
    public static FadeInOutScene Instance;

    // Separate states for better control
    public bool isFadingIn { get; private set; }
    public bool isFadingOut { get; private set; }
    public bool isTransitioning => isFadingIn || isFadingOut;

    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize fade image
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void LoadSceneWithFade(int sceneIndex)
    {
        LoadSceneWithFade(sceneIndex.ToString());
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Already transitioning, ignoring duplicate call");
            return;
        }
        
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);
        
        currentFadeCoroutine = StartCoroutine(FadeAndLoadScene(sceneName, true));
    }

    public void LoadSceneWithoutFadeOut(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Already transitioning, ignoring duplicate call");
            return;
        }
        
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);
        
        currentFadeCoroutine = StartCoroutine(FadeAndLoadScene(sceneName, false));
    }

    private IEnumerator FadeAndLoadScene(string sceneName, bool fadeOutAfterLoad)
    {
        // Fade in to black (block input during this phase)
        isFadingIn = true;
        yield return StartCoroutine(FadeToColor(Color.black, fadeDuration));
        isFadingIn = false;
        
        // Load scene
        SceneManager.LoadScene(sceneName);
        
        // Fade out if requested (allow input during this phase)
        if (fadeOutAfterLoad)
        {
            isFadingOut = true;
            yield return StartCoroutine(FadeToColor(Color.clear, fadeDuration));
            isFadingOut = false;
        }
        
        currentFadeCoroutine = null;
    }

    private IEnumerator FadeToColor(Color targetColor, float duration)
    {
        fadeImage.gameObject.SetActive(true);
        Color startColor = fadeImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            fadeImage.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = targetColor;
        
        // Hide the image if we're fully transparent
        if (targetColor.a == 0f)
            fadeImage.gameObject.SetActive(false);
    }

    // Public methods to check specific states
    public bool CanAcceptInput()
    {
        // Allow input when not fading OR when only fading out
        return !isFadingIn; // Block during fade in, allow during fade out and no fade
    }

    // Force stop any ongoing fade (useful for debugging)
    public void ForceStopFade()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }
        isFadingIn = false;
        isFadingOut = false;
        
        // Ensure fade image is hidden
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            fadeImage.color = Color.clear;
        }
    }
}