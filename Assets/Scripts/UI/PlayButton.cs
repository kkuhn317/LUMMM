using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public Button playButton;

    [System.Serializable]
    public class ButtonAction
    {
        public Button levelButton;
        public string levelName;
    }

    public List<ButtonAction> buttonActions = new List<ButtonAction>();

    void Start()
    {
        // Hook up each level button to the OnClick event
        foreach (var buttonAction in buttonActions)
        {
            buttonAction.levelButton.onClick.AddListener(() => OnLevelButtonClick(buttonAction.levelName));
        }

        playButton.gameObject.SetActive(false); // Deactivate the button if it's initially active
    }

    void OnLevelButtonClick(string levelName)
    {
        playButton.gameObject.SetActive(true);
        playButton.onClick.RemoveAllListeners(); // Remove previous listeners if any

        if (!string.IsNullOrEmpty(levelName))
        {
            playButton.onClick.AddListener(() => OnPlayButtonClick(levelName));
        }
    }

    void OnPlayButtonClick(string levelName)
    {
        if (!string.IsNullOrEmpty(levelName))
        {
            FadeInOutScene.Instance.LoadSceneWithFade(levelName);
        }
    }
}
