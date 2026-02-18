using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central controller for all cheat side effects.
/// Listens to CheatFlags events and applies the actual cheat behaviors.
/// This is the ONLY place where cheat state changes should have side effects.
/// </summary>
public class CheatController : MonoBehaviour
{
    [Header("Powerup Prefabs")]
    [SerializeField] private GameObject tinyMarioPrefab;
    [SerializeField] private GameObject iceMarioPrefab;
    [SerializeField] private GameObject fireMarioPrefab;

    [Header("Tags")]
    [SerializeField] private string plushieTag = "Plushie";
    
    [Header("Debug")]
    [SerializeField] private bool logCheatChanges = true;

    private PlayerRegistry playerRegistry;
    private Dictionary<string, GameObject[]> cachedTaggedObjects = new();

    private void Awake()
    {
        playerRegistry = FindObjectOfType<PlayerRegistry>();
    }

    private void OnEnable()
    {
        // Subscribe to all cheat events
        CheatFlags.OnPlushiesChanged += HandlePlushies;
        CheatFlags.OnStartPowerupModeChanged += HandleStartPowerup;
        CheatFlags.OnInvincibilityChanged += HandleInvincibility;
        CheatFlags.OnAllAbilitiesChanged += HandleAllAbilities;
        CheatFlags.OnDarknessChanged += HandleDarkness;
        CheatFlags.OnRandomizerChanged += HandleRandomizer;
        CheatFlags.OnBetaModeChanged += HandleBetaMode;

        // Apply current states immediately
        RefreshAllCheats();
    }

    private void OnDisable()
    {
        CheatFlags.OnPlushiesChanged -= HandlePlushies;
        CheatFlags.OnStartPowerupModeChanged -= HandleStartPowerup;
        CheatFlags.OnInvincibilityChanged -= HandleInvincibility;
        CheatFlags.OnAllAbilitiesChanged -= HandleAllAbilities;
        CheatFlags.OnDarknessChanged -= HandleDarkness;
        CheatFlags.OnRandomizerChanged -= HandleRandomizer;
        CheatFlags.OnBetaModeChanged -= HandleBetaMode;
    }

    private void Start()
    {
        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>();
    }

    #region Cheat Handlers

    /// <summary>
    /// Toggles all plushie objects in the scene.
    /// Updates GlobalVariables for legacy systems and directly controls GameObject visibility.
    /// </summary>
    private void HandlePlushies(bool enabled)
    {
        // Update legacy global variables
        GlobalVariables.cheatPlushies = enabled;
        
        // Notify other systems via events
        GameEvents.TriggerCheatToggled("Plushies", enabled);
        
        // DIRECT SIDE EFFECT: Activate/deactivate all plushies
        GameObject[] plushies = GetCachedObjectsByTag(plushieTag);
        foreach (var plushie in plushies)
        {
            if (plushie != null)
                plushie.SetActive(enabled);
        }

        if (logCheatChanges)
            Debug.Log($"[Cheat] Plushies: {enabled}");
    }

    /// <summary>
    /// Changes the starting powerup for the player.
    /// Updates GlobalVariables, triggers events, and applies to existing players at level start.
    /// </summary>
    private void HandleStartPowerup(StartPowerupMode mode)
    {
        // Update legacy global variables
        GlobalVariables.cheatStartTiny = mode == StartPowerupMode.Tiny;
        GlobalVariables.cheatStartIce = mode == StartPowerupMode.Ice;
        GlobalVariables.cheatFlamethrower = mode == StartPowerupMode.Flamethrower;
        
        // Notify other systems
        GameEvents.TriggerStartPowerupChanged(mode);
        GameEvents.TriggerCheatToggled("StartPowerup", mode != StartPowerupMode.None);
        
        // DIRECT SIDE EFFECT: Apply to players already in the scene
        // Only at the very beginning of a level (first 0.5 seconds)
        if (Time.timeSinceLevelLoad < 0.5f)
        {
            ApplyPowerupToAllPlayers(mode);
        }

        if (logCheatChanges)
            Debug.Log($"[Cheat] Start Powerup: {mode}");
    }

