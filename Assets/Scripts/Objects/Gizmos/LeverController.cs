using UnityEngine;
using UnityEngine.Events;

public class LeverController : UseableObject
{
    public Transform objectToRotate; // The GameObject to rotate
    public Vector3 targetRotation; // The target rotation after player interaction
    public Transform objectToMove; // The GameObject to move after rotation
    public Vector3 targetPosition; // The target position after player interaction
    public float rotationSpeed = 50f; // Speed at which the object rotates
    public float moveSpeed = 5f; // Speed at which the object moves after rotation

    private bool isRotating;
    private bool isMoving;
    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private Vector3 currentTargetRot;   // Will be set to either targetRotation or initialRotation
    private Vector3 currentTargetPos;   // Will be set to either targetPosition or initialPosition

    private AudioSource audioSourcePullerRotate;
    public AudioSource audioSourceBarrierMove;

    public AudioClip pullerAudioClip; // Audio clip for puller rotation
    public AudioClip barriermoveAudioClip;   // Audio clip for barrier movement

    // Used to do more custom things when the lever is pulled
    [SerializeField] UnityEvent onLeverPull;
    [SerializeField] UnityEvent onLeverReset;

    private void Start()
    {
        initialRotation = objectToRotate.rotation;
        initialPosition = objectToMove.position;

        audioSourcePullerRotate = GetComponent<AudioSource>();
        audioSourceBarrierMove = GetComponent<AudioSource>();

        // Deactivate the objectToActivate initially
        if (keyActivate != null)
        {
            keyActivate.SetActive(false);
        }
    }

    protected override void UseObject()
    {
        RotateHandle(targetRotation);   // Rotate lever handle
        onLeverPull.Invoke();
    }

    protected override void ResetObject()
    {
        RotateHandle(initialRotation.eulerAngles); // Reset lever rotation
        onLeverReset.Invoke();
    }

    private void Update()
    {
        if (isRotating) // rotating logic
        {
            // Rotate the object smoothly over time
            float step = rotationSpeed * Time.deltaTime;
            objectToRotate.rotation = Quaternion.RotateTowards(objectToRotate.rotation, Quaternion.Euler(currentTargetRot), step);

            if (Quaternion.Angle(objectToRotate.rotation, Quaternion.Euler(currentTargetRot)) < 0.01f)
            {
                objectToRotate.rotation = Quaternion.Euler(currentTargetRot);
                isRotating = false;

                if (!isMoving)
                {
                    if (hasUsed) {
                        MoveObject(targetPosition);
                    }
                    else {
                        MoveObject(initialPosition);
                    }
                }
            }
        }

        if (isMoving)
        {
            // Move the object smoothly over time
            float step = moveSpeed * Time.deltaTime;
            objectToMove.position = Vector3.MoveTowards(objectToMove.position, currentTargetPos, step);

            if (Vector3.Distance(objectToMove.position, currentTargetPos) < 0.01f)
            {
                objectToMove.position = currentTargetPos;
                isMoving = false;
            }
        }
    }

    private void RotateHandle(Vector3 targetRot)
    {
        currentTargetRot = targetRot;
        isRotating = true;

        if (audioSourcePullerRotate != null && pullerAudioClip != null)
        {
            audioSourcePullerRotate.PlayOneShot(pullerAudioClip);
        }
    }

    private void MoveObject(Vector3 targetPos)
    {
        currentTargetPos = targetPos;
        isMoving = true;

        if (audioSourceBarrierMove != null && barriermoveAudioClip != null)
        {
            audioSourceBarrierMove.PlayOneShot(barriermoveAudioClip);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
    }
}
