using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : ObjectPhysics
{

    public GameObject newMarioState;
    public int powerLevel = 1;
    public float starTime = -1;
    public GameObject starMusicOverride;

    public bool is1Up = false;

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
                return;
            }
            if (starTime > 0) {
                other.GetComponent<MarioMovement>().startStarPower(starTime);
                GameObject starSong = Instantiate(starMusicOverride);
                // add back when music override added
                //starSong.GetComponent<musicOverride>().stopPlayingAfterTime(starTime);
            }
            if (newMarioState) {
                MarioMovement player = other.GetComponent<MarioMovement>();
                MarioMovement.PowerupState playerState = player.powerupState;
                if (canGetPowerup(playerState)) 
                    other.GetComponent<MarioMovement>().ChangePowerup(newMarioState);
            }
            GetComponent<AudioSource>().Play();
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
            Destroy(gameObject, 2);
        }
    }

    private bool canGetPowerup(MarioMovement.PowerupState state) {
        if (state == MarioMovement.PowerupState.small)
            return true;
        else {
            if (powerLevel >= 2)
                return true;
            else
                return false;
        }
    }

}
