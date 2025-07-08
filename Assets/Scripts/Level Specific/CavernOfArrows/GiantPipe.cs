using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ArrowShot
{
    [Tooltip("The transform in the scene where the arrow will be spawned from.")]
    public Transform firePoint;

    [Tooltip("The angle where the arrow will be shot")]
    public float angle;

    [Tooltip("The arrow prefab that will be instantiated.")]
    public GameObject arrowPrefab;

    [Tooltip("The speed at which the arrow travels.")]
    public float arrowSpeed = 10f;

    [Tooltip("Delay before this arrow is fired during the sequence.")]
    public float delayBeforeShot = 0.5f;
}

[System.Serializable]
public class ArrowSequence
{
    [Tooltip("List of individual arrow shots in this sequence.")]
    public List<ArrowShot> shots;

    [Tooltip("Delay after completing this sequence before the next begins.")]
    public float delayAfterSequence = 2f;
}

public class GiantPipe : MonoBehaviour
{
    [Tooltip("The sequences of arrow shots this pipe will fire in order.")]
    public List<ArrowSequence> sequences;

    [Tooltip("Sound effect to play when an arrow is fired.")]
    public AudioClip shootSFX;

    [Tooltip("Sound effect to play when reloading before firing an arrow.")]
    public AudioClip reloadSFX;

    [Tooltip("How long this pipe will continue firing before stopping (in seconds).")]
    public float totalRunTime = 30f;

    [Tooltip("If true, the sequences will repeat in a loop.")]
    public bool loopSequences = true;

    [Tooltip("Sprite Renderer used to place the arrow below the pipe")]
    public SpriteRenderer pipeSpriteRenderer;

    [Tooltip("Explosions gamoebject that should appear when the pipe starts shooting")]
    public GameObject arrowExplosionParent;

    private AudioSource audioSource;
    private Animator animator;
    private bool isRunning = false;
    private bool isVisible = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    public void GiantPipeSequence()
    {
        StartCoroutine(RunAllSequences());
    }

    private IEnumerator RunAllSequences()
    {
        animator?.SetTrigger("reload");
        audioSource?.PlayOneShot(reloadSFX);

        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("reload") &&
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);

        isRunning = true;
        float startTime = Time.time;

        arrowExplosionParent.SetActive(true);

        do
        {
            foreach (var sequence in sequences)
            {
                foreach (var shot in sequence.shots)
                {
                    yield return new WaitForSeconds(shot.delayBeforeShot);
                    ShootArrow(shot);
                }

                yield return new WaitForSeconds(sequence.delayAfterSequence);
            }

            if (!loopSequences || Time.time - startTime >= totalRunTime)
                break;

        } while (loopSequences && Time.time - startTime < totalRunTime);

        isRunning = false;
        arrowExplosionParent.SetActive(false);
    }

    private void ShootArrow(ArrowShot shot)
    {
        if (shot.firePoint == null || shot.arrowPrefab == null)
            return;

        GameObject arrow = Instantiate(shot.arrowPrefab, shot.firePoint.position, shot.firePoint.rotation);
        var arrowScript = arrow.GetComponent<Arrow>();
        var arrowRenderer = arrow.GetComponentInChildren<SpriteRenderer>();

        // The arrow sprite will one sorting order number below the pipe sprite renderer's sorting order number to be shown "inside" the pipe
        arrowRenderer.sortingOrder = pipeSpriteRenderer.sortingOrder - 1;

        if (arrowScript != null)
        {
            float angle = shot.angle;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

            float speed = Mathf.Abs(shot.arrowSpeed);
            if (shot.arrowSpeed < 0)
                dir = -dir;

            ObjectPhysics physics = arrow.GetComponent<ObjectPhysics>();
            if (physics != null)
            {
                physics.realVelocity = dir * speed;
            }
        }

        audioSource?.PlayOneShot(shootSFX);
    }

    private void OnBecameVisible()
    {
        isVisible = true;
        if (!isRunning)
        {
            StartCoroutine(RunAllSequences());
        }
    }

    void OnBecameInvisible()
    {
        isVisible = false;
    }

    void OnDrawGizmos()
    {
        if (sequences == null) return;

        Gizmos.color = Color.red;
        foreach (var sequence in sequences)
        {
            foreach (var shot in sequence.shots)
            {
                if (shot.firePoint != null)
                {
                    Gizmos.DrawSphere(shot.firePoint.position, 0.1f);

                    float angle = shot.angle;
                    Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

                    Gizmos.DrawRay(shot.firePoint.position, dir * 0.5f);
                }
            }
        }
    }
}