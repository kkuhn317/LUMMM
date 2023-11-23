using UnityEngine;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public void LoadSceneWithFade(string sceneName)
    {
        FadeInOutScene.Instance.LoadSceneWithFade(sceneName);
    }

    public void LoadSceneWithoutFadeOut(string sceneName)
    {
        FadeInOutScene.Instance.LoadSceneWithoutFadeOut(sceneName);
    }     
}
