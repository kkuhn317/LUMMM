using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeInOutScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 5f;
    public bool transitioning = false;
    private bool fadingIn = false;  // Whether the scene is currently fading into black.
    private bool fadingOut = false; // Whether the scene is currently fading out of black.
    public static FadeInOutScene Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #region FadeInAndOut
    private IEnumerator FadeIn(float waitTime, bool doFadeOut = true)
    {
        fadeImage.gameObject.SetActive(true);
        fadingIn = true;

        float elapsedTime = 0f;

        Color originalColor = Color.clear;
        if (fadingOut)
        {  
            // Fade in from the fade out color so it still looks smooth.
            originalColor = fadeImage.color;
        }

        while (elapsedTime < fadeSpeed)
        {
            fadeImage.color = Color.Lerp(originalColor, Color.black, elapsedTime / fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }


        if (doFadeOut)
        {
            yield return new WaitForSeconds(waitTime);
            fadingIn = false;
            StartCoroutine(FadeOut());
        } else {
            fadingIn = false;
            fadeImage.gameObject.SetActive(false);
            transitioning = false;
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        fadingOut = true;

        while (elapsedTime < fadeSpeed && !fadingIn)
        {
            fadeImage.color = Color.Lerp(Color.black, Color.clear, elapsedTime / fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // If interrupted by fading in, just return and let the fade in finish.
        if (fadingIn)
        {
            yield break;
        }

        fadingOut = false;
        fadeImage.gameObject.SetActive(false);
        transitioning = false;
    }
    #endregion  

    public void LoadNextSceneWithFade()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            StartCoroutine(FadeInAndLoad(nextSceneIndex, true));
        }
        else
        {
            Debug.LogWarning("No next scene available.");
        }
    }

    private IEnumerator FadeInAndLoad(int sceneIndex, bool doFadeOut = true)
    {
        yield return StartCoroutine(FadeIn(fadeSpeed, doFadeOut));

        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator FadeInAndLoad(string sceneName, bool doFadeOut = true)
    {
        yield return StartCoroutine(FadeIn(fadeSpeed, doFadeOut));

        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneWithFade(string sceneNameOrIndex, bool doFadeOut = true)
    {
        // If already transitioning somewhere, don't allow another transition. (fading out is fine though)
        if (fadingIn)
        {
            return;
        }
        transitioning = true;
        

        if (int.TryParse(sceneNameOrIndex, out int sceneIndex))
        {
            StartCoroutine(FadeInAndLoad(sceneIndex, doFadeOut));
        }
        else
        {
            StartCoroutine(FadeInAndLoad(sceneNameOrIndex, doFadeOut));
        }
    }

    public void LoadSceneWithoutFadeOut(string sceneNameOrIndex)
    {
        LoadSceneWithFade(sceneNameOrIndex, false);
    }
}
