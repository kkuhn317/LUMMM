using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRegistry : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset playerInputActions;

    // Changed from MarioMovement to MarioCore
    private List<MarioCore> players = new List<MarioCore>();

    public InputActionAsset GetPlayerInputActions() => playerInputActions;

    public void RegisterPlayer(MarioCore player, int playerIndex)
    {
        if (playerIndex < 0)
        {
            Debug.LogWarning($"PlayerRegistry.RegisterPlayer ignored invalid index: {playerIndex}");
            return;
        }
        
        while (players.Count <= playerIndex) players.Add(null);
        players[playerIndex] = player;

        // Ensure GameEvents is updated to accept MarioCore if necessary
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

    public void UnregisterPlayer(MarioCore player)
    {
        if (player == null) return;

        int idx = players.IndexOf(player);
        if (idx >= 0) players[idx] = null;
    }

    public MarioCore GetPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return null;
        return players[playerIndex];
    }

    public MarioCore[] GetAllPlayers()
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