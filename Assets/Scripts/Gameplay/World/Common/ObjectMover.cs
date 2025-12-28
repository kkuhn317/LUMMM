using System.Collections;
using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    public GameObject objectToMove;
    public Vector3 moveDirection;
    public float moveSpeed;
    public float moveDistance;

    private Vector3 originalPosition;
    
    // TODO: currently unused. Remove if not needed

    private void Start()
    {
        originalPosition = objectToMove.transform.position;
    }

    public void MoveObject()
    {
        StartCoroutine(MoveObjectCoroutine());
    }

    private IEnumerator MoveObjectCoroutine()
    {
        Vector3 startPosition = objectToMove.transform.position;
        Vector3 targetPosition = startPosition + (moveDirection.normalized * moveDistance);
        float distanceMoved = 0f;

        while (distanceMoved < moveDistance)
        {
            float moveAmount = moveSpeed * Time.deltaTime;
            Vector3 newPosition = objectToMove.transform.position + moveDirection.normalized * moveAmount;
            objectToMove.transform.position = newPosition;
            distanceMoved += moveAmount;

            yield return null;
        }

        // Ensure the object reaches the exact target position
        objectToMove.transform.position = targetPosition;
    }
}
