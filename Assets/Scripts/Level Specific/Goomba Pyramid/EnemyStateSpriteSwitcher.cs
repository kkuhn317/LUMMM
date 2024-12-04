using System.Collections.Generic;
using UnityEngine;

public class EnemyStateSpriteSwitcher : MonoBehaviour
{
    private BoxCollider2D boxCollider2D;
    private List<ObjectPhysics> objectsInTrigger = new List<ObjectPhysics>();

    private void Start() 
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ObjectPhysics objectPhysics = other.GetComponent<ObjectPhysics>();
        if (objectPhysics != null)
        {
            objectsInTrigger.Add(objectPhysics);
            UpdateSpriteSwapAreaState();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ObjectPhysics objectPhysics = other.GetComponent<ObjectPhysics>();
        if (objectPhysics != null)
        {
            objectsInTrigger.Remove(objectPhysics);
            UpdateSpriteSwapAreaState();
        }
    }

    private void Update()
    {
        UpdateSpriteSwapAreaState();
    }

    private void UpdateSpriteSwapAreaState()
    {
        bool hasFallingObjects = false;

        foreach (ObjectPhysics obj in objectsInTrigger)
        {
            if (obj != null && obj.objectState == ObjectPhysics.ObjectState.falling)
            {
                hasFallingObjects = true;
                break;
            }
        }

        if (boxCollider2D != null)
        {
            boxCollider2D.enabled = hasFallingObjects;
        }
    }
}
