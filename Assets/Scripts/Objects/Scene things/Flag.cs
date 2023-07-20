using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour
{

    [HideInInspector]
    public float height;
    public GameObject flag;
    public GameObject pole;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeHeight(float height)
    {
        this.height = height;
        
        // flag
        flag.transform.localPosition = new Vector3(-0.5f, height - 0.55f, 0);

        // pole
        pole.GetComponent<SpriteRenderer>().size = new Vector2(0.5f, height);
        pole.transform.localPosition = new Vector3(0, (height+1) / 2, 0);

        // collider
        GetComponent<BoxCollider2D>().size = new Vector2(0.25f, height);
        GetComponent<BoxCollider2D>().offset = new Vector2(0, (height + 1) / 2);
    }
}
