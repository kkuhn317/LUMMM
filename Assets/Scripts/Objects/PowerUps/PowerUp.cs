using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerupState = PowerStates.PowerupState;

public class PowerUp : ObjectPhysics
{
    [Header("Power Up")]
    public GameObject newMarioState;
    public int powerLevel = 1;
    public float starTime = -1;
    public GameObject starMusicOverride;

    [Header("1UP")]
    public bool is1Up = false;
    public GameObject oneupSpritePrefab; // Reference to the sprite to move up
    public float upSpeed = 2.0f; // Speed at which the sprite moves up

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        enabled = false;

    }

    void OnBecameVisible() {
        
        enabled = true;
    }


    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Player") {
            if (is1Up) {
                GameManager.Instance.AddLives();
                Destroy(gameObject);

                if (oneupSpritePrefab != null)
                {
                    GameObject oneupSprite = Instantiate(oneupSpritePrefab, transform.position, Quaternion.identity);
                    Rigidbody2D upSpriteRigidbody = oneupSprite.GetComponent<Rigidbody2D>();
                    upSpriteRigidbody.velocity = Vector2.up * upSpeed;

                    Destroy(oneupSprite, 1.0f);
                }
                
                return;
            }
            if (starTime > 0) {
                GameManager.Instance.AddScorePoints(1000);
                other.GetComponent<MarioMovement>().startStarPower(starTime);
                GameObject starSong = Instantiate(starMusicOverride);
                starSong.GetComponent<MusicOverride>().stopPlayingAfterTime(starTime);
            }
            if (newMarioState) {
                GameManager.Instance.AddScorePoints(1000);
                MarioMovement player = other.GetComponent<MarioMovement>();
                PowerupState playerState = player.powerupState;
                

                if (canGetPowerup(playerState)) 
                    other.GetComponent<MarioMovement>().ChangePowerup(newMarioState);
            }
            GetComponent<AudioSource>().Play();
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
            Destroy(gameObject, 2);
        }
    }

    private bool canGetPowerup(PowerupState state) {
        var newPowerState = newMarioState.GetComponent<MarioMovement>().powerupState;

        // Prevent redundant transformations for the same power-up type
        if (state == newPowerState)
            return false;

        // Allow transformation to tiny regardless of current state
        if (newPowerState == PowerupState.tiny)
            return true;

        // Small states can always take a power-up
        if (PowerStates.IsSmall(state))
            return true;

        // Allow transformations if the new power-up level is higher
        return powerLevel >= 2 || state != PowerupState.power;
    }

}
