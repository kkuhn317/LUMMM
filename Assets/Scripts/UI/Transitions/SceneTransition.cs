using UnityEngine;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public void LoadSceneWithFade(string sceneName)
    {
        FadeInOutScene.Instance.LoadSceneWithFade(sceneName);
    }

    public void LoadSceneWithoutFadeIn(string sceneName)
    {
        FadeInOutScene.Instance.LoadSceneWithoutFadeIn(sceneName);
    }     
}
