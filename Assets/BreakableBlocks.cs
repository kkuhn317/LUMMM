using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableBlocks : MonoBehaviour
{

    public GameObject BlockPiece;
    public AudioClip breakSound;

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

        GetComponent<AudioSource>().PlayOneShot(breakSound);
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
