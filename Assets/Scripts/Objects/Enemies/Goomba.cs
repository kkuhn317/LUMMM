using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Goomba : EnemyAI
{
    public enum EnemyState {
        walking,
        crushed
    }

    public EnemyState state = EnemyState.walking;

    public bool stompable = true;

    public bool shouldntEnemyMoveWhenDie = true;
    protected bool crushed = false;
    private float deathTimer = 0;
    public bool shouldDestroyAfterCrush = true;
    public float timeBeforeDestroy = 1.0f;

    [Header("Cutscene")]
    public PlayableDirector cutscene;
    public float delayBeforeCutscene = 1.0f;

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

    protected override void hitByGroundPound(MarioMovement player)
    {
        KnockAway(false);
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

        crushed = true;       

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

        if (crushed && shouldDestroyAfterCrush) {
            
            if (deathTimer <= timeBeforeDestroy) {

                deathTimer += Time.deltaTime;

            } else {
                
                crushed = false;

                Destroy(this.gameObject);
            }
        }
    }
}