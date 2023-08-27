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

    protected override void Update() {
        base.Update();

        // rotate around
        transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);
    }

    // When we hit a wall, we should poof away
    protected override void onTouchWall(GameObject other){
        Dissapear();
    }

    public override void Land() {
        Dissapear();
    }

    protected override void hitByPlayer(GameObject player) {
        base.hitByPlayer(player); // TODO: Turn mario into pig?
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

        // destroy
        Destroy(gameObject, 1f);

        // disable collider
        GetComponent<Collider2D>().enabled = false;

        // disable movement
        realVelocity = Vector2.zero;
    }
    
}