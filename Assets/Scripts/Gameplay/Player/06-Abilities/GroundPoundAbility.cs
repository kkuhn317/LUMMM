using UnityEngine;

/// <summary>
/// Grants Mario the ability to ground pound.
/// Sets State.CanGroundPound when enabled/disabled.
///
/// The actual ground pound physics live in the three GroundPound FSM states.
/// This ability just:
/// - Unlocks the flag so CheckTransitions in Rise/Fall/MidairSpin can see it
/// - Provides the particle spawn hook on landing
/// - Owns the GroundPoundParticles prefab reference
/// </summary>
public class GroundPoundAbility : MarioAbility
{
    [Header("Effects")]
    public GameObject GroundPoundParticlesPrefab;

    public override void Initialize(MarioCore core)
    {
        base.Initialize(core);
        State.CanGroundPound = enabled;
    }

    private void OnEnable()
    {
        if (Core != null) State.CanGroundPound = true;
        MarioEvents.OnGroundPoundLanded += OnGroundPoundLanded;
    }

    private void OnDisable()
    {
        if (Core != null) State.CanGroundPound = false;
        MarioEvents.OnGroundPoundLanded -= OnGroundPoundLanded;
    }

    private void OnGroundPoundLanded(int playerIndex, GameObject hitObject)
    {
        if (playerIndex != PlayerIndex) return;
        if (GroundPoundParticlesPrefab == null) return;

        float colliderHalfHeight = Core.Collider.bounds.size.y / 2f;
        Vector3 spawnPos = new(
            Core.transform.position.x,
            Core.transform.position.y - colliderHalfHeight,
            Core.transform.position.z
        );
        Instantiate(GroundPoundParticlesPrefab, spawnPos, Quaternion.identity);
    }
}
