using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerupState = PowerStates.PowerupState;

public class Key : MonoBehaviour
{
    public GameObject particle;

    private MarioCore _playerCore;

    private bool collectable = false;
    private bool collected   = false;

    private Vector3 actualposition;

    [Header("Collected from Enemy")]
    public bool fromEnemy = false;
    public AnimationCurve riseCurve  = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve updownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AudioClip keyRiseSound;
    public AudioClip keyToPlayerSound;

    [Header("When collected key")]
    public bool shouldPlayAnimationAndAudio = false;
    private float notMovingTime = 0f;
    public float timeThreshold = 5f;
    public string animationParameterTrigger;
    public AudioClip playerAudioClip;

    [Header("Bounce")]
    public float bounceheight = 0.5f;
    public float bounceSpeed  = 0.5f;
    private float bounceOffset = 0;

    void Start()
    {
        actualposition = transform.position;
        
        // Fetch the player reference up front so goToMario() can fly toward them
        var registry = GameManager.Instance?.GetSystem<PlayerRegistry>();
        if (registry != null)
            _playerCore = registry.GetPlayer(0);


        if (fromEnemy)
            StartCoroutine(goToMario());
        else
            collectable = true;
    }

    void Update()
    {
        if (!collected) return;

        // Refresh player reference every frame in case Mario transformed —
        // the old MarioCore gets destroyed and replaced with a new one.
        if (_playerCore == null || !_playerCore.gameObject.activeInHierarchy)
        {
            var registry = GameManager.Instance?.GetSystem<PlayerRegistry>();
            if (registry != null)
            {
                var fresh = registry.GetPlayer(0);
                if (fresh != null) _playerCore = fresh;
            }
        }

        followPlayer();

        if (_playerCore == null) return;

        // isMoving equivalent: check horizontal velocity
        bool isMoving = Mathf.Abs(_playerCore.Rb.velocity.x) > 0.1f;

        if (isMoving)
        {
            notMovingTime = 0f;
            shouldPlayAnimationAndAudio = false;
        }
        else if (shouldPlayAnimationAndAudio)
        {
            notMovingTime += Time.deltaTime;
            if (notMovingTime >= timeThreshold)
            {
                PlayAudioAndAnimation();
                shouldPlayAnimationAndAudio = false;
            }
        }
    }

    void PlayAudioAndAnimation()
    {
        if (_playerCore == null) return;

        var playerAnimator = _playerCore.GetComponentInChildren<Animator>();
        if (playerAnimator != null)
            playerAnimator.SetTrigger(animationParameterTrigger);
        else
            Debug.LogError("Player Animator component not found.");

        var playerAudioSource = _playerCore.GetComponent<AudioSource>();
        if (playerAudioSource != null && playerAudioClip != null)
            playerAudioSource.PlayOneShot(playerAudioClip);
        else
            Debug.LogError("Player AudioSource component or audio clip not found.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || collected || !collectable) return;

        // MarioCore is on the ROOT, not the child collider
        var core = other.GetComponent<MarioCore>() ?? other.GetComponentInParent<MarioCore>();
        if (core == null) return;

        actualposition = transform.position;
        _playerCore    = core;
        collected      = true;

        var keys = GameManager.Instance.GetSystem<KeyInventorySystem>();
        keys?.AddKey(gameObject);

        GetComponent<AudioSource>().Play();
        spawnParticles();

        var parentWingedObject = GetComponentInParent<WingedObject>();
        parentWingedObject?.WingsFall();
    }

    void spawnParticles()
    {
        int[] vertdirections  = new int[] { -1, 0, 1 };
        int[] horizdirections = new int[] { -1, 0, 1 };

        for (int i = 0; i < vertdirections.Length; i++)
        {
            for (int j = 0; j < horizdirections.Length; j++)
            {
                if (vertdirections[i] == 0 && horizdirections[j] == 0) continue;

                float distance = (vertdirections[i] != 0 && horizdirections[j] != 0) ? 0.7f : 1f;
                Vector3 startoffset = new Vector3(horizdirections[i] * distance, vertdirections[j] * distance, 0);

                GameObject newParticle = Instantiate(particle, transform.position + startoffset, Quaternion.identity);
                var star = newParticle.GetComponent<StarMoveOutward>();
                if (star != null)
                {
                    star.direction = new Vector2(vertdirections[i], horizdirections[j]);
                    star.speed     = 2f;
                }
            }
        }
    }

    void followPlayer()
    {
        if (_playerCore == null) return;

        bool    facingRight = _playerCore.State.FacingRight;
        Vector3 offset      = new Vector3(facingRight ? -1 : 1, 0, 0);

        if (PowerStates.IsBig(_playerCore.State.PowerupState))
            offset += new Vector3(0, -0.5f, 0);

        Vector3 finalLocation = _playerCore.transform.position + offset;

        float distance = Vector2.Distance(actualposition, finalLocation);
        float speed    = Mathf.Pow(distance, 2) * 2f;

        actualposition = Vector2.MoveTowards(actualposition, finalLocation, speed * Time.deltaTime);

        bounceOffset += Time.deltaTime * bounceSpeed;
        if (bounceOffset > 1) bounceOffset = 0;

        transform.position = actualposition + new Vector3(0, Mathf.Sin(bounceOffset * Mathf.PI) * bounceheight, 0);
    }

    IEnumerator goToMario()
    {
        GetComponent<AudioSource>().PlayOneShot(keyRiseSound);

        float t        = 0;
        float duration = 0.5f;

        float startHeight = transform.position.y;
        AnimationCurve[] curves  = { riseCurve, updownCurve, updownCurve };
        float[]          heights = { startHeight + 2, startHeight + 1.5f, startHeight + 2 };
        Vector3          fromPosition = transform.position;
        int i = 0;

        while (true)
        {
            t += Time.deltaTime;
            float s = t / duration;
            transform.position = Vector3.Lerp(fromPosition,
                new Vector3(transform.position.x, heights[i], transform.position.z),
                curves[i].Evaluate(s));

            if (s >= 1)
            {
                t = 0;
                i++;
                fromPosition = transform.position;
                if (i >= heights.Length) break;
            }
            yield return null;
        }

        collectable = true;
        GetComponent<AudioSource>().PlayOneShot(keyToPlayerSound);

        float velocity     = 0;
        float maxVelocity  = 20f;
        float acceleration = 7f;

        while (!collected)
        {
            if (_playerCore == null) yield break;

            Vector3 playerPos = _playerCore.transform.position;
            float   dist      = Vector2.Distance(transform.position, playerPos);

            velocity = Mathf.Min(velocity + Time.deltaTime * acceleration, maxVelocity);

            if (dist < velocity * Time.deltaTime)
                transform.position = playerPos;
            else
                transform.localPosition = Vector3.MoveTowards(transform.position, playerPos, velocity * Time.deltaTime);

            yield return null;
        }
    }
}