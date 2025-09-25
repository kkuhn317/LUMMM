using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatButton : MonoBehaviour
{
    public Sprite unlockedImage;
    public TMP_Text levelsRequiredText;
    public LevelButton[] requiredLevels;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (GetLevelsBeaten() >= requiredLevels.Length)
        {
            GetComponent<Image>().sprite = unlockedImage;
        }

        levelsRequiredText.text = GetLevelsBeaten() + "/" + requiredLevels.Length;
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
        }
    }
}
