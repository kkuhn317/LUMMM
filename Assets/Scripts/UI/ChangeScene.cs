using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    //This script can be used to load any scene
    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
        // Debug.Log("New level");
    }

    //Method to exit the application
    public void ExitApplication()
    {
        Application.Quit();
        // Debug.Log("Exit Application");
    }
    
    public void LoadSceneWithFade(string sceneName)
    {
        if (FadeInOutScene.Instance != null)
        {
            FadeInOutScene.Instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            Debug.LogError("No FadeInOutScene instance found! Loading scene without fade.");
            SceneManager.LoadScene(sceneName);
        }
    }
}