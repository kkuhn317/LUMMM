// For any damaging projectile that moves in a straight line
// Magic, Bullet Bill, Cannonball, etc.
// To set it up, set gravity to 0, movement to sliding, and wall and floor masks to what you want it to collide with

using UnityEngine;

public class Magic : EnemyAI
{
    public GameObject smoke;
    public float smokeSize = 0.5f;

    public float rotateSpeed = 1.0f;

    public bool gone = false;

    public GameObject pigMario;

    protected override void Update() {
        base.Update();

        // rotate around
        transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);
    }

    // When we hit a wall, we should poof away
    protected override void onTouchWall(GameObject other){
        Dissapear();
    }

    public override void Land(GameObject other = null) {
        Dissapear();
    }

    protected override void hitOnSide(GameObject player)
    {  
        player.GetComponent<MarioMovement>().TransformIntoObject(pigMario);
        Dissapear();
    }

    private void Dissapear() {
        if (gone) return;
        gone = true;
    
        Instantiate(smoke, transform.position, Quaternion.identity);
        smoke.transform.localScale = new Vector3(smokeSize, smokeSize, smokeSize);

        // play sound
        GetComponent<AudioSource>().Play();

        // disable sprite
        GetComponent<SpriteRenderer>().enabled = false;

        // Deactivate all child GameObjects
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }

        // destroy
        Destroy(gameObject, 1f);

        // disable collider
        GetComponent<Collider2D>().enabled = false;

        // disable movement
        realVelocity = Vector2.zero;
    }
    
}