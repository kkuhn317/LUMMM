using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeInOutScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 5f;

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
    private IEnumerator FadeIn(float waitTime)
    {
        fadeImage.gameObject.SetActive(true);

        float elapsedTime = 0f;

        while (elapsedTime < fadeSpeed)
        {
            fadeImage.color = Color.Lerp(Color.clear, Color.black, elapsedTime / fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeSpeed)
        {
            fadeImage.color = Color.Lerp(Color.black, Color.clear, elapsedTime / fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);
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

    private IEnumerator FadeInAndLoad(int sceneIndex, bool doFadeOut = true, float waitTime = 1.5f)
    {
        yield return StartCoroutine(FadeIn(fadeSpeed));

        SceneManager.LoadScene(sceneIndex);

        if (doFadeOut)
        {
            yield return new WaitForSeconds(waitTime);
        }
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeInAndLoad(sceneName, true));
    }

    public void LoadSceneWithoutFadeOut(string sceneName)
    {
        StartCoroutine(FadeInAndLoad(sceneName, false, 0.5f));
    }

    private IEnumerator FadeInAndLoad(string sceneName, bool doFadeOut = true, float waitTime = 1.5f)
    {
        yield return StartCoroutine(FadeIn(fadeSpeed));

        SceneManager.LoadScene(sceneName);

        if (doFadeOut)
        {
            yield return new WaitForSeconds(waitTime);
        }
    }
}
