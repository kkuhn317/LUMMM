using System.Collections;
using UnityEngine;

public class TimedSpawnAndMoveObject : MonoBehaviour
{
    public GameObject targetObject; // this is the object you want to activate and move
    public Transform destination; // the target object to move the spikes to
    public float requiredStayTime = 2.0f; // just the time the player must stay in the trigger
    public float moveSpeed = 5.0f; // speed at which spikes move to the target

    private bool isPlayerInTrigger = false;
    private float stayTimer = 0.0f;

    private void Start()
    {
        // Deactivate target object
        targetObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            Debug.Log("Player detected!");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            stayTimer = 0.0f;
        }
    }

    void Update()
    {
        if (isPlayerInTrigger)
        {
            stayTimer += Time.deltaTime;
            Debug.Log(stayTimer);

            if (stayTimer >= requiredStayTime)
            {
                StartCoroutine(ActivateAndMoveSpikes());
                stayTimer = 0.0f; // Reset timer after spawning spikes
                isPlayerInTrigger = false;
            }
        }
    }

    IEnumerator ActivateAndMoveSpikes()
    {
        Debug.Log("Object appears!");
        // Activate the spike object
        targetObject.SetActive(true);

        // Move the spike towards the target object
        while (Vector3.Distance(targetObject.transform.position, destination.position) > 0.1f)
        {
            Debug.Log("Object is moving to their destination");
            targetObject.transform.position = Vector3.MoveTowards(targetObject.transform.position, destination.position, moveSpeed * Time.deltaTime);
            yield return null; // Wait until the next frame
        }
    }
}
