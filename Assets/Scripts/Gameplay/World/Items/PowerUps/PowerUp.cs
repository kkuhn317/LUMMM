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
    public PowerUpData powerUpData;
    public float starTime = -1;
    public GameObject starMusicOverride;
    public PowerUpType powerUpType = PowerUpType.Destroy;
    public float temporalInactiveTime = 5.0f;
    public bool is1Up = false;

    // Cached components
    private AudioSource audioSource;
    private Collider2D _collider;
    private SpriteRenderer _renderer;

    // Cached values read once from powerUpData
    private PowerupState _newPowerupState;
    private string _newPowerupType;
    private bool _prefabCached;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();
        enabled = false;
        CachePrefabData();
    }

    void OnBecameVisible()
    {
        enabled = true;
    }

    // Read the target prefab's powerup data once, instead of calling
    // GetComponent every time canGetPowerup is evaluated.
    private void CachePrefabData()
    {
        if (_prefabCached || powerUpData == null) return;

        _newPowerupState = powerUpData.PowerupState;
        _newPowerupType  = powerUpData.PowerupType ?? "";

        _prefabCached = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
            HandlePowerUp(other);
    }

    private void HandlePowerUp(Collider2D other)
    {
        var mario = other.GetComponentInParent<MarioCore>();
        if (mario == null) return;
        if (mario.State.IsTransforming) return;

        if (is1Up)
        {
            Handle1Up(mario);
            return;
        }

        if (starTime > 0)
            HandleStarPower(mario);

        if (powerUpData != null)
            HandleStateTransition(mario);

        audioSource.Play();
        _collider.enabled = false;
        _renderer.enabled = false;

        if (powerUpType == PowerUpType.Destroy)
            Destroy(gameObject, 2);
        else if (powerUpType == PowerUpType.Temporal)
            StartCoroutine(ReactivateAfterDelay());
    }

    private void Handle1Up(MarioCore mario)
    {
        _collider.enabled = false;
        _renderer.enabled = false;
        audioSource.Play();

        GameManager.Instance.GetSystem<LifeSystem>()?.AddLife();

        if (ScorePopupManager.Instance != null)
        {
            ScorePopupManager.Instance.ShowPopup(
                new ComboResult(RewardType.OneUp, PopupID.OneUp, 0),
                transform.position + Vector3.up * 0.5f,
                mario.State.PowerupState);
        }

        Destroy(gameObject, 2);
    }

    private void HandleStarPower(MarioCore mario)
    {
        mario.Combat.StartStarPower(starTime);
        GameManager.Instance.GetSystem<ScoreSystem>().AddScore(1000);

        if (ScorePopupManager.Instance != null)
        {
            ScorePopupManager.Instance.ShowPopup(
                new ComboResult(RewardType.Score, PopupID.Score1000, 1000),
                transform.position + Vector3.up * 0.5f,
                mario.State.PowerupState);
        }

        if (starMusicOverride != null)
        {
            GameObject starSong = Instantiate(starMusicOverride);
            starSong.GetComponent<MusicOverride>()?.stopPlayingAfterTime(starTime);
        }
    }

    private void HandleStateTransition(MarioCore mario)
    {
        bool can = canGetPowerup(mario.State.PowerupState, mario.State.CurrentPowerupType);

        if (can)
        {
            if (mario.Powerup == null)
            {
                Debug.LogWarning("[PowerUp] mario.Powerup is null!");
                return;
            }
            mario.Powerup.ChangePowerup(powerUpData);
        }

        // Always award score and show popup, even for duplicate powerups
        GameManager.Instance.GetSystem<ScoreSystem>().AddScore(1000);

        if (ScorePopupManager.Instance != null)
        {
            ScorePopupManager.Instance.ShowPopup(
                new ComboResult(RewardType.Score, PopupID.Score1000, 1000),
                transform.position + Vector3.up * 0.5f,
                mario.State.PowerupState);
        }
    }

    private IEnumerator ReactivateAfterDelay()
    {
        yield return new WaitForSeconds(temporalInactiveTime);
        _collider.enabled = true;
        _renderer.enabled = true;
        enabled = true;
    }

    private bool canGetPowerup(PowerupState currentState, string currentType)
    {
        CachePrefabData(); // no-op if already cached

        // Prevent picking up the exact same powerup again
        if (currentState == _newPowerupState && currentType == _newPowerupType)
            return false;

        // Always allow tiny transformation
        if (_newPowerupState == PowerupState.tiny)
            return true;

        // Small/tiny Mario can always pick up any powerup
        if (PowerStates.IsSmall(currentState))
            return true;

        // Big Mario: allow if upgrading to power, or not already at power tier
        return _newPowerupState == PowerupState.power || currentState != PowerupState.power;
    }
}