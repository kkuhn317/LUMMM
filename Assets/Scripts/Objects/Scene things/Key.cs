using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    public GameObject particle;

    private GameObject _player; // don't use this directly, use the player property instead
    private GameObject player {
        get {
            if (_player == null) {
                _player = GameObject.FindGameObjectWithTag("Player");
            }
            return _player;
        }
        set { _player = value; }
    }

    private bool collectable = false;
    private bool collected = false;

    private Vector3 actualposition;

    [Header("Collected from Enemy")]

    // if true, then the key will immediately rise up and go towards the player
    public bool fromEnemy = false;

    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // this is changed in inspector
    public AnimationCurve updownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AudioClip keyRiseSound;
    public AudioClip keyToPlayerSound;

    /*[Header("When collected key")]
    private bool isMoving = false;
    public bool shouldPlayAnimationAndAudio = true;
    public bool playerMoved = false; // New flag to track if the player moved after the timer started
    private float notMovingTime = 0f;
    public float timeThreshold = 5f; // Adjust this to set the time threshold for triggering the audio and animation
    private GameObject playerwithKey; // Reference to the player object that grabs the key
    public string animationParameterTrigger;
    public AudioClip playerAudioClip; // Audio clip from the player object*/

    [Header("Bounce")]

    public float bounceheight = 0.5f;
    public float bounceSpeed = 0.5f;
    private float bounceOffset = 0;

    // Start is called before the first frame update
    void Start()
    {
        actualposition = transform.position;
        if (fromEnemy) {
            StartCoroutine(goToMario());
        } else {
            collectable = true;
        }
    }

    void Update()
    {
        /*if (collected) {
            followPlayer();
        }*/

        if (collected)
        {
            followPlayer();

            /*if (isMoving)
            {
                notMovingTime = 0f;
                playerMoved = true; // Set the flag to true when the player moves
            }
            else
            {
                notMovingTime += Time.deltaTime;
                if (notMovingTime >= timeThreshold && !playerMoved && shouldPlayAnimationAndAudio) // Check if the player has not moved before triggering audio and animation
                {
                    PlayAudioAndAnimation();
                }
            }*/
        }
    }

    // Additional function to handle playing audio and animation when the key goes to Mario after not moving for a certain time.
    /*void PlayAudioAndAnimation()
    {
        if (playerwithKey != null)
        {
            // Play the animation on the player GameObject
            Animator playerAnimator = playerwithKey.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(animationParameterTrigger);
            }
            else
            {
                Debug.LogError("Player Animator component not found.");
            }

            // Play audio on the player GameObject
            AudioSource playerAudioSource = playerwithKey.GetComponent<AudioSource>();
            if (playerAudioSource != null && playerAudioClip != null)
            {
                playerAudioSource.PlayOneShot(playerAudioClip);
            }
            else
            {
                Debug.LogError("Player AudioSource component or audio clip not found.");
            }
        }
        else
        {
            Debug.LogError("Player with Key GameObject not found.");
        }
    }

    // Function to be called when the key is collected by a player
    public void OnCollected(GameObject player, AudioClip audioClip, bool playAnimationAndAudio)
    {
        playerwithKey = player; // Set the reference to the player that collected the key
        playerAudioClip = audioClip; // Set the audio clip from the player
        shouldPlayAnimationAndAudio = playAnimationAndAudio; // Set the flag to determine if animation and audio should be played

        if (!shouldPlayAnimationAndAudio)
        {
            // If the flag is set to false, reset the notMovingTime and playerMoved flags
            notMovingTime = 0f;
            playerMoved = false;
        }
    }*/

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player" && !collected && collectable)
        {
            actualposition = transform.position;
            player = other.gameObject;
            collected = true;
            GameManager.Instance.keys.Add(gameObject);
            GetComponent<AudioSource>().Play();
            spawnParticles();
        }
    }

    void spawnParticles()
    {
        // spawn 8 of them around the key and make them move outwards in specific directions
        int[] vertdirections = new int[] { -1, 0, 1 };
        int[] horizdirections = new int[] { -1, 0, 1 };
        for (int i = 0; i < vertdirections.Length; i++)
        {
            for (int j = 0; j < horizdirections.Length; j++)
            {
                if (vertdirections[i] == 0 && horizdirections[j] == 0)
                {
                    continue;
                }
                float distance;
                if (vertdirections[i] != 0 && vertdirections[j] != 0) {
                    distance = 0.7f;
                } else {
                    distance = 1f;
                }
                Vector3 startoffset = new Vector3(horizdirections[i] * distance, vertdirections[j] * distance, 0);
                
                GameObject newParticle = Instantiate(particle, transform.position + startoffset, Quaternion.identity);

                // make the particles move outwards at constant speed
                newParticle.GetComponent<StarMoveOutward>().direction = new Vector2(vertdirections[i], horizdirections[j]);
                newParticle.GetComponent<StarMoveOutward>().speed = 2f;
            }
        }
    }

    // follow the player when they have collected the key
    void followPlayer()
    {
        //transform.position = player.transform.position;
        // slowly move towards the player
        // faster if farther away

        if (player == null) {
            return;
        }

        // go behind the player
        MarioMovement playerScript = player.GetComponent<MarioMovement>();
        if (!playerScript) {
            return;
        }
        bool playerDirection = playerScript.facingRight;
        Vector3 offset = new Vector3(playerDirection ? -1 : 1, 0, 0);
        if (playerScript.powerupState != MarioMovement.PowerupState.small) {
            offset += new Vector3(0, -0.5f, 0);
        }    

        Vector3 finalLocation = player.transform.position + offset;

        float distance = Vector2.Distance(actualposition, finalLocation);
        //print("dist: " + distance);
        float speed = Mathf.Pow(distance, 2) * 2f;

        //print("speed: " + speed);
        actualposition = Vector2.MoveTowards(actualposition, finalLocation, speed * Time.deltaTime);

        
        // bounce up and down
        //bounceOffset += Time.deltaTime * 0.5f;
        //if (bounceOffset > 1)
        //{
        //    bounceOffset = 0;
        //}
        bounceOffset += Time.deltaTime * bounceSpeed;
        if (bounceOffset > 1)
        {
            bounceOffset = 0;
        }
        //actualposition.y += Mathf.Sin(bounceOffset * Mathf.PI) * bounceheight;
        transform.position = actualposition + new Vector3(0, Mathf.Sin(bounceOffset * Mathf.PI) * bounceheight, 0);

    }

    IEnumerator goToMario()
    {
        // play sound
        GetComponent<AudioSource>().PlayOneShot(keyRiseSound);

        float t = 0;
        float duration = 0.5f;

        // rise up 2, then go down 0.5, then go up 0.5, then go to mario
        float startHeight = transform.position.y;
        AnimationCurve[] curves = { riseCurve, updownCurve, updownCurve };
        float[] heights = {startHeight + 2, startHeight + 1.5f, startHeight + 2};
        Vector3 fromPosition = transform.position;
        int i = 0;

        while (true)
        {
            // use the curve
            t += Time.deltaTime;
            float s = t / duration;
            transform.position = Vector3.Lerp(fromPosition, new Vector3(transform.position.x, heights[i], transform.position.z), curves[i].Evaluate(s));

            if (s >= 1)
            {
                t = 0;
                i++;
                fromPosition = transform.position;
                if (i >= heights.Length)
                {
                    break;
                }
            }

            yield return null;
        }

        collectable = true;

        GetComponent<AudioSource>().PlayOneShot(keyToPlayerSound);

        float velocity = 0;
        float maxVelocity = 20f;
        float acceleration = 7f;

        // now go to mario speeding up over time
        while (collected == false)
        {
            Vector3 playerPos = player.transform.position;
            float distance = Vector2.Distance(transform.position, playerPos);

            if (velocity < maxVelocity) {
                velocity += Time.deltaTime * acceleration;
            } else {
                velocity = maxVelocity;
            }

            if (distance < velocity * Time.deltaTime)
            {
                transform.position = playerPos;
            } else {
                // increase velocity
                velocity += Time.deltaTime * acceleration;
                transform.localPosition = Vector3.MoveTowards(transform.position, playerPos, velocity * Time.deltaTime);
            }
            yield return null;
        }
    }
}
