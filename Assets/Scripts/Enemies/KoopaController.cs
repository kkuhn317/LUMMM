using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KoopaController : EnemyAI
{

    public enum EnemyState {
        walking,
        inShell,
        movingShell
    }

    public EnemyState state = EnemyState.walking;
    public float walkingSpeed = 2;
    public float movingShellSpeed = 10;
    private Animator animator;

    private AudioSource audioSource;

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        switch(state) {
            case EnemyState.walking:
                ToWalking();
                break;
            case EnemyState.inShell:
                ToInShell();
                break;
            case EnemyState.movingShell:
                ToMovingShell(movingLeft);
                break;
        }
    }

    protected override void touchNonPlayer(GameObject other)
    {
        if (other.gameObject.tag == "Enemy" && state == EnemyState.movingShell) {
            other.gameObject.GetComponent<EnemyAI>().KnockAway(movingLeft);
            audioSource.PlayOneShot(knockAwaySound);
        }
    }

    private void ToWalking() {
        state = EnemyState.walking;
        animator.SetBool("inShell", false);
        velocity = new Vector2(walkingSpeed, velocity.y);
        checkObjectCollision = true;
    }

    private void ToInShell() {
        state = EnemyState.inShell;
        animator.SetBool("inShell", true);
        velocity = new Vector2(0, velocity.y);
        checkObjectCollision = true;
    }

    private void ToMovingShell(bool direction) {
        state = EnemyState.movingShell;
        movingLeft = direction;
        animator.SetBool("inShell", true);
        velocity = new Vector2(movingShellSpeed, velocity.y);
        checkObjectCollision = false;
    }

    protected override void hitByStomp(GameObject player) {
        MarioMovement playerScript = player.GetComponent<MarioMovement>();
        switch (state) {
            case EnemyState.walking:
                playerScript.Jump();
                audioSource.Play();
                ToInShell();
                break;
            case EnemyState.inShell:
                audioSource.PlayOneShot(knockAwaySound);
                ToMovingShell(player.transform.position.x > transform.position.x);
                break;
            case EnemyState.movingShell:
                playerScript.Jump();
                audioSource.Play();
                ToInShell();
                break;
        }

    }

    protected override void hitOnSide(GameObject player) {
        MarioMovement playerScript = player.GetComponent<MarioMovement>();
        switch (state) {
            case EnemyState.walking:
                playerScript.damageMario();
                break;
            case EnemyState.inShell:
                audioSource.PlayOneShot(knockAwaySound);
                ToMovingShell(player.transform.position.x > transform.position.x);
                break;
            case EnemyState.movingShell:
                playerScript.damageMario();
                break;
        }
    }
}
