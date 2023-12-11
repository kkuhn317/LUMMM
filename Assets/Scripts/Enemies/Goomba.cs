using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Goomba : EnemyAI
{
    public enum EnemyState {
        walking,
        crushed,
        ambush
    }

    public EnemyState state = EnemyState.walking;

    public bool stompable = true;
    private bool isAmbushing = false;

    public bool shouldntEnemyMoveWhenDie = true;
    protected bool shouldDie = false;
    private float deathTimer = 0;
    public bool shouldDestroyAfterCrush = true;
    public float timeBeforeDestroy = 1.0f;

    [Header("Cutscene")]
    public PlayableDirector cutscene;
    public float delayBeforeCutscene = 1.0f;

    protected override void Update() {
        base.Update();

        CheckAmbush();
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

        if (shouldntEnemyMoveWhenDie)
        {
            movement = ObjectMovement.still;
        }
        else
        {
            velocity.x = 0; 
            movement = ObjectMovement.sliding;
        }

        GetComponent<Animator>().SetBool("isCrushed", true);

        GetComponent<Collider2D>().enabled = false;

        if (shouldDestroyAfterCrush)
        { // Only destroy if the flag is set to true
            shouldDie = true;       
        }
        releaseItem();
        StartCoroutine(PlayCutscene());

    }

    private IEnumerator PlayCutscene()
    {
        yield return new WaitForSeconds(delayBeforeCutscene);

        if (cutscene != null)
        {
            cutscene.Play();
        }
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

    public void Ambush()
    {
        isAmbushing = true;
        movement = ObjectMovement.bouncing;
    }

    public void CheckAmbush()
    {
        if (state == EnemyState.ambush && !isAmbushing) {
            Ambush();
        } else {
            bounceHeight = 0;
        }
    }
}
