using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.U2D.Animation;
using PowerupState = PowerStates.PowerupState;

public class PlayerTransformation : MonoBehaviour
{
    [Header("Set in Inspector")]
    public GameObject oldChild;
    public GameObject newChild;

    [Header("Set in MarioMovement")]
    public GameObject oldPlayer;
    public GameObject newPlayer;

    private PowerupState oldPowerupState;
    private PowerupState newPowerupState;

    // Cached BEFORE oldPlayer gets destroyed (because transformPlayer runs later via Invoke)
    private float cachedOldFeetY;
    private Vector3 cachedSpawnPosition;

    void Start()
    {
    }

    public void startTransformation()
    {
        // Cache the position we want to spawn at (transformation object's position)
        cachedSpawnPosition = transform.position;

        // Cache "feet Y" from the OLD player right now (safe, oldPlayer still exists at this moment)
        cachedOldFeetY = cachedSpawnPosition.y;
        if (oldPlayer != null)
        {
            var oldCol = oldPlayer.GetComponent<Collider2D>();
            if (oldCol != null)
            {
                cachedOldFeetY = oldCol.bounds.min.y;
            }
            else
            {
                cachedOldFeetY = oldPlayer.transform.position.y;
            }
        }

        // Existing logic
        MarioMovement oldPlayerScript = oldPlayer.GetComponent<MarioMovement>();
        MarioMovement newPlayerScript = newPlayer.GetComponent<MarioMovement>();

        // Make the GameManager assign this transformation player to the same player number as the old player
        GameManager.Instance.SetPlayer(GetComponent<MarioMovement>(), oldPlayerScript.playerNumber);

        oldPowerupState = oldPlayerScript.powerupState;
        newPowerupState = newPlayerScript.powerupState;
        
        bool wasBig = PowerStates.IsBig(oldPowerupState);
        bool wasSmall = oldPowerupState == PowerupState.small;
        bool wasTiny = oldPowerupState == PowerupState.tiny;
        
        bool isBig = PowerStates.IsBig(newPowerupState);
        bool isSmall = newPowerupState == PowerupState.small;
        bool isTiny = newPowerupState == PowerupState.tiny;

        // Set the sprite libraries of this to the old player's sprite library
        SpriteLibrary oldPlayerSpriteLibrary = oldPlayer.GetComponent<SpriteLibrary>();
        SpriteLibrary newPlayerSpriteLibrary = newPlayer.GetComponent<SpriteLibrary>();

        oldChild.GetComponent<SpriteLibrary>().spriteLibraryAsset = oldPlayerSpriteLibrary.spriteLibraryAsset;
        newChild.GetComponent<SpriteLibrary>().spriteLibraryAsset = newPlayerSpriteLibrary.spriteLibraryAsset;

        // set the scale of both children to the scales of the old and new players
        oldChild.transform.localScale = oldPlayer.transform.localScale;
        newChild.transform.localScale = newPlayer.transform.localScale;

        // Flip the sprites if the player is facing left
        if (!GetComponent<MarioMovement>().facingRight)
        {
            oldChild.GetComponent<SpriteRenderer>().flipX = true;
            newChild.GetComponent<SpriteRenderer>().flipX = true;
        }

        Animator animator = GetComponent<Animator>();
        float time = 1f;

        if (wasBig && isTiny)
        {
            animator.Play("BigToTiny");
        }
        else if (wasTiny && isBig)
        {
            animator.Play("TinyToBig");
        }
        else if (wasBig && isSmall)
        {
            animator.Play("BigToSmall");
        }
        else if (wasSmall && isBig)
        {
            animator.Play("SmallToBig");
        }
        else if ((wasSmall || wasTiny) && isTiny)
        {
            animator.Play("SmallToTiny");
        }
        else if (wasBig && isBig)
        {
            animator.Play("BigToBig");
        }

        Invoke(nameof(transformPlayer), time);
    }

    public void transformPlayer()
    {
        // Instantiate the new player at the transformation object's position
        GameObject newMario = Instantiate(newPlayer, cachedSpawnPosition, Quaternion.identity);

        // match the bottom of the NEW collider to cachedOldFeetY
        var newCol = newMario.GetComponent<Collider2D>();
        if (newCol != null)
        {
            float newFeetY = newCol.bounds.min.y;
            float deltaY = cachedOldFeetY - newFeetY;
            newMario.transform.position += new Vector3(0f, deltaY, 0f);
        }

        // Transfer gameplay state (velocity, devices, etc.)
        GetComponent<MarioMovement>().transferProperties(newMario);

        Destroy(gameObject);
    }
}