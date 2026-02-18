using UnityEngine;
using UnityEngine.SceneManagement;
using static PowerStates;

public struct CutsceneContext
{
    // Core global stuff
    public GameManagerRefactored gameManager;
    public Scene scene;

    public MarioMovement mainPlayer;

    // Useful player data at the moment of the trigger
    public Vector3 playerPosition;
    public PowerupState powerupState;
    public bool hasStarPower;
    public bool isDead;
}