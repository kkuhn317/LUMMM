using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Burner : MonoBehaviour
{
    public Animator animator;
    public GameObject flame;

    public float burnTime = 10f;
    private float burnTimer = 0f;

    private bool isBurning = false;

    private void Update()
    {
        if (isBurning)
        {
            burnTimer += Time.deltaTime;
            if (burnTimer >= burnTime)
            {
                StopBurn();
            }
        }
    }

    public void StartBurn()
    {
        if (!isBurning)
        {
            isBurning = true;
            burnTimer = 0f;
            animator.SetFloat("duration", burnTimer);
            flame.SetActive(true);
        }
    }

    public void StopBurn()
    {
        if (isBurning)
        {
            isBurning = false;
            animator.SetFloat("duration", burnTimer);
            flame.SetActive(false);
        }
    }
}
