using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class WaterfallBlockManager : MonoBehaviour
{
    public Transform block1;
    public Transform block2;

    public Vector2 block1SnapPos;
    public Vector2 block2SnapPos;

    private bool block1Done = false;
    private bool block2Done = false;
    private bool cutsceneStarted = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (block1.position.x >= block1SnapPos.x && !block1Done)
        {
            block1.position = block1SnapPos;
            block1.GetComponentInChildren<Pushable>().enabled = false;
            block1Done = true;
        }
        if (block2.position.x <= block2SnapPos.x && !block2Done)
        {
            block2.position = block2SnapPos;
            block2.GetComponentInChildren<Pushable>().enabled = false;
            block2Done = true;
        }
        if (block1Done && block2Done && !cutsceneStarted)
        {
            cutsceneStarted = true;
            Invoke(nameof(StartCutscene), 0.2f);
            
        }
    }

    void StartCutscene()
    {
        GetComponent<PlayableDirector>().Play();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(block1SnapPos, 0.2f);
        Gizmos.DrawSphere(block2SnapPos, 0.2f);
    }
}