    /// <summary>
    /// Toggles player invincibility.
    /// Note: You need to implement these methods in MarioMovement.cs
    /// </summary>
    private void HandleInvincibility(bool enabled)
    {
        GlobalVariables.cheatInvincibility = enabled;
        GameEvents.TriggerCheatToggled("Invincibility", enabled);
        
        // DIRECT SIDE EFFECT: Apply to all active players
        var players = GetActivePlayers();
        foreach (var player in players)
        {
            if (player != null)
            {
                player.EnableInvincibility(enabled);
            }
        }

        if (logCheatChanges)
            Debug.Log($"[Cheat] Invincibility: {enabled}");
    }

    /// <summary>
    /// Toggles all movement abilities (wall jump, ground pound, double jump, etc.)
    /// Note: You need to implement these methods in MarioMovement.cs
    /// </summary>
    private void HandleAllAbilities(bool enabled)
    {
        GlobalVariables.cheatAllAbilities = enabled;
        GameEvents.TriggerCheatToggled("AllAbilities", enabled);
        
        // DIRECT SIDE EFFECT: Apply to all active players
        var players = GetActivePlayers();
        foreach (var player in players)
        {
            if (player != null)
            {
                player.EnableAllAbilities(enabled);
            }
        }

        if (logCheatChanges)
            Debug.Log($"[Cheat] All Abilities: {enabled}");
    }

    /// <summary>
    /// Toggles darkness mode - makes the CircleTransition circle smaller and faster.
    /// CircleTransition reads GlobalVariables.cheatDarkness in Start(), so we just update the variable.
    /// For live toggling, we need to refresh any active CircleTransitions.
    /// </summary>
    private void HandleDarkness(bool enabled)
    {
        // Update legacy global variables - CircleTransition reads this
        GlobalVariables.cheatDarkness = enabled;
        
        // Notify other systems
        GameEvents.TriggerCheatToggled("Darkness", enabled);
        
        // Update any active CircleTransition components for mid-level toggling
        var circleTransitions = FindObjectsOfType<CircleTransition>();
        foreach (var transition in circleTransitions)
        {
            transition?.SetDarknessMode(enabled);
        }

        if (logCheatChanges)
            Debug.Log($"[Cheat] Darkness: {enabled}");
    }

    /// <summary>
    /// Toggles randomizer mode - enables Randomizer components and triggers randomization.
    /// </summary>
    private void HandleRandomizer(bool enabled)
    {
        // Update legacy global variables - Randomizer reads this in Start()
        GlobalVariables.cheatRandomizer = enabled;
        
        // Notify other systems
        GameEvents.TriggerCheatToggled("Randomizer", enabled);
        
        // DIRECT SIDE EFFECT: Enable/disable all Randomizer components
        var randomizers = FindObjectsOfType<Randomizer>();
        foreach (var randomizer in randomizers)
        {
            if (randomizer != null)
            {
                randomizer.enabled = enabled;
                
                // If enabling, trigger randomization immediately
                if (enabled)
                {
                    randomizer.RandomizeNow();
                }
            }
        }

        if (logCheatChanges)
            Debug.Log($"[Cheat] Randomizer: {enabled}");
    }

    /// <summary>
    /// Toggles beta/unfinished content visibility.
    /// </summary>
    private void HandleBetaMode(bool enabled)
    {
        GlobalVariables.cheatBetaMode = enabled;
        GameEvents.TriggerCheatToggled("BetaMode", enabled);

        if (logCheatChanges)
            Debug.Log($"[Cheat] Beta Mode: {enabled}");
    }

    #endregion

    #region Powerup Helpers

