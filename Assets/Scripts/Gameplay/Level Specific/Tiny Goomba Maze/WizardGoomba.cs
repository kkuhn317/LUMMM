using System;
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
    private Vector3 realPosition;
    private Vector3 prevPosition;
    private bool moveInitiated = false;
    public float moveSpeed = 1.0f;

    public GameObject magicPrefab;
    public Vector2 magicOffset;
    public float shootSpeed = 1.0f;

    public GameObject wandPrefab;
    public GameObject hitSpike;

    private GameObject player;

    public AudioClip shootSound;
    public AudioClip moveSound;
    private float t;
    private bool shootingAllowed = true;
    private AudioSource audioSource;
    private Animator animator;
    public Vector2[] positions;
    public Vector2[] divePositions; // When the wizard goomba dives down to stomp the player
    private int currentDivePos = -1;

    [Header("Bobbing")]

    public float bobbingSpeed = 1.0f; // how fast the wizard goomba bobs up and down
    public float bobbingHeight = 0.5f; // how high the wizard goomba bobs up and down

    public GameObject crushedMario;

    protected override void Start() {
        base.Start();

        player = GameObject.FindGameObjectWithTag("Player");

        moveToPosition = transform.position;
        realPosition = transform.position;

        // Get the Animator component
        animator = GetComponent<Animator>();

        // Get the AudioSource component
        audioSource = GetComponent<AudioSource>();

        // test
        StartCoroutine(Shoot());
    }

    protected override void Update() {
        base.Update();

        if (crushed || objectState == ObjectState.knockedAway || !shootingAllowed) return;

        if (moveToPosition != realPosition)
        {
            if (moveInitiated)
            {
                // move to position
                realPosition = Vector3.Lerp(prevPosition, moveToPosition, AnimationCurve.EaseInOut(0, 0, 1, 1).Evaluate(t));
                //realPosition = Vector3.Lerp(prevPosition, moveToPosition, t);
                t += Time.deltaTime * moveSpeed;    // Intentionally move faster when the distance is farther (looks more natural)
                //print("t increased by " + Time.deltaTime * moveSpeed);
            }
        }
        else
        {
            moveInitiated = false;
            t = 0f;

            if (currentDivePos == divePositions.Length - 1)
            {
                currentDivePos = -1;
            }
            else if (currentDivePos >= 0)
            {
                // Start moving to next dive position
                currentDivePos++;
                prevPosition = realPosition;
                moveToPosition = divePositions[currentDivePos];
                moveInitiated = true;
            }
        }

        transform.position = realPosition + new Vector3(0, Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight, 0);
    }

    public void MoveToPosition(int position) {
        if (currentDivePos >= 0) return; // don't move if diving down

        if (!crushed)
        { // Check if the WizardGoomba is not dead
            moveInitiated = true;
            prevPosition = realPosition;
            t = 0f;
            moveToPosition = positions[position];

            if (moveSound != null)
                audioSource.PlayOneShot(moveSound);
        }
    }

    public void DiveDown() {
        if (!crushed && currentDivePos == -1)
        { // Check if the WizardGoomba is not dead and is not already diving down
            moveInitiated = true;
            currentDivePos = 0;
            prevPosition = realPosition;
            t = 0f;
            moveToPosition = divePositions[0];

            if (moveSound != null)
                audioSource.PlayOneShot(moveSound);
        }
    }

    public void StartShooting() {
        StartCoroutine(Shoot());
    }

    public void StopShooting() {
        StopCoroutine(Shoot());
    }

    IEnumerator Shoot() {
        while (true ) {
            yield return new WaitForSeconds(shootRate);
            ShootMagic();
            animator.SetTrigger("magicAttack");
        }
    }

    void ShootMagic() {
        if (crushed || objectState == ObjectState.knockedAway || !shootingAllowed || !isVisible) return;
        if (player == null) return;

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
        // Prevent shooting permanently
        shootingAllowed = false;
        // Stop moving
        moveInitiated = false;
        t = 0f;
        gravity = 5f;

        if (hitSpike != null)
        {
            hitSpike = Instantiate(hitSpike, transform.position, Quaternion.identity);
        }

        // Instantiate the wandPrefab
        if (wandPrefab != null)
        {
            wandPrefab = Instantiate(wandPrefab, transform.position + (Vector3)magicOffset, Quaternion.identity);

            StartCoroutine(UpdateWandRotation());
        }
    }
    private IEnumerator UpdateWandRotation()
    {
        // Check the object state of the wand in a loop
        while (wandPrefab != null)
        {
            ObjectPhysics wandObjectPhysics = wandPrefab.GetComponent<ObjectPhysics>();

            // Check if the wand is in the "grounded" state
            if (wandObjectPhysics != null && wandObjectPhysics.objectState == ObjectPhysics.ObjectState.grounded)
            {
                // Rotate the wand by 90 degrees along the X-axis
                wandPrefab.transform.rotation = Quaternion.Euler(0f, 0f, 40f);

                // Stop the coroutine once the wand is rotated
                yield break;
            }

            // Wait for a short time before checking again
            yield return new WaitForSeconds(0.1f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision) {
        print("Collision with " + collision.gameObject.name);
        print("currentDivePos: " + currentDivePos);
        if (collision.gameObject.CompareTag("Player") && currentDivePos == 0) {
            // Broom hits player while diving down, so crush the player
            // Yes theres some hardcoding that only divePos 0 is allowed to crush the player, it can be changed later if needed
            collision.gameObject.GetComponent<MarioMovement>().TransformIntoObject(crushedMario);
        }
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

        Gizmos.color = Color.red;
        for (int i = 0; i < divePositions.Length; i++) {
            Gizmos.DrawSphere(divePositions[i], 0.02f);
        }
    }
}
