using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRegistry : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset playerInputActions;

    private List<MarioMovement> players = new List<MarioMovement>();

    public InputActionAsset GetPlayerInputActions() => playerInputActions;

    public void RegisterPlayer(MarioMovement player, int playerIndex)
    {
        // playerIndex can be -1 when PlayerInput hasn't joined/initialized yet
        if (playerIndex < 0)
        {
            Debug.LogWarning($"PlayerRegistry.RegisterPlayer ignored invalid index: {playerIndex}");
            return;
        }
        
        while (players.Count <= playerIndex) players.Add(null);
        players[playerIndex] = player;

        GameEvents.TriggerPlayerRegistered(player, playerIndex);
    }

    public int ActivePlayerCount
    {
        get
        {
            int count = 0;
            foreach (var p in players)
                if (p != null) count++;
            return count;
        }
    }
    
    public void UnregisterPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        players[playerIndex] = null;
    }

    public void UnregisterPlayer(MarioMovement player)
    {
        if (player == null) return;

        int idx = players.IndexOf(player);
        if (idx >= 0) players[idx] = null;
    }

    public MarioMovement GetPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return null;
        return players[playerIndex];
    }

    public MarioMovement[] GetAllPlayers()
    {
        return players.ToArray();
    }

    public GameObject[] GetAllPlayerObjects()
    {
        List<GameObject> activePlayers = new List<GameObject>();
        foreach (var player in players)
        {
            if (player != null) activePlayers.Add(player.gameObject);
        }
        return activePlayers.ToArray();
    }

    public int PlayerCount => players.Count;
}