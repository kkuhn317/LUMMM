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

    /// <summary>True if the current scene was loaded via FadeInOutScene — suppresses CircleTransition.</summary>
    public static bool LoadedWithFade { get; private set; }

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

    public void LoadSceneWithoutFadeIn(string sceneName)
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

    /// <summary>
    /// Fades to black, loads the scene, then lets CircleTransition handle the reveal.
    /// Use this for death/restart — fade out is handled here, fade in is handled by CircleTransition.
    /// </summary>
    public void RestartSceneWithFade(string sceneName)
    {
        if (isTransitioning) return;
        currentFadeCoroutine = StartCoroutine(FadeAndRestartScene(sceneName));
    }

    public void RestartSceneWithFade(int sceneIndex)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        RestartSceneWithFade(sceneName);
    }

    private IEnumerator FadeAndRestartScene(string sceneName)
    {
        isTransitioning = true;

        // Fade to black
        yield return StartCoroutine(FadeToColor(Color.black, fadeDuration));

        // Do NOT set LoadedWithFade — CircleTransition should play on the new scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;

        yield return new WaitForEndOfFrame();

        // Fade the black overlay back to clear so CircleTransition can take over
        yield return StartCoroutine(FadeToColor(Color.clear, 0f));

        isTransitioning = false;
        currentFadeCoroutine = null;
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
        
        // Signal that the incoming scene was loaded via fade — suppresses CircleTransition
        LoadedWithFade = true;

        // Load the scene asynchronously so we can wait for it to fully complete
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;

        // Wait an extra frame for scene objects (including CircleTransition) to fully initialize
        yield return new WaitForEndOfFrame();

        // Reset LoadedWithFade NOW — CircleTransition.Start() has already run by this point
        // and read the flag. Resetting here means the next scene load starts clean.
        LoadedWithFade = false;

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
                LoadSceneWithoutFadeIn(pendingSceneName);
        }
    }

    private IEnumerator FadeToColor(Color targetColor, float duration)
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        Color startColor = fadeImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (targetColor.a == 0f && !string.IsNullOrEmpty(pendingSceneName))
                break;

            fadeImage.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f); // Clamp to max 50ms per frame
            yield return null;
        }

        fadeImage.color = targetColor;

        if (targetColor.a == 0f)
            fadeImage.gameObject.SetActive(false);
    }
}