    /// <summary>
    /// Applies the selected start powerup cheat to all active players.
    /// </summary>
    private void ApplyPowerupToAllPlayers(StartPowerupMode mode)
    {
        var players = GetActivePlayers();
        
        foreach (var player in players)
        {
            if (player != null)
                ApplyPowerupToPlayer(player, mode);
        }
    }

    /// <summary>
    /// Applies a specific powerup cheat to an individual player.
    /// </summary>
    private void ApplyPowerupToPlayer(MarioMovement player, StartPowerupMode mode)
    {
        if (player == null) return;

        switch (mode)
        {
            case StartPowerupMode.Tiny:
                if (player.powerupState != PowerStates.PowerupState.tiny)
                    player.ChangePowerup(tinyMarioPrefab);
                break;
            case StartPowerupMode.Ice:
                player.ChangePowerup(iceMarioPrefab);
                break;
            case StartPowerupMode.Flamethrower:
                player.ChangePowerup(fireMarioPrefab);
                break;
            // None mode does nothing
        }
    }

    #endregion

    #region Player Helpers

    /// <summary>
    /// Gets all active players in the scene.
    /// Tries PlayerRegistry first, falls back to FindObjectsOfType.
    /// </summary>
    private List<MarioMovement> GetActivePlayers()
    {
        List<MarioMovement> activePlayers = new();

        // Try to get players from registry first (more efficient)
        if (playerRegistry != null)
        {
            var players = playerRegistry.GetAllPlayers();
            foreach (var player in players)
            {
                if (player != null)
                    activePlayers.Add(player);
            }
        }
        
        // Fallback to finding them manually
        if (activePlayers.Count == 0)
        {
            activePlayers.AddRange(FindObjectsOfType<MarioMovement>());
        }

        return activePlayers;
    }

    #endregion

    #region Cache Helpers

    /// <summary>
    /// Gets all GameObjects with the specified tag, using a cache to avoid repeated Find calls.
    /// </summary>
    private GameObject[] GetCachedObjectsByTag(string tag)
    {
        if (!cachedTaggedObjects.ContainsKey(tag) || 
            cachedTaggedObjects[tag] == null || 
            cachedTaggedObjects[tag].Length == 0)
        {
            cachedTaggedObjects[tag] = GameObject.FindGameObjectsWithTag(tag);
        }
        return cachedTaggedObjects[tag];
    }

    /// <summary>
    /// Clears the cache for a specific tag. Call this when objects are created/destroyed.
    /// </summary>
    public void ClearCache(string tag)
    {
        if (cachedTaggedObjects.ContainsKey(tag))
            cachedTaggedObjects.Remove(tag);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Manually refreshes all cheat states.
    /// Useful after loading a new scene or respawning.
    /// </summary>
    public void RefreshAllCheats()
    {
        HandlePlushies(CheatFlags.Plushies);
        HandleStartPowerup(CheatFlags.StartPowerup);
        HandleInvincibility(CheatFlags.Invincibility);
        HandleAllAbilities(CheatFlags.AllAbilities);
        HandleDarkness(CheatFlags.Darkness);
        HandleRandomizer(CheatFlags.Randomizer);
        HandleBetaMode(CheatFlags.BetaMode);
    }

    /// <summary>
    /// Applies the start powerup cheat to a specific player.
    /// Useful for checkpoint respawns.
    /// </summary>
    public void ApplyStartPowerupToPlayer(MarioMovement player)
    {
        if (CheatFlags.StartPowerup != StartPowerupMode.None)
        {
            ApplyPowerupToPlayer(player, CheatFlags.StartPowerup);
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        // Make sure plushies are disabled when the controller is destroyed
        if (CheatFlags.Plushies)
        {
            var plushies = GetCachedObjectsByTag(plushieTag);
            foreach (var plushie in plushies)
            {
                if (plushie != null)
                    plushie.SetActive(false);
            }
        }
    }

    #endregion
}