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
    private bool pendingRevealAfterLoad = true;

    /// <summary>
    /// True if the current scene was loaded through this screen-fade system.
    /// Used to suppress CircleTransition on the next scene.
    /// </summary>
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

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            fadeImage.color = Color.clear;
        }
    }

    /// <summary>
    /// Fade the screen to black, load the new scene, then reveal the new scene back from black.
    /// Use this for normal scene transitions.
    /// </summary>
    public void LoadSceneWithScreenFade(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.Log("Queueing scene transition to: " + sceneName);
            pendingSceneName = sceneName;
            pendingRevealAfterLoad = true;
            return;
        }

        currentFadeCoroutine = StartCoroutine(TransitionAndLoadScene(sceneName, true));
    }

    /// <summary>
    /// Fade the screen to black, load the new scene, but do not reveal it here.
    /// Useful when another transition system in the next scene will handle the reveal.
    /// </summary>
    public void LoadSceneWithoutScreenReveal(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.Log("Queueing scene transition to: " + sceneName);
            pendingSceneName = sceneName;
            pendingRevealAfterLoad = false;
            return;
        }

        currentFadeCoroutine = StartCoroutine(TransitionAndLoadScene(sceneName, false));
    }

    /// <summary>
    /// Fade the screen to black, reload the scene, then let CircleTransition reveal the scene.
    /// Use this for death or restart flows.
    /// </summary>
    public void RestartSceneWithFadeToBlack(string sceneName)
    {
        if (isTransitioning) return;
        currentFadeCoroutine = StartCoroutine(FadeToBlackAndRestartScene(sceneName));
    }

    public void RestartSceneWithFadeToBlack(int sceneIndex)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        RestartSceneWithFadeToBlack(sceneName);
    }

    private IEnumerator FadeToBlackAndRestartScene(string sceneName)
    {
        isTransitioning = true;

        yield return StartCoroutine(FadeToColor(Color.black, fadeDuration));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;

        Time.timeScale = 1f;

        yield return new WaitForEndOfFrame();

        yield return StartCoroutine(FadeToColor(Color.clear, 0f));

        isTransitioning = false;
        currentFadeCoroutine = null;
    }

    public void LoadNextSceneWithScreenFade()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            LoadSceneWithScreenFade(sceneName);
        }
        else
        {
            Debug.LogWarning("No next scene available.");
        }
    }

    public void LoadSceneWithScreenFade(int sceneIndex)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        LoadSceneWithScreenFade(sceneName);
    }

    private IEnumerator TransitionAndLoadScene(string sceneName, bool revealAfterLoad)
    {
        isTransitioning = true;

        // Cover the current scene with black.
        yield return StartCoroutine(FadeToColor(Color.black, fadeDuration));

        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            Debug.Log("Loading queued scene instead: " + pendingSceneName);
            sceneName = pendingSceneName;
            revealAfterLoad = pendingRevealAfterLoad;
            pendingSceneName = "";
        }

        LoadedWithFade = true;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;

        // New scene is now active — restore global time here.
        Time.timeScale = 1f;

        yield return new WaitForEndOfFrame();

        LoadedWithFade = false;

        if (revealAfterLoad)
        {
            yield return StartCoroutine(FadeToColor(Color.clear, fadeDuration));
        }

        isTransitioning = false;
        currentFadeCoroutine = null;

        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            Debug.Log("Starting queued transition: " + pendingSceneName);

            if (pendingRevealAfterLoad)
                LoadSceneWithScreenFade(pendingSceneName);
            else
                LoadSceneWithoutScreenReveal(pendingSceneName);
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
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            yield return null;
        }

        fadeImage.color = targetColor;

        if (targetColor.a == 0f)
            fadeImage.gameObject.SetActive(false);
    }
}