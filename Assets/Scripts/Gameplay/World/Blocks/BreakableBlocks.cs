using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BreakableBlocks : MonoBehaviour
{
    public GameObject BlockPiece;
    public AudioClip breakSound;
    public UnityEvent OnBreak;

    [HideInInspector]
    public bool broken = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void Break()
    {
        if (broken) return;
        broken = true;
        Vector2 blockPos = transform.position;

        if (BlockPiece != null) {
        
            GameObject BlockPiece1 = (GameObject)Instantiate(BlockPiece) as GameObject;
            BlockPiece1.transform.position = new Vector3(blockPos.x - .25f, blockPos.y + .25f, -3);
            BlockPiece1.GetComponent<Rigidbody2D>().velocity = new Vector2(-4, 16);
            GameObject BlockPiece2 = (GameObject)Instantiate(BlockPiece) as GameObject;
            BlockPiece2.transform.position = new Vector3(blockPos.x + .25f, blockPos.y + .25f, -3);
            BlockPiece2.GetComponent<Rigidbody2D>().velocity = new Vector2(4, 16);
            GameObject BlockPiece3 = (GameObject)Instantiate(BlockPiece) as GameObject;
            BlockPiece3.transform.position = new Vector3(blockPos.x - .25f, blockPos.y - .25f, -3);
            BlockPiece3.GetComponent<Rigidbody2D>().velocity = new Vector2(-4, 10);
            GameObject BlockPiece4 = (GameObject)Instantiate(BlockPiece) as GameObject;
            BlockPiece4.transform.position = new Vector3(blockPos.x + .25f, blockPos.y - .25f, -3);
            BlockPiece4.GetComponent<Rigidbody2D>().velocity = new Vector2(4, 10);

        }

        if (breakSound != null) {
            GetComponent<AudioSource>().PlayOneShot(breakSound);
        }
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        OnBreak?.Invoke();
        Destroy(gameObject, 2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TriggerShakeCamera()
    {
        CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.ShakeCamera(0.5f, 0.2f, 1.0f, new Vector3(0, 1, 0));
        }
    }
}
