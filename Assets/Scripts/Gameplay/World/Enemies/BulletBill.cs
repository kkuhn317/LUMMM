using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBill : EnemyAI
{
    [Header("Bullet Bill")]

    public bool rotateToMovement = true; // if true, rotate to movement direction (like a bullet bill)
    // This should be off for cannon balls

    public AudioClip appearSound;   // For bullet bills that come from offscreen
    public float appearSoundVolume = 0.2f;

    protected override void Start()
    {
        base.Start();

        if (rotateToMovement) {
            RotateToMovement();
        }
    }

    protected override void Update()
    {
        base.Update();

        // rotate to movement
        if ((objectState != ObjectState.knockedAway) && rotateToMovement) {
            RotateToMovement();
        } 
    }

    protected virtual void RotateToMovement() {
        float angle = Mathf.Atan2(realVelocity.y, realVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle + 180, Vector3.forward);
        GetComponentInChildren<SpriteRenderer>().flipY = !movingLeft;
    }

    // knock away on stomp
    protected override void hitByStomp(GameObject player)
    {
        var mario = player.GetComponent<MarioMovement>();
        mario.Jump();

        KnockAway(movingLeft, false, KnockAwayType.flip, new Vector2(1, 0));
        GetComponent<AudioSource>().Play();

        Vector3 popupPos = transform.position + Vector3.up * 0.5f;
        AwardStompComboReward(popupPos);
    }

    protected override void hitByGroundPound(MarioMovement player)
    {
        KnockAway(movingLeft, false, KnockAwayType.flip, new Vector2(1,0));
        Vector3 popupPos = transform.position + Vector3.up * 0.5f;
        AwardStompComboReward(popupPos);
    }

    // When we hit a wall, we should die
    protected override void onTouchWall(GameObject other){
        StartCoroutine(OnHitSurface());
    }

    public override void Land(GameObject other = null) {
        StartCoroutine(OnHitSurface());
    }

    // Wait until end of frame so it can detect player collision first
    // Example: Fast moving spikes at end of World of Spikes
    private IEnumerator OnHitSurface() {
        yield return new WaitForEndOfFrame();
        KnockAway(!movingLeft);
    }

    public override void OnBecameVisible()
    {
        // First time appearing?
        if (!appeared && appearSound != null) {
            GetComponent<AudioSource>().PlayOneShot(appearSound, appearSoundVolume);
        }
        base.OnBecameVisible();
    }

}
