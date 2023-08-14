using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goomba : EnemyAI
{
    public enum EnemyState {
        walking,
        crushed,
        ambush
    }

    public EnemyState state = EnemyState.walking;

    public bool stompable = true;

    private bool shouldDie = false;
    private float deathTimer = 0;
    public float timeBeforeDestroy = 1.0f;

    protected override void Update() {
        base.Update();
        CheckCrushed();
    }

    protected override void hitByStomp(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        playerscript.Jump();
        Crush();
        GameManager.Instance.AddScorePoints(100); // Gives a hundred points to the player
    }

    public void Crush () {

        GetComponent<AudioSource>().Play();
        
        if (!stompable) {
            return;
        }

        state = EnemyState.crushed;
        movement = ObjectMovement.still;

        GetComponent<Animator>().SetBool("isCrushed", true);

        GetComponent<Collider2D>().enabled = false;

        shouldDie = true;

        releaseItem();

    }

    void CheckCrushed () {

        if (shouldDie) {
            
            if (deathTimer <= timeBeforeDestroy) {

                deathTimer += Time.deltaTime;

            } else {
                
                shouldDie = false;

                Destroy(this.gameObject);
            }
        }
    }
}
