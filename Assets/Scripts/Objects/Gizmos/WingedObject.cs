using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingedObject : MonoBehaviour
{
    public Wing[] wings;
    public float moveDistance = 3f;
    public float moveSpeed = 5f;

    private bool fell = false;

    private List<GameObject> children = new List<GameObject>();

    void Start() {
        foreach (Transform child in transform) {
            // if it isn't in wings list, add it to children list
            if (System.Array.IndexOf(wings, child.GetComponent<Wing>()) == -1) {
                children.Add(child.gameObject);
            }
        }
    }

    public void WingsFall()
    {
        foreach (Wing wing in wings)
        {
            if (wing == null) {
                continue;
            }
            // make it not a child of the block
            wing.transform.parent = null;
            wing.Fall();
        }
    }

    void Update()
    {
        if (fell) {
            return;
        }
        // move up and down
        transform.position += Vector3.up * Mathf.Cos(Time.time * moveSpeed) * Time.deltaTime * moveDistance;

        foreach (GameObject child in children) {
            // remove if child is null
            if (child == null) {
                children.Remove(child);
            }
        }
        if (children.Count == 0) {
            fell = true;
            WingsFall();
        }
    }
}
