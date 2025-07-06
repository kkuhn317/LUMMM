using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class LeverController : UseableObject
{
    public enum MovementMode { Parallel, Sequential }
    public MovementMode movementMode = MovementMode.Parallel;

    public Transform leverHandle;
    public Vector3 leverTargetRotation;

    public Transform[] objectsToMove;
    public Vector3[] targetDistances;

    public float rotationSpeed = 50f;
    public float moveSpeed = 5f;

    [Min(0f)] public float minTimeBetweenUses = 0f;
    private float lastUseTime = Mathf.NegativeInfinity;

    private Quaternion initialLeverRotation;
    private Vector3[] initialPositions;

    private AudioSource audioSourcePullerRotate;
    public AudioSource audioSourceBarrierMove;

    public AudioClip pullerAudioClip;
    public AudioClip barriermoveAudioClip;

    [SerializeField] UnityEvent onLeverPull;
    [SerializeField] UnityEvent onLeverReset;

    private bool isLocked = false;

    private void Start()
    {
        initialLeverRotation = leverHandle.rotation;

        initialPositions = new Vector3[objectsToMove.Length];
        for (int i = 0; i < objectsToMove.Length; i++)
            initialPositions[i] = objectsToMove[i].localPosition;

        audioSourcePullerRotate = GetComponent<AudioSource>();
        audioSourceBarrierMove = GetComponent<AudioSource>();

        if (keyActivate != null)
            keyActivate.SetActive(false);
    }

    protected override void UseObject()
    {
        if (isLocked || Time.time - lastUseTime < minTimeBetweenUses)
            return;

        lastUseTime = Time.time;
        isLocked = true;
        StartCoroutine(RotateLever(hasUsed ? leverTargetRotation : initialLeverRotation.eulerAngles));
        onLeverPull.Invoke();
    }

    protected override void ResetObject()
    {
        if (isLocked || Time.time - lastUseTime < minTimeBetweenUses)
            return;

        lastUseTime = Time.time;
        isLocked = true;
        StartCoroutine(RotateLever(initialLeverRotation.eulerAngles));
        onLeverReset.Invoke();
    }

    private IEnumerator RotateLever(Vector3 targetEuler)
    {
        Quaternion target = Quaternion.Euler(targetEuler);

        // 🔊 Play only once, regardless of mode
        if (audioSourcePullerRotate && pullerAudioClip)
            audioSourcePullerRotate.PlayOneShot(pullerAudioClip);

        while (Quaternion.Angle(leverHandle.rotation, target) > 0.01f)
        {
            leverHandle.rotation = Quaternion.RotateTowards(leverHandle.rotation, target, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        // Calculate target positions based on lever state
        bool hasUsed = leverHandle.rotation == Quaternion.Euler(leverTargetRotation);
        Vector3[] targetPositions = new Vector3[objectsToMove.Length];
        for (int i = 0; i < objectsToMove.Length; i++)
        {
            targetPositions[i] = hasUsed ? initialPositions[i] + targetDistances[i] : initialPositions[i];
        }

        if (movementMode == MovementMode.Parallel)
            yield return StartCoroutine(MoveAllParallel(targetPositions));
        else
            yield return StartCoroutine(MoveSequentially(targetPositions));

        isLocked = false;
    }

    private IEnumerator MoveAllParallel(Vector3[] targetPos)
    {
        if (audioSourceBarrierMove && barriermoveAudioClip)
            audioSourceBarrierMove.PlayOneShot(barriermoveAudioClip);

        bool moving = true;
        while (moving)
        {
            moving = false;
            for (int i = 0; i < objectsToMove.Length; i++)
            {
                objectsToMove[i].localPosition = Vector3.MoveTowards(objectsToMove[i].localPosition, targetPos[i], moveSpeed * Time.deltaTime);
                if (Vector3.Distance(objectsToMove[i].localPosition, targetPos[i]) > 0.01f)
                    moving = true;
            }
            yield return null;
        }
    }

    private IEnumerator MoveSequentially(Vector3[] targetPos)
    {
        int count = objectsToMove.Length;
        for (int j = 0; j < count; j++)
        {
            int i = hasUsed ? j : count - 1 - j;

            if (audioSourceBarrierMove && barriermoveAudioClip)
                audioSourceBarrierMove.PlayOneShot(barriermoveAudioClip);

            while (Vector3.Distance(objectsToMove[i].localPosition, targetPos[i]) > 0.01f)
            {
                objectsToMove[i].localPosition = Vector3.MoveTowards(objectsToMove[i].localPosition, targetPos[i], moveSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (objectsToMove != null && targetDistances != null && objectsToMove.Length == targetDistances.Length)
        {
            for (int i = 0; i < objectsToMove.Length; i++)
            {
                Vector3 initialPos = Application.isPlaying ? initialPositions[i] : objectsToMove[i].localPosition;
                Vector3 targetPos = initialPos + targetDistances[i];
                Gizmos.DrawWireSphere(objectsToMove[i].parent ? objectsToMove[i].parent.TransformPoint(targetPos) : targetPos, 0.5f);
            }
        }
    }
}