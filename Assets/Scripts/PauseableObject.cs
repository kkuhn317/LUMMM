using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseableObject : MonoBehaviour
{
    private PauseableMovement pauseableMovement;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame(); // Wait for one frame to let other components initialize.
        pauseableMovement = GetComponent<PauseableMovement>();
        GameManager.Instance.RegisterPauseableObject(pauseableMovement);
    }

    private void OnDestroy()
    {
        GameManager.Instance.UnregisterPauseableObject(pauseableMovement);
    }
}
