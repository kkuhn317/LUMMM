using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Script for all player abilities
// override whatever needed
// TODO: Move Crawling, wall jump (and more?) to use this (if possible)
public class MarioAbility : MonoBehaviour
{
    [HideInInspector] public bool isBlockingJump = false; // used for blocking jumping & spin jumping when ability is used (also blocks swimming)
    protected MarioMovement marioMovement;

    protected virtual void Start() { 
        marioMovement = GetComponent<MarioMovement>();
    }

    public virtual void onShootPressed() { }

    public virtual void onExtraActionPressed() { }
}
