using UnityEngine;

/// <summary>
/// Fire Mario's fireball ability.
///
/// Faithful 1:1 port of the original FirePower.cs.
/// The only changes from the source:
///   marioMovement.facingRight    → State.FacingRight
///   marioMovement.carrying       → State.Carrying
///   marioMovement.groundPounding → State.GroundPounding
///   GetComponent<Animator/AudioSource> retained inline — called infrequently,
///   no need to cache. MarioAbility.Initialize(core) wires Core and State.
///
/// Fires MarioEvents.FireFireballShot() so MarioAudio / MarioAnimatorController
/// can react via the event bus without polling.
/// </summary>
public class FirePower : MarioAbility
{
    public GameObject fireballObj;
    public int        fireballs    = 0;
    public int        fireballsMax = 2;
    public Vector2    shootOffset;
    public AudioClip  shootSound;

    // Granted by the current powerup. Fire/Ice now live as data on ONE Big prefab, so the
    // component is always present but only shoots when a powerup grants it a projectile.
    private bool _granted;

    /// <summary>
    /// Configure the shooter from powerup data. Pass a projectile + sound to grant it
    /// (Fire = fireball, Ice = iceball), or null to revoke (plain Big). Called by MarioPowerup.
    /// </summary>
    public void Configure(GameObject projectile, AudioClip sound)
    {
        _granted = projectile != null;
        if (!_granted) return;

        fireballObj = projectile;
        shootSound  = sound;
        fireballs   = 0;   // fresh ammo on (re)grant
    }

    // ─── Hooks ───────────────────────────────────────────────────────────────

    public override void onShootPressed()
    {
        if (!_granted) return;
        if (!State.Carrying && !State.GroundPounding)
            ShootProjectile(isLeft: !State.FacingRight, explicitDirection: false);
    }

    public override void onSpinPressed()
    {
        if (!_granted) return;
        ShootSpinningFireballs();
    }

    // ─── Fireball lifecycle ───────────────────────────────────────────────────

    /// <summary>Called by the Fireball script when it hits something or times out.</summary>
    public void onFireballDestroyed()
    {
        fireballs--;
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void ShootSpinningFireballs()
    {
        bool facingLeft = !State.FacingRight;
        ShootProjectile(facingLeft,  explicitDirection: true);
        ShootProjectile(!facingLeft, explicitDirection: true);
    }

    private void ShootProjectile(bool isLeft, bool explicitDirection)
    {
        if (fireballs >= fireballsMax && !GlobalVariables.cheatFlamethrower) return;

        if (!explicitDirection)
            isLeft = !State.FacingRight;

        int       directionInt    = isLeft ? -1 : 1;
        Vector3   spawnPos        = Core.transform.position
                                  + new Vector3(shootOffset.x * directionInt, shootOffset.y, 0f);

        GameObject    newFireball      = Instantiate(fireballObj, spawnPos, Quaternion.identity);
        Fireball      fireballScript   = newFireball.GetComponent<Fireball>();
        ObjectPhysics fireballPhysics  = newFireball.GetComponent<ObjectPhysics>();

        fireballScript.firePowerScript  = this;
        fireballPhysics.movingLeft      = isLeft;

        fireballs++;

        GetComponent<Animator>().SetTrigger("shoot");
        GetComponent<AudioSource>().PlayOneShot(shootSound);

        MarioEvents.FireFireballShot(PlayerIndex);
    }
}