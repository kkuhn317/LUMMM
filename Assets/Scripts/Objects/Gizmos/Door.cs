using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject destination;
    public bool locked = false;
    public AudioClip unlockSound, openSound, closeSound, blockedSound;
    protected float unlockTime = 0.5f;
    protected GameObject player;
    public GameObject particle; // The star particle that spawns when the door is unlocked
    protected Animator animator;
    protected Door otherDoor;   // The destination. If null, then the player does not teleport
    protected bool inUse = false;
    public GameObject blackFade;
    private bool hasPlayedBlockedSound = false;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        animator = GetComponent<Animator>();

        if (destination) {
            otherDoor = destination.GetComponent<Door>();
        }
    }

    void findPlayer() {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (inUse) {
            return;
        }

        if (player == null) {
            findPlayer();
            if (player == null) {
                return;
            }
        }

        if (!player.GetComponent<MarioMovement>()) {  // dead mario doesn't have this
            return;
        }

        MarioMovement playerScript = player.GetComponent<MarioMovement>();

        // if player is at the door
        if (PlayerAtDoor(playerScript))
        {
            // if pressing up
            if (playerScript.moveInput.y > 0.5)
            {
                // if the door is locked
                if (locked)
                {
                    // if player has the key
                    if (CheckForKey())
                    {
                        inUse = true;
                        Unlock();
                    }
                    else if (!hasPlayedBlockedSound)
                    {
                        GetComponent<AudioSource>().PlayOneShot(blockedSound);
                        animator.SetTrigger("Blocked");
                        hasPlayedBlockedSound = true;
                    }
                }
                else
                {
                    inUse = true;
                    Open();
                }
            }
            else
            {
                hasPlayedBlockedSound = false;
            }
        }
    }

    protected virtual bool PlayerAtDoor(MarioMovement playerScript) {
        Vector2 playerPos = player.transform.position;
        if (playerScript.powerupState == MarioMovement.PowerupState.small) {
            playerPos.y += 0.5f;
        }

        float xdist = Mathf.Abs(playerPos.x - transform.position.x);
        float ydist = Mathf.Abs(playerPos.y - transform.position.y);

        // if player is at the door
        return xdist < 0.4 && ydist < 0.1 && playerScript.onGround;
    }

    protected virtual bool CheckForKey() {
        if (GameManager.Instance.keys.Count > 0) {
            return true;
        }
        return false;
    }

    protected virtual void SpendKey() {
        GameObject usedKey = GameManager.Instance.keys[0];
        GameManager.Instance.keys.RemoveAt(0);
        Destroy(usedKey);
    }

    void FreezePlayer() {
        player.GetComponent<Rigidbody2D>().simulated = false;
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        Animator playerAnimator = player.GetComponent<Animator>();
        playerAnimator.SetBool("onGround", true);
        playerAnimator.SetBool("isRunning", false);
        playerAnimator.SetBool("isSkidding", false);

        // disable all scripts
        foreach (MonoBehaviour script in player.GetComponents<MonoBehaviour>()) {
            script.enabled = false;
        }
    }
    void UnfreezePlayer() {
        player.GetComponent<Rigidbody2D>().simulated = true;
        
        // enable all scripts
        foreach (MonoBehaviour script in player.GetComponents<MonoBehaviour>()) {
            script.enabled = true;
        }
    }

    void Unlock() {
        locked = false;
        SpendKey();
        GetComponent<AudioSource>().PlayOneShot(unlockSound);
        FreezePlayer();
        animator.SetTrigger("Unlock");
        spawnParticles();
        Invoke(nameof(Open), unlockTime);

        // Unlock other door if needed
        if (otherDoor) {
            if (otherDoor.locked) {
                otherDoor.locked = false;
                destination.GetComponent<Animator>().SetTrigger("Unlock");
            }
        }
    }

    void spawnParticles()
    {
        // spawn 2 of them at the top corners of the door
        GameObject newParticle1 = Instantiate(particle, transform.position + new Vector3(-0.5f, 1, 0), Quaternion.identity);
        GameObject newParticle2 = Instantiate(particle, transform.position + new Vector3(0.5f, 1, 0), Quaternion.identity);


        // make the particles move outwards at constant speed
        newParticle1.GetComponent<StarMoveOutward>().direction = new Vector2(-1, 1);
        newParticle2.GetComponent<StarMoveOutward>().direction = new Vector2(1, 1);
        newParticle1.GetComponent<StarMoveOutward>().speed = 2f;
        newParticle2.GetComponent<StarMoveOutward>().speed = 2f;
    }

    void Open() {
        GetComponent<AudioSource>().PlayOneShot(openSound);
        FreezePlayer();
        animator.SetTrigger("Open");
        Invoke(nameof(Teleport), 0.5f);
        Invoke(nameof(Close), 1);

        if (blackFade)
            blackFade.GetComponent<Animator>().SetTrigger("Fade");

        // Open and close the other door
        if (otherDoor) {
            otherDoor.inUse = true;
            destination.GetComponent<Animator>().SetTrigger("Open");
            otherDoor.Invoke(nameof(Close), 1);
        }

    }

    void Teleport() {
        if (otherDoor) {
            player.transform.position = destination.transform.position;
            if (player.GetComponent<MarioMovement>().powerupState == MarioMovement.PowerupState.small) {
                player.transform.position -= new Vector3(0, 0.5f, 0);
            }
        }

        UnfreezePlayer();
    }

    protected virtual void Close() {
        animator.SetTrigger("Close");
        GetComponent<AudioSource>().PlayOneShot(closeSound);
        Invoke(nameof(Ready), 0.5f);
    }

    void Ready() {
        inUse = false;
    }
}