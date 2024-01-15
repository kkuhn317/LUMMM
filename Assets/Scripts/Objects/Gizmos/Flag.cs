using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.U2D.Animation;

public class Flag : MonoBehaviour
{
    [HideInInspector]
    public float height;
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

    enum FlagState
    {
        Idle,
        Sliding,
        Cutscene
    }

    FlagState state = FlagState.Idle;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (state == FlagState.Sliding) {
            // flag moves down
            // final position is -0.5, 1.1 relative to the pole
            flag.transform.localPosition = Vector3.MoveTowards(flag.transform.localPosition, new Vector3(-0.5f, 1.1f, 0), flagMoveSpeed * Time.deltaTime);

            // cutsceneMario moves down
            // final position is -0.4, 1.0 relative to the pole
            if (!marioAtBottom) {
                csMario.transform.localPosition = Vector3.MoveTowards(csMario.transform.localPosition, endPos, marioSlideSpeed * Time.deltaTime);
                if (csMario.transform.localPosition.y == endPos.y) {
                    marioAtBottom = true;
                    // stop animating mario
                    csMario.GetComponent<Animator>().SetFloat("climbSpeed", 0f);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player") && state == FlagState.Idle)
        {
            GameManager.Instance.StopTimer();

            csMario = cutsceneMario;


            if (optCutsceneBigMario != null && other.gameObject.GetComponent<MarioMovement>().powerupState != MarioMovement.PowerupState.small)
            {
                csMario = optCutsceneBigMario;
                csMario.GetComponent<SpriteLibrary>().spriteLibraryAsset = other.gameObject.GetComponent<SpriteLibrary>().spriteLibraryAsset;
                endPos = new(-0.4f, 1.5f, 0);
            }

            csMario.SetActive(true);
            csMario.transform.position = new Vector2(transform.position.x - 0.4f, other.transform.position.y);

            // delete the player
            Destroy(other.gameObject);

            var animator = csMario.GetComponent<Animator>();
            animator.SetBool("isClimbing", true);
            animator.SetFloat("climbSpeed", 1f);

            state = FlagState.Sliding;

            // play the flagpole sound
            GetComponents<AudioSource>()[0].Play(); // first audio source is the flagpole sound, second is for music
            
            // stop the music
            GameManager.Instance.StopAllMusic();

            // change to cutscene state after a certain amount of time
            Invoke(nameof(ToCutsceneState), slideTime);
        }
    }

    void ToCutsceneState()
    {
        state = FlagState.Cutscene;

        // play the cutscene
        GetComponent<PlayableDirector>().Play();
        Invoke(nameof(endLevel), cutsceneTime);
    }

    // used in the editor to change the height of the entire flagpole easily
    public void ChangeHeight(float height)
    {
        this.height = height;
        
        // flag
        flag.transform.localPosition = new Vector3(-0.5f, height - 0.55f, 0);

        // pole
        pole.GetComponent<SpriteRenderer>().size = new Vector2(0.5f, height);
        pole.transform.localPosition = new Vector3(0, (height+1) / 2, 0);

        // collider
        GetComponent<BoxCollider2D>().size = new Vector2(0.25f, height);
        GetComponent<BoxCollider2D>().offset = new Vector2(0, (height + 1) / 2);
    }

    void endLevel()
    {
        GameManager.Instance.FinishLevel();
    }
}
