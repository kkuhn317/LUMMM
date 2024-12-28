using System.Collections;
using UnityEngine;
using PowerupState = PowerStates.PowerupState;

public enum PowerUpType
{
    Destroy,
    Temporal
}

public class PowerUp : ObjectPhysics
{
    [Header("Power Up")]
    public GameObject newMarioState;
    public float starTime = -1;
    public GameObject starMusicOverride;
    public PowerUpType powerUpType = PowerUpType.Destroy;
    public float temporalInactiveTime = 5.0f; // Time to remain inactive

    [Header("1UP")]
    public bool is1Up = false;
    public GameObject oneupSpritePrefab; // Reference to the sprite to move up
    public float upSpeed = 2.0f; // Speed at which the sprite moves up
    public AudioClip extraLife;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        enabled = false;
    }

    void OnBecameVisible()
    {
        enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            HandlePowerUp(other);
        }
    }

    private void HandlePowerUp(Collider2D other)
    {
        if (is1Up)
        {
            Handle1Up();
            return;
        }

        if (starTime > 0)
        {
            HandleStarPower(other);
        }

        if (newMarioState)
        {
            HandleStateTransition(other);
        }

        GetComponent<AudioSource>().Play();
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        if (powerUpType == PowerUpType.Destroy)
        {
            Destroy(gameObject, 2);
        }
        else if (powerUpType == PowerUpType.Temporal)
        {
            StartCoroutine(ReactivateAfterDelay());
        }
    }

    private void Handle1Up()
    {
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<AudioSource>().PlayOneShot(extraLife);
        GameManager.Instance.AddLives();
        Destroy(gameObject, 2);

        if (oneupSpritePrefab != null)
        {
            GameObject oneupSprite = Instantiate(oneupSpritePrefab, transform.position, Quaternion.identity);
            Rigidbody2D upSpriteRigidbody = oneupSprite.GetComponent<Rigidbody2D>();
            upSpriteRigidbody.velocity = Vector2.up * upSpeed;

            Destroy(oneupSprite, 1.0f);
        }
    }

    private void HandleStarPower(Collider2D other)
    {
        GameManager.Instance.AddScorePoints(1000);
        other.GetComponent<MarioMovement>().startStarPower(starTime);

        if (starMusicOverride != null)
        {
            GameObject starSong = Instantiate(starMusicOverride);
            starSong.GetComponent<MusicOverride>()?.stopPlayingAfterTime(starTime);
        }
    }

    private void HandleStateTransition(Collider2D other)
    {
        GameManager.Instance.AddScorePoints(1000);
        var marioMovement = other.GetComponent<MarioMovement>();
        if (marioMovement == null) return;

        if (canGetPowerup(marioMovement.powerupState, marioMovement.currentPowerupType))
        {
            marioMovement.ChangePowerup(newMarioState);
        }
    }

    private IEnumerator ReactivateAfterDelay()
    {
        yield return new WaitForSeconds(temporalInactiveTime);

        // Reactivate the power-up
        GetComponent<Collider2D>().enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        enabled = true;
    }

    private bool canGetPowerup(PowerupState currentState, string currentType)
    {
        var newPowerState = newMarioState.GetComponent<MarioMovement>().powerupState;
        var newPowerType = newMarioState.GetComponent<MarioMovement>().currentPowerupType;

        // Prevent redundant transformations for the same power-up type
        if (currentState == newPowerState && currentType == newPowerType)
            return false;

        // Allow transformation to tiny regardless of current state
        if (newPowerState == PowerupState.tiny)
            return true;

        // Small states can always take a power-up
        if (PowerStates.IsSmall(currentState))
            return true;

        // Allows transformation if the new power-up is a Power state or if the current state is not already Power
        return newPowerState == PowerupState.power || currentState != PowerupState.power;
    }
}