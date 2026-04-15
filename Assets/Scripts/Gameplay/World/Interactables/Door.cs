using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;
using PowerupState = PowerStates.PowerupState;

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

    // Used for positioning the particles
    public float doorWidth = 1.0f;
    public float doorHeight = 2.0f;
    protected AudioSource audioSource;

    public bool snapCameraX;
    public bool snapCameraY;

    private PlayerRegistry playerRegistry;
    private KeyInventorySystem keyInventory;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (destination)
        {
            otherDoor = destination.GetComponent<Door>();
        }

        CacheSystems();
    }

    private void CacheSystems()
    {
        // Prefer getting them through the refactored GM if you have GetSystem<T>()
        if (GameManager.Instance != null)
        {
            playerRegistry = GameManager.Instance.GetSystem<PlayerRegistry>();
            keyInventory  = GameManager.Instance.GetSystem<KeyInventorySystem>();
        }

        // Fallback (in case Door runs before GM systems are ready)
        if (playerRegistry == null) playerRegistry = FindObjectOfType<PlayerRegistry>(true);
        if (keyInventory == null)  keyInventory  = FindObjectOfType<KeyInventorySystem>(true);
    }

    void findPlayer()
    {
        // MarioCore playerScript = GameManager.Instance.GetPlayer(0);
        MarioCore playerScript = playerRegistry.GetPlayer(0);
        if (playerScript)
        {
            player = playerScript.gameObject;
        }

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (inUse)
        {
            return;
        }

        if (player == null)
        {
            findPlayer();
            if (player == null)
            {
                return;
            }
        }

        MarioCore playerScript = player.GetComponent<MarioCore>();

        if (playerScript == null)
        {
            return;
        }

        // if player is at the door
        if (PlayerAtDoor(playerScript))
        {
            // if pressing up
            if (playerScript.State.MoveInput.y > 0.5f)
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
                        audioSource.PlayOneShot(blockedSound);
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

    protected virtual bool PlayerAtDoor(MarioCore playerScript)
    {
        Vector2 playerPos = player.transform.position;
        // TODO: base off of player's actual height (this doesn't work for tiny mario)
        if (playerScript.State.PowerupState == PowerupState.small)
        {
            playerPos.y += 0.5f;
        }

        float xdist = Mathf.Abs(playerPos.x - transform.position.x);
        float ydist = Mathf.Abs(playerPos.y - transform.position.y);

        // if player is at the door
        return xdist < 0.4 && ydist < 0.1 && playerScript.State.OnGround;
    }

    protected virtual bool CheckForKey()
    {
        /*if (GameManager.Instance.keys.Count > 0)
        {
            return true;
        }
        return false;*/

        if (keyInventory == null) CacheSystems();
        if (keyInventory == null) return false;

        return keyInventory.HasKey();
    }

    protected virtual void SpendKey()
    {
        /*GameObject usedKey = GameManager.Instance.keys[0];
        GameManager.Instance.keys.RemoveAt(0);
        Destroy(usedKey);*/

        if (keyInventory == null) CacheSystems();
        if (keyInventory == null) return;
        
        GameObject usedKey = keyInventory.ConsumeKey();
        if (usedKey != null) Destroy(usedKey);
    }

    protected virtual void FreezePlayer()
    {
        var core = player.GetComponent<MarioCore>();
        if (core == null) return;

        // Cancel look-up immediately so it doesn't persist through the door sequence
        if (core.State.IsLookingUp)
        {
            core.State.IsLookingUp = false;
            MarioEvents.FireLookUpEnded(core.PlayerIndex);
        }

        core.SetModulesEnabled(false);
        core.DisableInputs();
        core.State.InputLocked  = true;
        core.State.IsUsingObject = true;
        core.Rb.velocity        = Vector2.zero;
        core.Rb.isKinematic     = true;
        if (core.Collider) core.Collider.enabled = false;
    }
    protected void UnfreezePlayer()
    {
        var core = player.GetComponent<MarioCore>();
        if (core == null) return;

        core.Rb.isKinematic      = false;
        if (core.Collider) core.Collider.enabled = true;
        core.SetModulesEnabled(true);
        core.State.InputLocked   = false;
        core.State.IsUsingObject = false;
        core.EnableInputs();
        core.StateMachine.ForceTransition(MarioStateID.Idle);
    }

    protected virtual void Unlock()
    {
        locked = false;
        SpendKey();
        audioSource.PlayOneShot(unlockSound);
        CenterPlayerAtDoor();
        FreezePlayer();
        animator.SetTrigger("Unlock");
        spawnParticles();
        Invoke(nameof(Open), unlockTime);

        // Unlock other door if needed
        if (otherDoor)
        {
            if (otherDoor.locked)
            {
                otherDoor.locked = false;
                destination.GetComponent<Animator>().SetTrigger("Unlock");
            }
        }
    }

    void spawnParticles()
    {
        // spawn 2 of them at the top corners of the door
        GameObject newParticle1 = Instantiate(particle, transform.position + new Vector3(-0.5f * doorWidth, 0.5f * doorHeight, 0), Quaternion.identity);
        GameObject newParticle2 = Instantiate(particle, transform.position + new Vector3(0.5f * doorWidth, 0.5f * doorHeight, 0), Quaternion.identity);


        // make the particles move outwards at constant speed
        newParticle1.GetComponent<StarMoveOutward>().direction = new Vector2(-1, 1);
        newParticle2.GetComponent<StarMoveOutward>().direction = new Vector2(1, 1);
        newParticle1.GetComponent<StarMoveOutward>().speed = 2f;
        newParticle2.GetComponent<StarMoveOutward>().speed = 2f;
    }

    void Open()
    {
        audioSource.PlayOneShot(openSound);
        FreezePlayer();
        animator.SetTrigger("Open");
        CenterPlayerAtDoor();
        Invoke(nameof(Teleport), 0.5f);
        Invoke(nameof(Close), 1);

        if (blackFade)
            blackFade.GetComponent<Animator>().SetTrigger("Fade");

        // Open and close the other door
        if (otherDoor)
        {
            otherDoor.inUse = true;
            destination.GetComponent<Animator>().SetTrigger("Open");
            otherDoor.Invoke(nameof(Close), 1);
        }

    }

    protected virtual void CenterPlayerAtDoor()
    {
        if (player != null)
        {
            Vector3 doorCenter = new(transform.position.x, player.transform.position.y, player.transform.position.z);
            player.transform.position = doorCenter;
        }
    }

    void Teleport()
    {
        if (otherDoor)
        {
            var core = player.GetComponent<MarioCore>();
            Vector2 dest = destination.transform.position;
            if (core != null && core.State.PowerupState == PowerupState.small)
                dest.y -= 0.5f;
            if (core != null)
                core.Rb.position = dest;
            else
                player.transform.position = destination.transform.position;
        }

        var camPos = Camera.main.transform.position;
        if (snapCameraX) camPos.x = player.transform.position.x;
        if (snapCameraY) camPos.y = player.transform.position.y;
        Camera.main.transform.position = camPos;

        UnfreezePlayer();
    }

    protected virtual void Close()
    {
        animator.SetTrigger("Close");
        audioSource.PlayOneShot(closeSound);
        Invoke(nameof(Ready), 0.5f);
    }

    void Ready()
    {
        inUse = false;
    }
    
    public void SetInUse(bool value)
    {
        inUse = value;
    }
}