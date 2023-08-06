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

    private void Start()
    {
        initialRotation = objectToRotate.rotation;
        initialPosition = objectToMove.position;
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.Z))
        {
            if (!isRotating && !isMoving)
            {
                RotateObject(targetRotation);
            }
            else if (isRotating && !isMoving)
            {
                RotateObject(initialRotation.eulerAngles);
            }
            else if (!isRotating && isMoving)
            {
                MoveObject(targetPosition);
            }
            else if (isRotating && isMoving)
            {
                MoveObject(initialPosition);
            }
        }

        if (isRotating)
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
    }

    private void MoveObject(Vector3 targetPos)
    {
        targetPosition = targetPos;
        isMoving = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}
