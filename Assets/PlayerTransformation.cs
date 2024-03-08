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


    // Start is called before the first frame update
    void Start()
    {
        
        
    }

    public void startTransformation() {
        MarioMovement oldPlayerScript = oldPlayer.GetComponent<MarioMovement>();
        MarioMovement newPlayerScript = newPlayer.GetComponent<MarioMovement>();

        oldPowerupState = oldPlayerScript.powerupState;
        newPowerupState = newPlayerScript.powerupState;
        bool wasBig = PowerStates.IsBig(oldPowerupState);
        bool wasSmall = PowerStates.IsSmall(oldPowerupState);
        bool isBig = PowerStates.IsBig(newPowerupState);
        bool isSmall = PowerStates.IsSmall(newPowerupState);

        // Set the sprite libraries of this to the old player's sprite library
        SpriteLibrary oldPlayerSpriteLibrary = oldPlayer.GetComponent<SpriteLibrary>();
        SpriteLibrary newPlayerSpriteLibrary = newPlayer.GetComponent<SpriteLibrary>();
        // SpriteResolver oldPlayerSpriteResolver = oldPlayer.GetComponent<SpriteResolver>();
        // SpriteResolver newPlayerSpriteResolver = newPlayer.GetComponent<SpriteResolver>();

        oldChild.GetComponent<SpriteLibrary>().spriteLibraryAsset = oldPlayerSpriteLibrary.spriteLibraryAsset;
        newChild.GetComponent<SpriteLibrary>().spriteLibraryAsset = newPlayerSpriteLibrary.spriteLibraryAsset;

        // set the scale of both children to the scales of the old and new players
        oldChild.transform.localScale = oldPlayer.transform.localScale;
        newChild.transform.localScale = newPlayer.transform.localScale;

        // Flip the sprites if the player is facing left
        if (!GetComponent<MarioMovement>().facingRight) {
            oldChild.GetComponent<SpriteRenderer>().flipX = true;
            newChild.GetComponent<SpriteRenderer>().flipX = true;
        }

        Animator animator = GetComponent<Animator>();

        float time = 1f;

        if (newPowerupState == PowerupState.tiny) {
            animator.Play("SmallToTiny");
        } else if (wasBig && isSmall) {
            animator.Play("BigToSmall");
        } else if (wasSmall && isBig) {
            animator.Play("SmallToBig");
        } else if (wasBig && isBig) {
            animator.Play("BigToBig");
        }

        Invoke(nameof(transformPlayer), time);
    }

    public void transformPlayer() {
        float verticalOffset = 0f;
        PowerupState newPowerupState = newPlayer.GetComponent<MarioMovement>().powerupState;

        if (PowerStates.IsSmall(newPowerupState) && !PowerStates.IsSmall(oldPowerupState)) {
            verticalOffset = -0.5f;
        } else if (!PowerStates.IsSmall(newPowerupState) && PowerStates.IsSmall(oldPowerupState)) {
            verticalOffset = 0.5f;
        }

        GameObject newMario = Instantiate(newPlayer, new Vector3(transform.position.x, transform.position.y + verticalOffset, transform.position.z), Quaternion.identity);
        
        GetComponent<MarioMovement>().transferProperties(newMario);

        Destroy(gameObject);
    }
}
