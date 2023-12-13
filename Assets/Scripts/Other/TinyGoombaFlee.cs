using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TinyGoombaFlee : MonoBehaviour
{
    public Vector2 targetPoint;
    public PlayableDirector goombaOnPipeDirector;
    public float scaredIdleDuration = 0.5f;

    private Vector2 startPosition;
    private Animator animator;
    private Goomba goomba;
    private bool isDone = false;

    // Start is called before the first frame update
    void Start()
    {
        startPosition = targetPoint;
        animator = GetComponent<Animator>();
        goomba = GetComponent<Goomba>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckAndMove();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(targetPoint.x, targetPoint.y, 0), 0.05f);
    }

    void CheckAndMove()
    {
        if (transform.childCount == 0 && !isDone)
        {
            StartCoroutine(MoveToTargetPoint());
        }   
    }

    IEnumerator MoveToTargetPoint()
    {
        isDone = true;

        animator.SetBool("isScaredIdle", true);

        goomba.velocity.x = 0;
        goomba.movement = ObjectPhysics.ObjectMovement.still;

        yield return new WaitForSeconds(scaredIdleDuration);

        animator.SetBool("isScared", true);

        float elapsedTime = 0f;
        float duration = 0.75f;

        Vector3 initialPosition = transform.position;
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y, transform.position.z);

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(initialPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;

            // Check if the enemy state changes to "crushed" during movement
            if (goomba.state == Goomba.EnemyState.crushed)
            {
                if (goombaOnPipeDirector != null && goombaOnPipeDirector.state == PlayState.Playing)
                {
                    // Stop the cutscene if it's playing
                    goombaOnPipeDirector.Stop();
                }

                yield break; // Exit the coroutine early
            }

            yield return null;
        }

        // Ensure the final position is exactly the target position
        transform.position = targetPosition;

        // Check if there is a Playable Director assigned and the enemy state is not "crushed"
        if (goombaOnPipeDirector != null && goomba.state != Goomba.EnemyState.crushed)
        {
            // Play the assigned cutscene
            goombaOnPipeDirector.Play();
        }
    }

}
