using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionManager : MonoBehaviour
{
    public static LevelSelectionManager Instance { get; private set; }

    public Button playButton;
    public Sprite uncollectedGreenCoinsprite;
    public Sprite collectedGreenCoinsprite;

    public Sprite[] minirankTypes;

    [System.Serializable]
    public class LevelAction
    {
        public Button levelButton;
        public string levelName;
        public bool isCompleted;
        public bool isPerfect;
        public GameObject completeLevelMark;
        public GameObject perfectLevelMark;
        public GameObject checkpointFlag;
        public GameObject obtainedRank;
        public List<Image> greenCoinListImages;

        [HideInInspector]
        public bool hasCompletedActionsExecuted = false;
        [HideInInspector]
        public bool hasPerfectActionsExecuted = false;
    }

    public List<LevelAction> levelActions = new List<LevelAction>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playButton.gameObject.SetActive(false); // Deactivate the button if it's initially active

        // Hook up each level button to the OnClick event
        foreach (var levelAction in levelActions)
        {
            var action = levelAction; // Create a local variable
            action.levelButton.onClick.AddListener(() => OnLevelButtonClick(action));
        }

    }

    void Update()
    {
        foreach (var buttonAction in levelActions)
        {
            if (buttonAction.isCompleted && !buttonAction.hasCompletedActionsExecuted)
            {
                // Execute actions for completed state only once
                buttonAction.completeLevelMark.SetActive(true);
                buttonAction.hasCompletedActionsExecuted = true;
            }

            if (buttonAction.isPerfect && !buttonAction.hasPerfectActionsExecuted)
            {
                // Execute actions for perfect state only once
                buttonAction.perfectLevelMark.SetActive(true);
                buttonAction.hasPerfectActionsExecuted = true;
                // Deactivate other GameObjects if needed
                buttonAction.completeLevelMark.SetActive(false);
            }
        }
    }


    void OnLevelButtonClick(LevelAction buttonAction)
    {
        playButton.gameObject.SetActive(true);
        playButton.onClick.RemoveAllListeners(); // Remove previous listeners if any

        if (!string.IsNullOrEmpty(buttonAction.levelName))
        {
            playButton.onClick.AddListener(() => OnPlayButtonClick(buttonAction.levelName));
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
