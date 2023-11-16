using UnityEngine;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    private FadeInOutScene fadeInOutScene;

    public void LoadSceneWithFade(string sceneName)
    {
        fadeInOutScene = FindObjectOfType<FadeInOutScene>();

        if (fadeInOutScene != null)
        {
            fadeInOutScene.LoadSceneWithFade(sceneName);
        }
    }

    public void LoadSceneWithoutFadeOut(string sceneName)
    {
        fadeInOutScene = FindObjectOfType<FadeInOutScene>();

        if (fadeInOutScene != null)
        {
            fadeInOutScene.LoadSceneWithoutFadeOut(sceneName);
        }
    }     
}
