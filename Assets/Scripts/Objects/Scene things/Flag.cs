using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Flag : MonoBehaviour
{
    [HideInInspector]
    public float height;
    public GameObject flag;
    public GameObject pole;
    public GameObject cutsceneMario;
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
                cutsceneMario.transform.localPosition = Vector3.MoveTowards(cutsceneMario.transform.localPosition, new Vector3(-0.4f, 1.0f, 0), marioSlideSpeed * Time.deltaTime);
                if (cutsceneMario.transform.localPosition.y == 1.0f) {
                    marioAtBottom = true;
                    // stop animating mario
                    cutsceneMario.GetComponent<Animator>().SetFloat("climbSpeed", 0f);
                    Invoke(nameof(endLevel), cutsceneTime);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player" && state == FlagState.Idle)
        {
            GameManager.Instance.StopTimer();

            cutsceneMario.SetActive(true);
            cutsceneMario.transform.position = new Vector2(transform.position.x - 0.4f, other.transform.position.y);

            // delete the player
            Destroy(other.gameObject);

            var animator = cutsceneMario.GetComponent<Animator>();

            animator.SetFloat("climbSpeed", 1f);
            animator.Play("mario_climbside");

            state = FlagState.Sliding;

            // play the flagpole sound
            GetComponents<AudioSource>()[0].Play(); // first audio source is the flagpole sound, second is for music
            
            // stop the music
            GameManager.Instance.stopAllMusic();

            // change to cutscene state after a certain amount of time
            Invoke("toCutsceneState", slideTime);
        }
    }

    void toCutsceneState()
    {
        state = FlagState.Cutscene;

        // play the cutscene
        GetComponent<PlayableDirector>().Play();
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
