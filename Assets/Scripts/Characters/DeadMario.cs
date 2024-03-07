using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeadMario : MonoBehaviour
{
    public float gravity;
    public Vector2 velocity;
    public int timeBeforeLoseLife = 1;

    public GameObject gameOverScreen;

    // Start is called before the first frame update
    void Start()
    {

        Invoke("loseLife", timeBeforeLoseLife);
        
    }

    void loseLife()
    {
        // Ensure GameManager.Instance is not null before calling DecrementLives()
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DecrementLives();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.localPosition;
        Vector3 scale = transform.localScale;

        pos.y += velocity.y * Time.deltaTime;
        pos.x += velocity.x * Time.deltaTime;

        velocity.y -= gravity * Time.deltaTime;

        transform.localPosition = pos;
    }

}
