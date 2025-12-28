using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TinyGoombaFlee : MonoBehaviour
{
    public Vector2 targetPoint;
    public PlayableDirector goombaOnPipeDirector;
    public float scaredIdleDuration = 0.5f;
    public AudioClip scaredGoombaSound;

    private Vector2 startPosition;
    private Animator animator;
    private Goomba goomba;
    private bool isDone = false;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        startPosition = targetPoint;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        goomba = GetComponent<Goomba>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckAndMove();
        FaceNearestPlayer();
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

    void FaceNearestPlayer()
    {
        GameObject nearestPlayer = FindNearestPlayer();
        if (nearestPlayer == null) return;

        Vector3 playerPosition = nearestPlayer.transform.position;

        // Flip the Goomba's local scale based on the player's position
        Vector3 scale = transform.localScale;
        scale.x = playerPosition.x > transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    GameObject FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject nearestPlayer = null;
        float shortestDistance = float.MaxValue;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestPlayer = player;
            }
        }

        return nearestPlayer;
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

        audioSource.PlayOneShot(scaredGoombaSound);

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
