using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;

public class LevelButton : MonoBehaviour
{
    public LevelInfo levelInfo;

    public GameObject selectionMark;
    public GameObject completeLevelMark;
    public GameObject perfectLevelMark;
    public GameObject checkpointFlag;
    public GameObject obtainedRank;
    public List<Image> greenCoinListImages;
    public LevelSelectionManager.MarioAnimator marioAnimator;

    public Sprite[] GreenCoinsprite; // 0 - uncollected, 1 - collected, 2 - unavailable
    public Sprite[] minirankTypes; // 0 - poison, 1 - mushroom, 2 - flower, 3 - 1up, 4 - star

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (levelInfo.levelScene == "") // Unavilable level
        {
            foreach (Image coin in greenCoinListImages)
            {
                // coin.sprite = GreenCoinsprite[2];
                coin.gameObject.SetActive(false);
            }
            return;
        }

        string id = levelInfo.levelID;
        // Set the saved info
        
        // Perfect/Complete Level Mark
        // TODO: Set the LevelPerfect playerpref when you perfect a level
        if (PlayerPrefs.GetInt("LevelPerfect_" + id, 0) == 1)
        {
            perfectLevelMark.SetActive(true);
        }
        else if (PlayerPrefs.GetInt("LevelCompleted_" + id, 0) == 1)
        {
            completeLevelMark.SetActive(true);
        }

        // Coins
        for (int i = 0; i < 3; i++)
        {
            int sprite = PlayerPrefs.GetInt("CollectedCoin" + i + "_" + id, 0);
            greenCoinListImages[i].sprite = GreenCoinsprite[sprite];
        }

        // Checkpoint
        if (PlayerPrefs.GetString("SavedLevel") == id && PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0 ) == 1)
        {
            checkpointFlag.SetActive(true);
        }
        else{
            checkpointFlag.SetActive(false);
        }

        // Rank
        int rank = PlayerPrefs.GetInt("HighestPlayerRank_" + id, -1);
        if (rank > 0)
        {
            obtainedRank.SetActive(true);
            obtainedRank.GetComponent<Image>().sprite = minirankTypes[rank - 1];
        }
    }


    public void OnClick()
    {
        LevelSelectionManager.Instance.OnLevelButtonClick(this);
    }

    public void OnDoubleClick()
    {
        LevelSelectionManager.Instance.OnPlayButtonClick();
        audioSource.Play();
    }


}
