using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
using System;
using UnityEngine.SceneManagement;

public class DiscordManager : MonoBehaviour
{
    public long applicationID = 1325573479881379892;
    [Space]
    public string details = "Playing";
    public string state = "";
    [Space]
    public string largeImage = "LUMMM_icon_512px";
    public string largeText = "Level Up - Mario's Minigames Mayhem";
    public string smallImage;
    public string smallText;

    private long time;

    private static bool instanceExists;
    public Discord.Discord discord;

    // Dictionary to manage scene-specific Discord presence
    private Dictionary<string, (string details, string state, string largeImage, string largeText)> levelSettings = new()
    {
        { "AttentionPlease", ("Disclaimer", "Acknowledging the game is fan-made.", "LUMMM_icon_512px", "") },
        { "MainMenu", ("In the Main Menu", "The game started", "LUMMM_icon_512px", "") },
        { "OptionsMenu", ("In the Options Menu", "", "LUMMM_icon_512px", "") },
        { "Credits", ("Credits", "", "LUMMM_icon_512px", "") },
        { "SelectLevel", ("Selecting a level", "Level Up - Mario's Minigames Mayhem", "LUMMM_icon_512px", "") },
        { "TheGreatPyramidofGoomba", ("Level: The Great Pyramid of Goomba", "Unearthing ancient secrets!", "greatpyramiofgoomba", "") },
        { "HowWillMarioEscapeThisMaze", ("Level: How Will Mario Escape This Maze?", "How will Mario escape?", "marioescapethismaze", "") },
        { "SMB1-1", ("Level: SMB 1-1", "Feeling nostalgic with classic gameplay!", "world1-1", "") },
        { "TinyGoombaMaze", ("Level: Tiny Goomba Maze", "Avoiding traps and collecting coins!", "tinygoombamaze", "") },
        { "CoinDoorsMaze", ("Level: Coin Doors Maze", "Finding coins and opening doors!", "coindoorsmaze", "") },
        { "TheWorldofSpikes", ("Level: World of Spikes", "Precision jumping through danger!", "worldofspikes", "") },
        { "MarioWinMaze", ("Level: How Will Mario Win This Maze?", "April Fools!", "mariowinmaze", "") },
        { "Lives", ("Lost a life", "", "LUMMM_icon_512px", "") },
        { "LivesBadMario", ("Lost a life", "", "LUMMM_icon_512px", "") },
        { "LivesTinyGoombaMaze", ("Lost a life", "", "LUMMM_icon_512px", "") }
    };

    private void Awake()
    {
        if (!instanceExists)
        {
            instanceExists = true;
            DontDestroyOnLoad(gameObject);
        }
        else if (FindObjectsOfType(GetType()).Length > 1)
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            discord = new Discord.Discord(applicationID, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);
            time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Debug.Log("Discord initialized successfully.");
        }
        catch (ResultException ex)
        {
            Debug.LogWarning($"Failed to initialize Discord: {ex.Message}");
            discord = null; // Prevent further issues
        }
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (discord != null)
            {
                discord.RunCallbacks();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error running Discord callbacks: {ex.Message}");
        }
    }

    private void LateUpdate()
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        try
        {
            if (discord == null) return;

            var activityManager = discord.GetActivityManager();
            string sceneName = SceneManager.GetActiveScene().name;

            // Check if the current scene has specific settings
            if (levelSettings.ContainsKey(sceneName))
            {
                var (sceneDetails, sceneState, sceneLargeImage, sceneLargeText) = levelSettings[sceneName];

                // Update Discord presence with scene-specific settings
                var activity = new Discord.Activity
                {
                    Details = sceneDetails,
                    State = sceneState,
                    Assets =
                    {
                        LargeImage = sceneLargeImage,
                        LargeText = sceneLargeText,
                        SmallImage = smallImage,
                        SmallText = smallText
                    },
                    Timestamps =
                    {
                        Start = time
                    }
                };

                activityManager.UpdateActivity(activity, (res) =>
                {
                    if (res != Discord.Result.Ok) Debug.LogWarning("Failed connecting to Discord!");
                });
            }
            else
            {
                // Default activity for unlisted scenes
                var activity = new Discord.Activity
                {
                    Details = "Playing",
                    State = "",
                    Assets =
                    {
                        LargeImage = largeImage,
                        LargeText = largeText,
                        SmallImage = smallImage,
                        SmallText = smallText
                    },
                    Timestamps =
                    {
                        Start = time
                    }
                };

                activityManager.UpdateActivity(activity, (res) =>
                {
                    if (res != Discord.Result.Ok) Debug.LogWarning("Failed connecting to Discord!");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating Discord status: {ex.Message}");
            Destroy(gameObject); // Destroy manager if Discord fails completely
        }
    }

    private void OnDisable()
    {
        try
        {
            if (discord != null)
            {
                discord.Dispose();
                discord = null;
                Debug.Log("Discord disposed successfully.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error disposing Discord: {ex.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            if (discord != null)
            {
                discord.Dispose();
                discord = null;
                Debug.Log("Discord disposed on application quit.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during application quit Discord disposal: {ex.Message}");
        }
    }
}