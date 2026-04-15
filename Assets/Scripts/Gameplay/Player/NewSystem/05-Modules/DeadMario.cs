using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeadMario : MonoBehaviour
{
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float timeBeforeLoseLife = 1f;
    public Vector2 velocity;

    public GameObject gameOverScreen;

    // Start is called before the first frame update
    void Start()
    {

        Invoke(nameof(LoseLife), timeBeforeLoseLife);
        
    }

    void LoseLife()
    {
        var levelFlow = GameManager.Instance != null
            ? GameManager.Instance.GetSystem<LevelFlowController>()
            : FindObjectOfType<LevelFlowController>(true);

        if (levelFlow != null)
            levelFlow.TriggerDeath();
        else
            Debug.LogError("DeadMario: LevelFlowController not found!");
    }


    // Update is called once per frame
    void Update()
    {
        velocity.y -= gravity * Time.deltaTime;
        transform.position += new Vector3(velocity.x, velocity.y) * Time.deltaTime;
    }

}
