using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeInOutScene : MonoBehaviour
{
    public Image fadeImage;
    public float fadeSpeed = 5f;

    private static FadeInOutScene instance;

    [SerializeField] bool fadeIn = false;
    [SerializeField] bool fadeOut = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(instance.gameObject);
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        if (fadeIn)
        {
            fadeImage.color = Color.Lerp(fadeImage.color, Color.black, fadeSpeed * Time.deltaTime);   
        }
        else if (fadeOut)
        {
            fadeImage.color = Color.Lerp(fadeImage.color, Color.clear, fadeSpeed * Time.deltaTime);
        }
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeInAndLoad(sceneName));
    }

    public void LoadSceneWithoutFadeIn(string sceneName)
    {
        StartCoroutine(FadeInAndLoad(sceneName, false));
    }

    IEnumerator FadeInAndLoad(string sceneName, bool doFadeOut = true)
    {
        fadeIn = true;
        fadeOut = doFadeOut;

        fadeImage.gameObject.SetActive(true);

        // Wait until the fade in is complete
        while (fadeImage.color.a >= 1)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(sceneName);

        fadeImage.gameObject.SetActive(true);

        fadeIn = false;
        fadeOut = true;

        // Wait until the fade out is complete
        while (fadeImage.color.a <= 0.01f)
        {
            yield return null;
        }

        //fadeOut = false;

        yield return new WaitForSeconds(1.5f);
        fadeImage.gameObject.SetActive(false);
    }
}
