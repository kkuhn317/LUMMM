using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionTriggerEvent : MonoBehaviour
{
    [SerializeField] UnityEvent onPlayerEnter;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // check if player enters the trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            onPlayerEnter.Invoke();
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
