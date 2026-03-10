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

        Invoke(nameof(loseLife), timeBeforeLoseLife);
        
    }

    void loseLife()
    {
        var levelFlow = GameManager.Instance != null
            ? GameManager.Instance.GetSystem<LevelFlowController>()
            : FindObjectOfType<LevelFlowController>(true);

        if (levelFlow != null)
            levelFlow.TriggerDeath();
        else
            Debug.LogError("DeadMario: LevelFlowController not found!");

        // Ensure GameManager.Instance is not null before calling DecrementLives()
        /*if (GameManager.Instance != null)
        {
            // GameManager.Instance.DecrementLives();
            GameManagerRefactored.Instance.GetSystem<LevelFlowController>()?.TriggerDeath();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }*/
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
