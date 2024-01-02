using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScript : MonoBehaviour
{
    public float transitionChangeDelay = 3f;

    void Start()
    {
        Invoke(nameof(ChangeScene), transitionChangeDelay);
    }

    void ChangeScene()
    {
        CursorHelper.ShowCursor();
        SceneManager.LoadScene("SelectLevel");
    }

}
