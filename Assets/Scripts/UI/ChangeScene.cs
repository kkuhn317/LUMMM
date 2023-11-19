using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    //This script can be used to load any scene
    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
        // Debug.Log("New level");
    }

    //Method to reset the current level
    //! DO NOT USE THIS METHOD TO RESET THE LEVEL!
    //! Use GameManager.Instance.ResetLevelFromCheckpoint OR ResetLevelFromBeginning instead
    // TODO: Remove this method when we know it's not being used
    public void ResetLevel() {
        // Reset Checkpoint
        GlobalVariables.checkpoint = -1;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        // Debug.Log("Reset level");
    }

    //Method to exit the application
    public void ExitApplication() {
        Application.Quit();
        // Debug.Log("Exit Application");
    }
}
