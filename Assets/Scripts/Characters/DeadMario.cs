using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeadMario : MonoBehaviour
{

    public float gravity;
    public Vector2 velocity;

    public GameObject gameOverScreen;

    // Start is called before the first frame update
    void Start()
    {

        Invoke("loseLife", 1);
        
    }

    void loseLife()
    {
        GameManager.Instance.DecrementLives();
    }



    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.localPosition;
        Vector3 scale = transform.localScale;

        pos.y += velocity.y * Time.deltaTime;

        velocity.y -= gravity * Time.deltaTime;

        transform.localPosition = pos;
    }

}
