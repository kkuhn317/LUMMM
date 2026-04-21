using UnityEngine;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public void LoadSceneWithFade(string sceneName)
    {
        FadeInOutScene.Instance.LoadSceneWithScreenFade(sceneName);
    }

    public void LoadSceneWithoutFadeIn(string sceneName)
    {
        FadeInOutScene.Instance.LoadSceneWithoutScreenReveal(sceneName);
    }     
}
