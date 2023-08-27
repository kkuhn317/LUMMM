using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using Unity.VisualScripting;
using UnityEngine;

public class WizardGoomba : Goomba
{
    [Header("Wizard Goomba")]

    public float shootRate = 1.0f;

    private Vector3 moveToPosition;
    private bool moveInitiated = false;
    public float moveSpeed = 1.0f;


    public GameObject magicPrefab;
    public Vector2 magicOffset;
    public float shootSpeed = 1.0f;

    private GameObject player;

    public AudioClip shootSound;
    public AudioClip moveSound;

    private float t;

    public Vector2[] positions;

    protected override void Start() {
        base.Start();

        player = GameObject.FindGameObjectWithTag("Player");

        moveToPosition = transform.position;

        // test
        StartCoroutine(Shoot());
    }

    protected override void Update() {
        base.Update();

        if (shouldDie || objectState == ObjectState.knockedAway) return;

        if (moveToPosition != transform.position) {
            if (moveInitiated) {
                // move to position
                transform.position = Vector3.Lerp(transform.position, moveToPosition, AnimationCurve.EaseInOut(0, 0, 1, 1).Evaluate(t));
                t += Time.deltaTime * moveSpeed;
            }
        } else {
            moveInitiated = false;
            t = 0f;
        }
    }

    public void MoveToPosition(int position) {
        moveInitiated = true;
        t = 0f;
        moveToPosition = positions[position];

        if (moveSound != null)
            GetComponent<AudioSource>().PlayOneShot(moveSound);
    }

    public void StartShooting() {
        StartCoroutine(Shoot());
    }

    public void StopShooting() {
        StopCoroutine(Shoot());
    }

    IEnumerator Shoot() {
        while (true) {
            yield return new WaitForSeconds(shootRate);
            ShootMagic();
        }
    }

    void ShootMagic() {
        if (shouldDie) return;

        // create magic
        GameObject magic = Instantiate(magicPrefab, transform.position + (Vector3)magicOffset, Quaternion.identity);

        // find direction to player
        Vector3 direction = player.transform.position - (transform.position + (Vector3)magicOffset);
        direction.Normalize();

        // move magic
        magic.GetComponent<EnemyAI>().realVelocity = direction * shootSpeed;

        // play sound
        if (shootSound != null)
            GetComponent<AudioSource>().PlayOneShot(shootSound);
    }

    protected override void hitByStomp(GameObject player)
    {
        base.hitByStomp(player);
        gravity = 30f;
    }

    // draw a debug point to show magic offset
    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + (Vector3)magicOffset, 0.02f);

        Gizmos.color = Color.green;
        for (int i = 0; i < positions.Length; i++) {
            Gizmos.DrawSphere(positions[i], 0.02f);
        }
    }

}
