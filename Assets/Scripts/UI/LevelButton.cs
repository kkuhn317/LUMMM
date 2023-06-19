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

    private void Start()
    {
        levelNameText.text = levelDescription.levelName;
        videoYearText.text = levelDescription.videoYear;
        levelDescriptionText.text = levelDescription.levelDescription;

        videoLinkButton.onClick.AddListener(OpenVideoLink);
    }

    private void OpenVideoLink()
    {
        Application.OpenURL(levelDescription.videoLink);
    }
}
