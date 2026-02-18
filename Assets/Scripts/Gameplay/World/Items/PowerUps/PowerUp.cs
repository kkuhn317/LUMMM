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
    public bool is1Up = false;

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
        var mario = other.GetComponent<MarioMovement>();
        if (mario == null) return;

        if (mario.isTransforming)
            return;

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
        GetComponent<AudioSource>().Play();

        // Grant life using GameManager so UI updates and animations play
        // GameManager.Instance.AddLives();
        GameManager.Instance.GetSystem<LifeSystem>()?.AddLife();

        // Show a "1UP" popup using the global popup system
        if (ScorePopupManager.Instance != null)
        {
            Vector3 popupPos = transform.position + Vector3.up * 0.5f;
            var marioMovement = FindObjectOfType<MarioMovement>();
            ComboResult result = new ComboResult(RewardType.OneUp, PopupID.OneUp, 0);
            
            ScorePopupManager.Instance.ShowPopup(result, popupPos, marioMovement.powerupState);
        }

        Destroy(gameObject, 2);
    }

    private void HandleStarPower(Collider2D other)
    {
        var marioMovement = other.GetComponent<MarioMovement>();
        marioMovement.startStarPower(starTime);

        // GameManager.Instance.AddScorePoints(1000);
        GameManager.Instance.GetSystem<ScoreSystem>().AddScore(1000);
        
        if (ScorePopupManager.Instance != null)
        {
            Vector3 popupPos = transform.position + Vector3.up * 0.5f;

            ComboResult result = new ComboResult(
                RewardType.Score,
                PopupID.Score1000,
                1000
            );

            ScorePopupManager.Instance.ShowPopup(result, popupPos, marioMovement.powerupState);
        }

        if (starMusicOverride != null)
        {
            GameObject starSong = Instantiate(starMusicOverride);
            starSong.GetComponent<MusicOverride>()?.stopPlayingAfterTime(starTime);
        }
    }

    private void HandleStateTransition(Collider2D other)
    {
        var marioMovement = other.GetComponent<MarioMovement>();
        if (marioMovement == null) return;

        // GameManager.Instance.AddScorePoints(1000);
        GameManager.Instance.GetSystem<ScoreSystem>().AddScore(1000);

        // Show "1000" popup at the power-up position
        if (ScorePopupManager.Instance != null)
        {
            Vector3 popupPos = transform.position + Vector3.up * 0.5f;

            ComboResult result = new ComboResult(
                RewardType.Score,
                PopupID.Score1000,
                1000
            );

            ScorePopupManager.Instance.ShowPopup(result, popupPos, marioMovement.powerupState);
        }

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