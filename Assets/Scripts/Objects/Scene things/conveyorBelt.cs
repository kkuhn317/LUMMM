using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class conveyorBelt : MonoBehaviour
{

    public GameObject left;

    public GameObject middle;
    public GameObject right;

    public BoxCollider2D moveArea;

    [HideInInspector]
    public int length;

    public float speed;

    Rigidbody2D rBody;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Animator>().SetFloat("Speed", speed);
        rBody = GetComponent<Rigidbody2D>();
    }


    Transform FindWithTag(Transform root, string tag)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (t.CompareTag(tag)) return t;
        }
        return null;
    }

    private void Update() {
        // do some weird movement to simulate a conveyor belt
        // still can't belive this actually works
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        if (player.transform.parent == transform)
        {
            Vector3 pos = transform.position;
            transform.position += new Vector3(speed * Time.deltaTime, 0);
            player.transform.parent = null;
            transform.position = pos;
            player.transform.parent = transform;
        }
    }

    public void ChangeLength(int length) {
        left.transform.localPosition = new Vector3(-(((float)length-1) / 2), 0, 0);
        middle.GetComponent<SpriteRenderer>().size = new Vector2(length-2, 1);
        right.transform.localPosition = new Vector3((((float)length-1) / 2), 0, 0);

        GetComponent<BoxCollider2D>().size = new Vector2(length, 1);
        moveArea.size = new Vector2(length+0.1f, moveArea.size.y);
    }
}
