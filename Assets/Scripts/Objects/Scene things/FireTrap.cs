using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTrap : MonoBehaviour
{
    public TrapDirection TrapDirection;
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private float fireDelay = 2f;
    [SerializeField] private LayerMask playerLayer;

    private bool isTimedTrap;
    private TrapDirection trapDirection = TrapDirection.Right;

    private void Start()
    {
        isTimedTrap = fireDelay > 0f;
        if (isTimedTrap)
        {
            InvokeRepeating(nameof(TriggerFire), fireDelay, fireDelay);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isTimedTrap && playerLayer == (playerLayer | (1 << other.gameObject.layer)))
        {
            TriggerFire();
        }
    }

    private void TriggerFire()
    {
        Vector3 position = transform.position;
        Quaternion rotation = Quaternion.identity;
        switch (trapDirection)
        {
            case TrapDirection.Right:
                position += new Vector3(1f, 0f, 0f);
                break;
            case TrapDirection.Left:
                position += new Vector3(-1f, 0f, 0f); // Changed from (1f, 0f, 0f)
                rotation = Quaternion.Euler(0f, 0f, 180f);
                break;
            case TrapDirection.Up:
                position += new Vector3(0f, 1f, 0f);
                rotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case TrapDirection.Down:
                position += new Vector3(0f, -1f, 0f);
                rotation = Quaternion.Euler(0f, 0f, -90f);
                break;
        }
        Instantiate(firePrefab, position, rotation);
    }

    public void SetTrapDirection(TrapDirection direction)
    {
        trapDirection = direction;
    }
}
    public enum TrapDirection
    {
        Right,
        Left,
        Up,
        Down
    }
