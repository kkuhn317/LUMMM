using UnityEngine;

/// <summary>
/// Special flagpole for the Spikes level. Overrides reward granting to
/// require the GiantThwomp to be defeated first. On destruction, spawns
/// a dead Mario at the player's last position and shows the broken pole.
/// </summary>
public class SpikesFlagPole : Flag, IDestructible
{
    [Header("Spikes Level")]
    public GiantThwomp giantThwomp;
    public GameObject  brokenFlagPole;

    [Tooltip("DeathCause used when the flagpole is destroyed while Mario is on it.")]
    public DeathCause deathCause;

    protected override void Start()
    {
        base.Start();
        brokenFlagPole.SetActive(false);
    }

    /// <summary>Only grant reward if the Thwomp is already defeated.</summary>
    protected override void OnGrantReward(Collider2D other, MarioCore mario)
    {
        bool thwompDefeated = giantThwomp == null || giantThwomp.CanBeDefeatedNow;
        if (!thwompDefeated) return;

        base.OnGrantReward(other, mario);
    }

    public void HidePuppets() => Slide.HideAllPuppets();

    public void OnDestruction()
    {
        // Hide all puppets immediately
        Slide.HideAllPuppets();

        // Kill all players currently sliding on the pole
        foreach (var ps in Slide.SlidingPlayers)
        {
            if (ps.Mario == null) continue;

            // Destroy the cutscene puppet
            if (ps.CutsceneMarioInstance != null)
                Destroy(ps.CutsceneMarioInstance);

            // Reactivate the real Mario before killing — puppet hid them
            ps.Mario.gameObject.SetActive(true);

            if (!ps.Mario.State.IsDead)
                ps.Mario.Combat.ToDead(deathCause);
        }

        // Activate broken pole before destroying — Destroy is deferred to end of frame
        if (brokenFlagPole != null)
            brokenFlagPole.SetActive(true);

        Destroy(gameObject);
    }
}