using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerConnectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRegistry registry;

    [Header("Behavior")]
    [SerializeField] private bool autoFindRegistry = true;
    [SerializeField] private bool syncExistingOnEnable = true;

    private PlayerInputManager pim;

    public static event Action<int> OnPlayerUnregistered;
    public static event Action<int> OnPlayerDeviceLost;
    public static event Action<int> OnPlayerDeviceRegained;

    private MethodInfo unregisterByIndexMethod;

    private void Awake()
    {
        pim = PlayerInputManager.instance;

        if (registry == null && autoFindRegistry)
            registry = FindObjectOfType<PlayerRegistry>(true);

        CacheUnregisterByIndexMethod();
    }

    private void OnEnable()
    {
        if (pim == null) pim = PlayerInputManager.instance;

        if (pim != null)
        {
            pim.onPlayerJoined += HandlePlayerJoined;
            pim.onPlayerLeft += HandlePlayerLeft;
        }

        if (syncExistingOnEnable)
            SyncExistingPlayers();
    }

    private void OnDisable()
    {
        if (pim != null)
        {
            pim.onPlayerJoined -= HandlePlayerJoined;
            pim.onPlayerLeft -= HandlePlayerLeft;
        }
    }

    private void CacheUnregisterByIndexMethod()
    {
        if (registry == null) return;

        unregisterByIndexMethod = registry.GetType().GetMethod(
            "UnregisterPlayer",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null
        );
    }

    private void EnsureRegistry()
    {
        if (registry == null && autoFindRegistry)
        {
            registry = FindObjectOfType<PlayerRegistry>(true);
            CacheUnregisterByIndexMethod();
        }
    }

    private void SyncExistingPlayers()
    {
        EnsureRegistry();
        if (registry == null) return;

        var existing = FindObjectsOfType<PlayerInput>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            TryRegister(existing[i]);
            HookDeviceCallbacks(existing[i]);
        }
    }

    private void HandlePlayerJoined(PlayerInput playerInput)
    {
        TryRegister(playerInput);
        HookDeviceCallbacks(playerInput);
    }

    private void HandlePlayerLeft(PlayerInput playerInput)
    {
        UnhookDeviceCallbacks(playerInput);
        TryUnregister(playerInput);

        OnPlayerUnregistered?.Invoke(playerInput.playerIndex);
    }

    private void HookDeviceCallbacks(PlayerInput playerInput)
    {
        if (playerInput == null) return;

        // Evita doble-hook
        playerInput.onDeviceLost -= HandleDeviceLost;
        playerInput.onDeviceRegained -= HandleDeviceRegained;

        playerInput.onDeviceLost += HandleDeviceLost;
        playerInput.onDeviceRegained += HandleDeviceRegained;
    }

    private void UnhookDeviceCallbacks(PlayerInput playerInput)
    {
        if (playerInput == null) return;

        playerInput.onDeviceLost -= HandleDeviceLost;
        playerInput.onDeviceRegained -= HandleDeviceRegained;
    }

    private void HandleDeviceLost(PlayerInput playerInput)
    {
        OnPlayerDeviceLost?.Invoke(playerInput.playerIndex);
    }

    private void HandleDeviceRegained(PlayerInput playerInput)
    {
        OnPlayerDeviceRegained?.Invoke(playerInput.playerIndex);
    }

    private void TryRegister(PlayerInput playerInput)
    {
        if (playerInput == null) return;

        // PlayerInput can report -1 briefly (not joined yet), which breaks registry indexing.
        if (playerInput.playerIndex < 0)
        {
            Debug.LogWarning($"PlayerConnectionManager.TryRegister skipped PlayerInput with invalid index: {playerInput.playerIndex}");
            return;
        }

        EnsureRegistry();
        if (registry == null) return;

        var movement = playerInput.GetComponent<MarioMovement>();
        if (movement == null) return;

        var current = registry.GetPlayer(playerInput.playerIndex);
        if (current == movement) return;

        registry.RegisterPlayer(movement, playerInput.playerIndex);
    }

    private void TryUnregister(PlayerInput playerInput)
    {
        if (playerInput == null) return;

        EnsureRegistry();
        if (registry == null) return;

        // Preferido: UnregisterPlayer(int)
        if (unregisterByIndexMethod != null)
        {
            try
            {
                unregisterByIndexMethod.Invoke(registry, new object[] { playerInput.playerIndex });
                return;
            }
            catch
            {
                // fall through
            }
        }

        // Fallback: UnregisterPlayer(MarioMovement)
        var movement = playerInput.GetComponent<MarioMovement>();
        if (movement != null)
            registry.UnregisterPlayer(movement);
    }
}