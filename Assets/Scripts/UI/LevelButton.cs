using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    public SelectionDescription levelDescription;

    public TMP_Text levelNameText;
    public TMP_Text videoYearText;
    public Button videoLinkButton;
    public TMP_Text levelDescriptionText;

    // It has text at the beginning
    private void Start()
    {
        LevelDescription();
    }

    // Allows you to assign a different scriptable object to each button and update the UI accordingly.
    public void SetLevelDescription(SelectionDescription newLevelDescription)
    {
        levelDescription = newLevelDescription;
        LevelDescription();
    }

    private void LevelDescription()
    {
        // The text relates to the scriptable object 
        levelNameText.text = levelDescription.levelName;
        videoYearText.text = levelDescription.videoYear;
        levelDescriptionText.text = levelDescription.levelDescription;

        // Remove any existing listeners from the button's onClick event
        videoLinkButton.onClick.RemoveAllListeners();

        // set lives
        GlobalVariables.lives = levelDescription.lives;

        videoLinkButton.onClick.AddListener(OpenVideoLink);
    }

    // Allows you open an URL
    private void OpenVideoLink()
    {
        Application.OpenURL(levelDescription.videoLink);
    }
}
