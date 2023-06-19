using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{

    public GameObject particle;

    private GameObject player;
    private bool collected = false;

    private Vector3 acutalposition;

    public float bounceheight = 0.5f;

    public float bounceSpeed = 0.5f;
    private float bounceOffset = 0;

    
    

    // Start is called before the first frame update
    void Start()
    {
        acutalposition = transform.position;
    }

    void Update()
    {
        if (collected) {
            followPlayer();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player" && player == null)
        {
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

    void findPlayer() {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void followPlayer()
    {
        //transform.position = player.transform.position;
        // slowly move towards the player
        // faster if farther away
        if (player == null)
        {
            findPlayer();
            if (player == null)
            {
                return;
            }
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

        float distance = Vector2.Distance(acutalposition, finalLocation);
        //print("dist: " + distance);
        float speed = Mathf.Pow(distance, 2) * 2f;

        //print("speed: " + speed);
        acutalposition = Vector2.MoveTowards(acutalposition, finalLocation, speed * Time.deltaTime);

        
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
        //acutalposition.y += Mathf.Sin(bounceOffset * Mathf.PI) * bounceheight;
        transform.position = acutalposition + new Vector3(0, Mathf.Sin(bounceOffset * Mathf.PI) * bounceheight, 0);

    }


}
