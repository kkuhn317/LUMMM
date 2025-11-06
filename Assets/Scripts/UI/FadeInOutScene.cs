using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeInOutScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 0.5f;
    public bool isTransitioning = false;
    public static FadeInOutScene Instance;

    private Coroutine currentFadeCoroutine;
    private string pendingSceneName = "";
    private bool pendingFadeOut = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Ensure fade image starts in the correct state
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            fadeImage.color = Color.clear;
        }
    }

    public void LoadSceneWithFade(string sceneName)
    {
        // If already transitioning, queue the new scene
        if (isTransitioning)
        {
            Debug.Log("Queueing scene transition to: " + sceneName);
            pendingSceneName = sceneName;
            pendingFadeOut = true;
            return;
        }
        
        currentFadeCoroutine = StartCoroutine(FadeAndLoadScene(sceneName, true));
    }

    public void LoadSceneWithoutFadeOut(string sceneName)
    {
        // If already transitioning, queue the new scene
        if (isTransitioning)
        {
            Debug.Log("Queueing scene transition to: " + sceneName);
            pendingSceneName = sceneName;
            pendingFadeOut = false;
            return;
        }
        
        currentFadeCoroutine = StartCoroutine(FadeAndLoadScene(sceneName, false));
    }

    public void LoadNextSceneWithFade()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            LoadSceneWithFade(sceneName);
        }
        else
        {
            Debug.LogWarning("No next scene available.");
        }
    }

    public void LoadSceneWithFade(int sceneIndex)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        LoadSceneWithFade(sceneName);
    }

    private IEnumerator FadeAndLoadScene(string sceneName, bool fadeOutAfterLoad)
    {
        isTransitioning = true;
        
        // Fade in to black
        yield return StartCoroutine(FadeToColor(Color.black, fadeDuration));
        
        // Check if a new scene was requested during the fade-in
        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            Debug.Log("Loading queued scene instead: " + pendingSceneName);
            sceneName = pendingSceneName;
            fadeOutAfterLoad = pendingFadeOut;
            pendingSceneName = ""; // Clear the queue
        }
        
        // Load the scene
        SceneManager.LoadScene(sceneName);
        
        // Wait for scene to initialize (one frame, just in case)
        yield return null;
        
        // Fade out if requested
        if (fadeOutAfterLoad)
        {
            yield return StartCoroutine(FadeToColor(Color.clear, fadeDuration));
        }
        
        // Transition complete
        isTransitioning = false;
        currentFadeCoroutine = null;
        
        // Check if another transition was requested while we were finishing
        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            Debug.Log("Starting queued transition: " + pendingSceneName);
            // Use the appropriate method based on the fadeOut setting
            if (pendingFadeOut)
                LoadSceneWithFade(pendingSceneName);
            else
                LoadSceneWithoutFadeOut(pendingSceneName);
        }
    }

    private IEnumerator FadeToColor(Color targetColor, float duration)
    {
        fadeImage.gameObject.SetActive(true);
        Color startColor = fadeImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // If a new scene was queued during fade-out, we can break early
            if (targetColor.a == 0f && !string.IsNullOrEmpty(pendingSceneName))
            {
                break;
            }

            fadeImage.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = targetColor;

        // Hide the image if we're fully transparent
        if (targetColor.a == 0f)
            fadeImage.gameObject.SetActive(false);
    }

    // This is just optional helper methods to check fade state
    // they're not strictly necessary, I just added them for convenience in the future
    public bool IsFadingIn()
    {
        return isTransitioning && fadeImage.color.a < 1f;
    }

    public bool IsFadingOut()
    {
        return isTransitioning && fadeImage.color.a > 0f;
    }
}