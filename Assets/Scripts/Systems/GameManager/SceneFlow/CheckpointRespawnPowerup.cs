using System.Collections;
using UnityEngine;

public class CheckpointRespawnPowerup : MonoBehaviour
{
    [SerializeField] private StartPowerupMode respawnPowerup = StartPowerupMode.Tiny;

    private PlayerRegistry playerRegistry;
    private CheatController cheatController;

    private void Awake()
    {
        playerRegistry = FindObjectOfType<PlayerRegistry>();
        cheatController = FindObjectOfType<CheatController>();
    }

    public void Apply()
    {
        if (playerRegistry == null) playerRegistry = FindObjectOfType<PlayerRegistry>();
        if (cheatController == null) cheatController = FindObjectOfType<CheatController>();

        StartCoroutine(ApplyNextFrame());
    }

    private IEnumerator ApplyNextFrame()
    {
        yield return null;

        foreach (var player in playerRegistry.GetAllPlayers())
        {
            if (player != null)
                cheatController.ForceApplyPowerupToPlayer(player, respawnPowerup);
        }
    }
}