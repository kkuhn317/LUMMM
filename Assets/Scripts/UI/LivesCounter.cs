using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;

public class LivesCounter : MonoBehaviour
{
    public bool oldNumber = false;

    // Start is called before the first frame update
    void Start()
    {
        int livesNum = GlobalVariables.lives;
        if (oldNumber) livesNum += 1;
        GetComponent<TextMeshProUGUI>().text = livesNum.ToString();
        if (oldNumber) {
            Invoke("PlaySound", 0.5f);

            PlayableDirector director = GameObject.FindObjectOfType<PlayableDirector>();

            if (director != null) {
                double timelineDuration = director.duration;

                // Use the timeline duration as the delay before loading the scene
                Invoke("GoBackToLevel", (float)timelineDuration);
            } else {
                Invoke("GoBackToLevel", 2f);
            }
        }
    }

    void GoBackToLevel() {
        SceneManager.LoadScene(GlobalVariables.levelInfo.levelScene);
    }

    void PlaySound() {
        GetComponent<AudioSource>().Play();
    }
}
