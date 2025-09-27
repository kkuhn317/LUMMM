using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CheatsMenu;

public class CheatButton : MonoBehaviour
{
    public Sprite unlockedImage;
    public TMP_Text levelsRequiredText;
    public LevelButton[] requiredLevels;
    private AudioSource audioSource;
    public CanvasGroup mainCanvasGroup; // CanvasGroup for the main UI
    public CanvasGroup cheatListCanvasGroup; // CanvasGroup for the cheat list window
    public TMP_Text cheatListText;
    public Button closeButton;
    public AudioClip lockedSound;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (GetLevelsBeaten() >= requiredLevels.Length)
        {
            GetComponent<Image>().sprite = unlockedImage;
        }

        levelsRequiredText.text = GetLevelsBeaten() + "/" + requiredLevels.Length;

        // Build cheat list text
        CheatBinding[] cheats = cheatBindings;
        cheatListText.text = "";
        foreach (var cheat in cheats)
        {
            if (cheat.code == "club") continue;
            cheatListText.text += $"{cheat.code} - {GetCheatDescription(cheat.code)}\n";
        }

        // Assign close button
        closeButton.onClick.AddListener(OnCheatListClose);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public int GetLevelsBeaten()
    {
        int count = 0;
        foreach (LevelButton level in requiredLevels)
        {
            if (level.beaten)
            {
                count++;
            }
        }
        return count;
    }

    public void OnClick()
    {
        if (GetLevelsBeaten() >= requiredLevels.Length)
        {
            // todo: Open Menu showing cheat list
            audioSource.Play();

            // Disable main UI and enable cheat list UI
            mainCanvasGroup.interactable = false;
            mainCanvasGroup.blocksRaycasts = false;
            cheatListCanvasGroup.interactable = true;
            cheatListCanvasGroup.blocksRaycasts = true;
            cheatListCanvasGroup.gameObject.SetActive(true);

            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
        else
        {
            audioSource.PlayOneShot(lockedSound);
        }
    }

    public void OnCheatListClose()
    {
        // Enable main UI and disable cheat list UI
        mainCanvasGroup.interactable = true;
        mainCanvasGroup.blocksRaycasts = true;
        cheatListCanvasGroup.interactable = false;
        cheatListCanvasGroup.blocksRaycasts = false;
        cheatListCanvasGroup.gameObject.SetActive(false);

        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
