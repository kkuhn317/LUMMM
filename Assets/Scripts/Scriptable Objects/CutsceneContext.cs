using UnityEngine;
using UnityEngine.SceneManagement;
using static PowerStates;

public struct CutsceneContext
{
    // Core global stuff
    public GameManager gameManager;
    public Scene scene;

    public MarioMovement mainPlayer;

    // Datos útiles del jugador en el momento del trigger
    public Vector3 playerPosition;
    public PowerupState powerupState;
    public bool hasStarPower;
    public bool isDead;
}