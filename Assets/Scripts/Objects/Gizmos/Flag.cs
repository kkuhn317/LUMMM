using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.Animation;
using PowerupState = PowerStates.PowerupState;

public class Flag : MonoBehaviour
{
    [HideInInspector]
    public float height;

    public GameObject starParticlePrefab;
    public AudioClip finishFlag;

    public GameObject flag;
    public GameObject pole;
    public GameObject cutsceneMario;

    // if null, it always uses cutsceneMario
    // if not null, use cutsceneMario if mario is small, and use optCutsceneBigMario if mario is big or higher
    // for big cutscene mario, its sprite library will be set to the same as the mario that touched the flag
    // so all powerups will be preserved
    public GameObject optCutsceneBigMario;

    GameObject csMario; // the mario that will be used in the cutscene
    Vector3 endPos = new(-0.4f, 1.0f, 0); // Where mario will stop sliding down the pole (changes when he's big)
    

    public float marioSlideSpeed = 50f;
    public float flagMoveSpeed = 50f;
    public float slideTime = 2f; // how long from when mario first touches the flag to when he gets off the pole
    public float cutsceneTime = 10f; // how long the cutscene lasts before the level ends
    bool marioAtBottom = false;
    public bool flagOnRight = false;    // if the flag is on the right side of the pole

    [Header("Cutscene System")]
    [SerializeField] private CutsceneSelector cutsceneSelector;

    enum FlagState
    {
        Idle,
        Sliding,
        Cutscene
    }

    FlagState state = FlagState.Idle;


    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (state == FlagState.Sliding) {
            // flag moves down
            // final position is -0.5, 1.1 relative to the pole
            flag.transform.localPosition = Vector3.MoveTowards(flag.transform.localPosition, new Vector3(flagOnRight ? 0.5f : -0.5f, 1.1f, 0), flagMoveSpeed * Time.deltaTime);

            Vector3 nonLocalEndPos = transform.position + endPos;

            // cutsceneMario moves down
            // final position is -0.4, 1.0 relative to the pole
            if (!marioAtBottom) {
                csMario.transform.position = Vector3.MoveTowards(csMario.transform.position, nonLocalEndPos, marioSlideSpeed * Time.deltaTime);
                if (csMario.transform.position.y == nonLocalEndPos.y) {
                    marioAtBottom = true;
                    // stop animating mario
                    csMario.GetComponent<Animator>().SetFloat("climbSpeed", 0f);
                }
            }
        }
    }

    // might be of use later but for now it's useless (I was testing so this is why this is here)
    private CutsceneContext BuildContext()
    {
        var gm = GameManager.Instance;
        var mario = FindObjectOfType<MarioMovement>();

        return new CutsceneContext
        {
            gameManager = gm,
            scene = SceneManager.GetActiveScene(),
            mainPlayer = mario,
            playerPosition = mario.transform.position,
            powerupState = mario.powerupState,
            hasStarPower = mario.starPower,
            isDead = mario.Dead,
        };
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player") && state == FlagState.Idle)
        {
            var mario = other.GetComponent<MarioMovement>();

            GameManager.Instance.StopTimer();

            csMario = cutsceneMario;

            if (optCutsceneBigMario != null && PowerStates.IsBig(other.gameObject.GetComponent<MarioMovement>().powerupState))
            {
                csMario = optCutsceneBigMario;
                csMario.GetComponent<SpriteLibrary>().spriteLibraryAsset = other.gameObject.GetComponent<SpriteLibrary>().spriteLibraryAsset;
                endPos = new(-0.4f, 1.5f, 0);
            }

            csMario.SetActive(true);
            csMario.transform.position = new Vector2(transform.position.x - 0.4f, other.transform.position.y);

            if (starParticlePrefab != null)
            {
                SpawnStarParticles();
            }

            // hide the player
            other.gameObject.SetActive(false);

            var animator = csMario.GetComponent<Animator>();
            animator.SetBool("isSideClimbing", true);
            animator.SetFloat("climbSpeed", 5f);

            state = FlagState.Sliding;

            // play the flagpole sound
            GetComponents<AudioSource>()[0].Play(); // first audio source is the flagpole sound, second is for music
            
            // stop the music
            GameManager.Instance.StopAllMusic();

            // change to cutscene state after a certain amount of time
            StartCoroutine(ToCutsceneState());
        }
    }

    private CutsceneContext BuildCutsceneContext()
    {
        return new CutsceneContext
        {
            gameManager = GameManager.Instance,
            scene = SceneManager.GetActiveScene()
        };
    }

    private IEnumerator ToCutsceneState()
    {
        yield return new WaitForSeconds(slideTime);

        state = FlagState.Cutscene; // Transition to cutscene state
        print("Transitioning to cutscene state.");

        var ctx = BuildCutsceneContext();

        if (cutsceneSelector != null)
        {
            StartCoroutine(cutsceneSelector.PlaySelectedCutscene(ctx));
        }
        else
        {
            // fallback
            StartCoroutine(GameManager.Instance.TriggerEndLevelCutscene(
                GetComponent<PlayableDirector>(), // PlayableDirector
                0f, // No additional delay for cutscene
                cutsceneTime, // Duration of the cutscene
                false, // Players are already destroyed
                false, // Music is already stopped
                true // Hide UI
            ));
        }
    }

    // used in the editor to change the height of the entire flagpole easily
    public void ChangeHeight(float height)
    {
        this.height = height;
        
        // flag
        flag.transform.localPosition = new Vector3(flagOnRight ? 0.5f : -0.5f, height - 0.55f, 0);

        // pole
        pole.GetComponent<SpriteRenderer>().size = new Vector2(0.5f, height);
        pole.transform.localPosition = new Vector3(0, (height+1) / 2, 0);

        // collider
        GetComponent<BoxCollider2D>().size = new Vector2(0.25f, height);
        GetComponent<BoxCollider2D>().offset = new Vector2(0, (height + 1) / 2);
    }

    #region particles
    void SpawnStarParticles()
    {
        // Spawn 8 stars around the flag
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
                float distance = (vertdirections[i] != 0 && horizdirections[j] != 0) ? 0.7f : 1f;
                Vector3 startOffset = new Vector3(horizdirections[i] * distance, vertdirections[j] * distance, 0);

                // Instantiate the star particle
                GameObject starParticle = Instantiate(starParticlePrefab, csMario.transform.position + startOffset, Quaternion.identity);

                starParticle.GetComponent<StarMoveOutward>().direction = new Vector2(vertdirections[i], horizdirections[j]);
                starParticle.GetComponent<StarMoveOutward>().speed = 2f;
            }
        }
    }
    #endregion
}