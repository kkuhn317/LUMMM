using System.Collections;
using UnityEngine;

public class LifeSystem : MonoBehaviour
{
    public int CurrentLives => GlobalVariables.lives;
    public int maxLives = 99;
    
    public void AddLife()
    {
        if (CurrentLives < maxLives)
        {
            GlobalVariables.lives++;
            GameEvents.TriggerExtraLifeGained();
            GameEvents.TriggerLivesChanged(GlobalVariables.lives);
        }
    }

    public bool RemoveLife()
    {
        bool gameOver = RemoveLifeSilent();
        GameEvents.TriggerLivesChanged(GlobalVariables.lives);
        return gameOver;
    }

    public bool RemoveLifeSilent()
    {
        if (GlobalVariables.infiniteLivesMode)
            return false;

        GlobalVariables.lives = Mathf.Max(0, GlobalVariables.lives - 1);
        return GlobalVariables.lives <= 0;
    }
    
    public bool IsGameOver()
    {
        return GlobalVariables.lives <= 0 && !GlobalVariables.infiniteLivesMode;
    }
}