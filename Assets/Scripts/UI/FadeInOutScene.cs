using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeInOutScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 5f;

    public bool transitioning = false;

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

        float elapsedTime = 0f;

        while (elapsedTime < fadeSpeed)
        {
            fadeImage.color = Color.Lerp(Color.clear, Color.black, elapsedTime / fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (doFadeOut)
        {
            yield return new WaitForSeconds(waitTime);

            StartCoroutine(FadeOut());
        } else {
            fadeImage.gameObject.SetActive(false);
            transitioning = false;
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        yield return null;  // Wait a frame because of first frame lag when entering a new scene
        // TODO: maybe come up with a better way to fix it

        while (elapsedTime < fadeSpeed)
        {
            print("elapsedTime: " + elapsedTime);
            fadeImage.color = Color.Lerp(Color.black, Color.clear, elapsedTime / fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

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
        if (transitioning)
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
