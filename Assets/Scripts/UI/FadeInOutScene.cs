using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class FadeInOutScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;
    public bool transitioning = false;
    private bool isFading = false; // Whether the scene is currently fading
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
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;
        
        fadeImage.gameObject.SetActive(true);
        isFading = true;

        float elapsedTime = 0f;
        Color startColor = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            fadeImage.color = Color.Lerp(startColor, Color.black, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = Color.black; // Ensure it's fully black

        if (doFadeOut)
        {
            yield return new WaitForSeconds(waitTime);
            StartCoroutine(FadeOut());
        }
        else
        {
            isFading = false;
            // Keep the black screen for scenes without fade out
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            fadeImage.color = Color.Lerp(Color.black, Color.clear, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = Color.clear;
        fadeImage.gameObject.SetActive(false);
        isFading = false;
        transitioning = false;

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = true; 
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
        yield return StartCoroutine(FadeIn(fadeDuration, doFadeOut));
        SceneManager.LoadScene(sceneIndex);
        
        if (doFadeOut)
        {
            // Start fade out after scene load
            StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeInAndLoad(string sceneName, bool doFadeOut = true)
    {
        yield return StartCoroutine(FadeIn(fadeDuration, doFadeOut));
        SceneManager.LoadScene(sceneName);
        
        if (doFadeOut)
        {
            // Start fade out after scene load
            StartCoroutine(FadeOut());
        }
    }

    public void LoadSceneWithFade(string sceneNameOrIndex, bool doFadeOut = true)
    {
        if (isFading)
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