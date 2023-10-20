using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    private Vector3 originalPosition;
    private Vector3 checkpointPosition;

    // Singleton pattern to ensure there's only one CheckpointManager
    public static CheckpointManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    public void SetOriginalPosition(Vector3 position)
    {
        originalPosition = position;
    }

    public Vector3 GetRespawnPosition()
    {
        return checkpointPosition != Vector3.zero ? checkpointPosition : originalPosition;
    }
}
