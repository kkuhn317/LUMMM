using UnityEngine;

public class LeverController : MonoBehaviour
{
    public Transform objectToRotate; // The GameObject to rotate
    public Vector3 targetRotation; // The target rotation after player interaction
    public Transform objectToMove; // The GameObject to move after rotation
    public Vector3 targetPosition; // The target position after player interaction
    public float rotationSpeed = 50f; // Speed at which the object rotates
    public float moveSpeed = 5f; // Speed at which the object moves after rotation

    private bool isPlayerInRange;
    private bool isRotating;
    private bool isMoving;
    private Quaternion initialRotation;
    private Vector3 initialPosition;

    private AudioSource audioSourcePullerRotate;
    public AudioSource audioSourceBarrierMove;

    public AudioClip pullerAudioClip; // Audio clip for puller rotation
    public AudioClip barriermoveAudioClip;   // Audio clip for barrier movement

    [Header("Activation key")]
    public GameObject keyActivate;
    private bool hasPulledLever = false;

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

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.Z))
        {
            if (!isRotating && !isMoving && !hasPulledLever)
            {
                if (keyActivate != null)
                {
                    keyActivate.SetActive(false);
                }

                RotateObject(targetRotation); // The puller rotates
                hasPulledLever = true; // Set the flag to true after pulling the lever
            }
            /*else if (isRotating && !isMoving)
            {
                RotateObject(initialRotation.eulerAngles);
            }*/
            else if (!isRotating && isMoving)
            {
                MoveObject(targetPosition); // The object moves to the target position
            }
            /*else if (isRotating && isMoving)
            {
                MoveObject(initialPosition);
            }*/
        }

        if (isRotating) // rotating logic
        {
            // Rotate the object smoothly over time
            float step = rotationSpeed * Time.deltaTime;
            objectToRotate.rotation = Quaternion.RotateTowards(objectToRotate.rotation, Quaternion.Euler(targetRotation), step);

            if (Quaternion.Angle(objectToRotate.rotation, Quaternion.Euler(targetRotation)) < 0.01f)
            {
                objectToRotate.rotation = Quaternion.Euler(targetRotation);
                isRotating = false;

                if (!isMoving)
                {
                    MoveObject(targetPosition);
                }
            }
        }

        if (isMoving)
        {
            // Move the object smoothly over time
            float step = moveSpeed * Time.deltaTime;
            objectToMove.position = Vector3.MoveTowards(objectToMove.position, targetPosition, step);

            if (Vector3.Distance(objectToMove.position, targetPosition) < 0.01f)
            {
                objectToMove.position = targetPosition;
                isMoving = false;
            }
        }
    }

    private void RotateObject(Vector3 targetRot)
    {
        targetRotation = targetRot;
        isRotating = true;

        if (audioSourcePullerRotate != null && pullerAudioClip != null)
        {
            audioSourcePullerRotate.PlayOneShot(pullerAudioClip);
        }
    }

    private void MoveObject(Vector3 targetPos)
    {
        targetPosition = targetPos;
        isMoving = true;

        if (audioSourceBarrierMove != null && barriermoveAudioClip != null)
        {
            audioSourceBarrierMove.PlayOneShot(barriermoveAudioClip);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            // Activate the keyActivate when the player enters the trigger zone and haven't pull the level 
            if (keyActivate != null && !hasPulledLever) {
                keyActivate.SetActive(true);
            } 
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;

            // Deactivate the keyActivate when the player exits the trigger zone
            if (keyActivate != null)
            {
                keyActivate.SetActive(false);
            }
        }
    }
}
