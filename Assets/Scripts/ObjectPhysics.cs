using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ObjectPhysics : MonoBehaviour
{

    [Header("Object Physics")]

    public GameObject spawnPrefab; // The prefab to spawn when the enemy is killed
    public bool spawnAtCustomPosition = false; // Set this to true if you want to use the custom spawn position
    public Vector3 customSpawnPosition; // The custom spawn position you can set in the Inspector

    public bool movingLeft = true;
    public Vector2 velocity = new Vector2(2, 0);

    public float gravity = 60f;

    public float width = 1;
    public float height = 1;

    // THIS IS DISTANCE AWAY FROM SIDES
    public float floorRaycastSpacing = 0.2f;
    public float wallRaycastSpacing = 0.2f;

    public LayerMask floorMask;
    public LayerMask wallMask;

    // should mostly be true, except for things like moving koopa shells
    public bool checkObjectCollision = true;

    public bool DontFallOffLedges = false;


    // this is possible, but i'd say to just put these in the objects that actually need them
    //[SerializeField] UnityEvent onFloorTouch;
    //[SerializeField] UnityEvent onWallTouch;

    public bool flipObject = true;
    private Vector2 normalScale;
    private float adjDeltaTime;

    public bool FrequentMovement = false; // if true, will update position every frame instead of every physics update
    private bool firstframe = true;
    public bool lavaKill = true;

    public LayerMask lavaMask;


    public enum ObjectState
    {
        falling,    // in the air
        grounded,   // on ground
        knockedAway, // upside down and falling off the screen
        onLava // on Lava
    }

    public AudioClip knockAwaySound;

    public enum ObjectMovement
    {
        still,      // not moving at all
        sliding,    // falling and sliding
        bouncing    // falling and bouncing

    }

    public ObjectState objectState = ObjectState.falling;
    public ObjectMovement movement = ObjectMovement.sliding;

    public bool stopAfterLand = false;  // do we set the horizontal velocity to 0 after landing?

    [Header("Bouncing")]
    public int maxBounces = 1; // -1 for infinite
    private int bounceCount = 0;
    public float bounceHeight;

    protected virtual void Start()
    {
        normalScale = transform.localScale;
        adjDeltaTime = Time.fixedDeltaTime;
        lavaMask = LayerMask.GetMask("Lava");
    }

    protected virtual void Update()
    {

        if (FrequentMovement)
        {
            adjDeltaTime = Time.deltaTime;

            if (adjDeltaTime > 0.1f)
            {
                adjDeltaTime = 0f;  // lag spike fix
                //print("lagging!");
            }

            if ((!(movement == ObjectMovement.still) || objectState == ObjectState.knockedAway) && !firstframe)
                UpdatePosition();
        }
        firstframe = false;

    }

    protected virtual void FixedUpdate()
    {
        if (!FrequentMovement)
        {
            if (!(movement == ObjectMovement.still) || objectState == ObjectState.knockedAway)
                UpdatePosition();
        }
    }

    public void UpdatePosition()
    {

        Vector3 pos = transform.position;
        Vector3 scale = transform.localScale;

        // check walls first
        if (objectState != ObjectState.knockedAway)
        {
            if (velocity.x != 0)
            {
                CheckWalls(pos, movingLeft ? -1 : 1);
            }
        }

        // vertical movement
        if (objectState == ObjectState.falling || objectState == ObjectState.knockedAway)
        {

            pos.y += velocity.y * adjDeltaTime;

            velocity.y -= gravity * adjDeltaTime;
        }

        // horizontal movement
        if (movingLeft)
        {

            pos.x -= velocity.x * adjDeltaTime;

            if (flipObject)
                scale.x = normalScale.x;

        }
        else
        {

            pos.x += velocity.x * adjDeltaTime;

            if (flipObject)
                scale.x = -normalScale.x;
        }


        // fix bug where object has y velocity but walking
        // making it walk in the air
        if (objectState == ObjectState.grounded)
        {
            velocity.y = 0;
        }

        if (objectState != ObjectState.knockedAway)
        {

            if (velocity.y <= 0)
            {
                pos = CheckGround(pos);
            }

            // Check for lava collision
            if (lavaKill && objectState != ObjectState.onLava)
            {
                CheckLava(pos);
            }

            if (DontFallOffLedges && objectState == ObjectState.grounded)
            {
                CheckLedges(pos);
            }
        }

        transform.position = pos;
        transform.localScale = scale;
    }

    private void OnDestroy()
    {
        // Spawn the object when the enemy is destroyed and in the ObjectState.knockedAway or ObjectState.onLava state
        if (spawnPrefab != null && (objectState == ObjectState.knockedAway || objectState == ObjectState.onLava))
        {
            Vector3 spawnPosition;

            // If using customSpawnPosition, spawn the object at the custom position
            if (spawnAtCustomPosition)
            {
                spawnPosition = customSpawnPosition;
            }
            else
            {
                // Otherwise, spawn it at the enemy's position
                spawnPosition = transform.position;
            }

            Instantiate(spawnPrefab, spawnPosition, Quaternion.identity);
        }
    }

    Vector3 CheckGround(Vector3 pos)
    {

        float halfHeight = height / 2;
        float halfWidth = width / 2;

        Vector2 originLeft = new Vector2(pos.x - halfWidth + floorRaycastSpacing, pos.y - halfHeight + 0.02f);
        Vector2 originMiddle = new Vector2(pos.x, pos.y - halfHeight + 0.02f);
        Vector2 originRight = new Vector2(pos.x + halfWidth - floorRaycastSpacing, pos.y - halfHeight + 0.02f);
        //print("adjDeltaTime is:" + (adjDeltaTime));
        //print("Velocity is:" + velocity);
        RaycastHit2D[] groundLeft = Physics2D.RaycastAll(originLeft, Vector2.down, -velocity.y * adjDeltaTime + .04f, floorMask);
        RaycastHit2D[] groundMiddle = Physics2D.RaycastAll(originMiddle, Vector2.down, -velocity.y * adjDeltaTime + .04f, floorMask);
        RaycastHit2D[] groundRight = Physics2D.RaycastAll(originRight, Vector2.down, -velocity.y * adjDeltaTime + .04f, floorMask);


        RaycastHit2D[][] groundCollides = { groundLeft, groundMiddle, groundRight };


        // get shortest distance
        float shortestDistance = float.MaxValue;
        RaycastHit2D shortestRay = new RaycastHit2D();
        Collider2D thisCollider = GetComponent<Collider2D>();

        foreach (RaycastHit2D[] groundCols in groundCollides)
        {
            foreach (RaycastHit2D hitRay in groundCols)
            {
                if (hitRay.collider != thisCollider)
                {
                    if (hitRay.collider.gameObject.GetComponent<ObjectPhysics>())
                    {
                        if (!checkifObjectCollideValid(hitRay.collider.gameObject.GetComponent<ObjectPhysics>()))
                        {
                            continue;
                        }
                    }
                    if (hitRay.distance < shortestDistance)
                    {
                        shortestDistance = hitRay.distance;
                        shortestRay = hitRay;
                    }
                }
            }
        }

        if (shortestRay)
        {
            // We hit the ground

            pos.y = shortestRay.point.y + halfHeight;
            velocity.y = 0;

            if (movement == ObjectMovement.sliding)
            {
                Land();
            }
            else if (movement == ObjectMovement.bouncing)
            {
                if (maxBounces == -1)
                {
                    // infinite bounces
                    bounceCount = -5;   // just needs to be less than maxBounces
                }
                if (bounceCount < maxBounces)
                {
                    bounceCount++;
                    velocity.y = bounceHeight;
                }
                else
                {
                    // we're done bouncing
                    Land();
                }
                velocity.y = bounceHeight;
            }

        }
        else
        {
            // We didn't hit the ground

            if (objectState != ObjectState.falling)
            {
                // We were grounded, but now we're not

                Fall();

            }
        }
        return pos;
    }

    RaycastHit2D RaycastWalls(Vector3 pos, float direction)
    {
        // use raycast all and don't count itself
        float halfHeight = height / 2;
        float halfWidth = width / 2;

        Vector2 originTop = new Vector2(pos.x + direction * halfWidth, pos.y + halfHeight - wallRaycastSpacing);
        Vector2 originMiddle = new Vector2(pos.x + direction * halfWidth, pos.y);
        Vector2 originBottom = new Vector2(pos.x + direction * halfWidth, pos.y - halfHeight + wallRaycastSpacing);

        RaycastHit2D[] wallTop = Physics2D.RaycastAll(originTop, new Vector2(direction, 0), velocity.x * adjDeltaTime, wallMask);
        RaycastHit2D[] wallMiddle = Physics2D.RaycastAll(originMiddle, new Vector2(direction, 0), velocity.x * adjDeltaTime, wallMask);
        RaycastHit2D[] wallBottom = Physics2D.RaycastAll(originBottom, new Vector2(direction, 0), velocity.x * adjDeltaTime, wallMask);

        RaycastHit2D[][] wallCollides = { wallTop, wallMiddle, wallBottom };


        // get shortest distance
        float shortestDistance = float.MaxValue;
        RaycastHit2D shortestRay = new RaycastHit2D();
        Collider2D thisCollider = GetComponent<Collider2D>();

        foreach (RaycastHit2D[] wallCols in wallCollides)
        {
            foreach (RaycastHit2D hitRay in wallCols)
            {
                if (hitRay.collider != thisCollider)
                {
                    if (hitRay.collider.gameObject.GetComponent<ObjectPhysics>())
                    {
                        if (!checkifObjectCollideValid(hitRay.collider.gameObject.GetComponent<ObjectPhysics>()))
                        {
                            continue;
                        }
                    }
                    if (hitRay.distance < shortestDistance)
                    {
                        shortestDistance = hitRay.distance;
                        shortestRay = hitRay;
                    }
                }
            }
        }

        return shortestRay;
    }

    private bool checkifObjectCollideValid(ObjectPhysics other)
    {
        if (!checkObjectCollision)
        {
            //print("hit object physics while I am turned off");
            return false;
        }

        if (!other.checkObjectCollision)
        {
            //print("hit object physics while the other one is turned off");
            return false;
        }

        return true;
    }

    void CheckLava(Vector3 pos)
    {
        float halfHeight = height / 2;

        // Raycast downward to check for the "Lava" layer
        Vector2 originMiddle = new Vector2(pos.x, pos.y - halfHeight + 0.02f);
        RaycastHit2D[] lavaHits = Physics2D.RaycastAll(originMiddle, Vector2.down, -velocity.y * adjDeltaTime + 0.04f, lavaMask);

        if (lavaHits.Length > 0)
        {
            // We hit the "Lava" layer
            objectState = ObjectState.onLava;
            // Implement any specific behavior for the object on lava (e.g., burn or take damage)
            Destroy(gameObject);
        }
        else
        {
            // We are not on the lava layer, reset the objectState to falling
            objectState = ObjectState.falling;
        }
    }

    public virtual bool CheckWalls(Vector3 pos, float direction)
    {

        //RaycastHit2D hitRay = RaycastWalls (pos, direction);
        RaycastHit2D hitRay = RaycastWalls(pos, direction);

        if (hitRay.collider == null)
        {
            return false;
        }

        // check if the gameobject we hit is also an objectphysics
        if (hitRay.collider.gameObject.GetComponent<ObjectPhysics>() != null)
        {
            ObjectPhysics other = hitRay.collider.gameObject.GetComponent<ObjectPhysics>();

            //probably this checking should come in the raycast walls function
            if (other.objectState == ObjectState.knockedAway)
            {
                // They are knocked away, so we can walk through them
                return false;
            }

            // TODO: Implement proper object collision
        }

        // hit something
        onTouchWall(hitRay.collider.gameObject);
        // flip direction
        movingLeft = !movingLeft;
        return true;

    }

    protected virtual void onTouchWall(GameObject other)
    {
        //print("touching wall");
        // override me for custom behavior
    }

    void CheckLedges(Vector3 pos)
    {
        float halfHeight = height / 2;
        float halfWidth = width / 2;

        Vector2 origin;

        if (movingLeft)
            origin = new Vector2(pos.x - halfWidth, pos.y - halfHeight);
        else
            origin = new Vector2(pos.x + halfWidth, pos.y - halfHeight);

        RaycastHit2D ground = Physics2D.Raycast(origin, Vector2.down, 0.5f, floorMask);

        if (!ground)
        {
            movingLeft = !movingLeft;
        }

    }

    void Fall()
    {

        velocity.y = 0;

        objectState = ObjectState.falling;

        bounceCount = 0; // reset bounce count

    }

    void Land()
    {
        objectState = ObjectState.grounded;
        if (stopAfterLand)
        {
            velocity.x = 0;
        }
    }

    public void KnockAway(bool direction)
    {
        if (objectState != ObjectState.knockedAway)
        {
            GetComponent<SpriteRenderer>().flipY = true;
            objectState = ObjectState.knockedAway;
            velocity.y = 5;
            velocity.x = 5;
            movingLeft = direction;
            GetComponent<Collider2D>().enabled = false;
            transform.rotation = Quaternion.identity;
            // play sound
            if (knockAwaySound != null && GetComponent<AudioSource>() != null)
            {
                GetComponent<AudioSource>().PlayOneShot(knockAwaySound);
            }
        }
    }

    private void OnBecameInvisible()
    {
        // once the knocked away object is off screen, destroy it
        if (objectState == ObjectState.knockedAway)
        {
            Destroy(gameObject);
        }
    }

    // for use with signals
    public void SetXVelocity(float x)
    {
        velocity.x = x;
    }
    public void SetYVelocity(float y)
    {
        velocity.y = y;
    }

    private void OnDrawGizmos()
    {

        if (movement == ObjectMovement.still)
        {
            return;
        }

        // Floor Raycasts
        Vector2 pos = transform.position;
        float halfHeight = height / 2;
        float halfWidth = width / 2;

        Vector2 originLeft = new Vector2(pos.x - halfWidth + floorRaycastSpacing, pos.y - halfHeight + 0.02f);
        Vector2 originMiddle = new Vector2(pos.x, pos.y - halfHeight + 0.02f);
        Vector2 originRight = new Vector2(pos.x + halfWidth - floorRaycastSpacing, pos.y - halfHeight + 0.02f);

        Gizmos.color = Color.red;

        float distance = Application.isPlaying ? -velocity.y * adjDeltaTime + .04f : 0.2f;

        Gizmos.DrawLine(originLeft, originLeft + new Vector2(0, -distance));
        Gizmos.DrawLine(originMiddle, originMiddle + new Vector2(0, -distance));
        Gizmos.DrawLine(originRight, originRight + new Vector2(0, -distance));

        // spawn position
        Gizmos.DrawSphere(transform.position, 0.1f);

        // Wall Raycasts
        float direction = movingLeft ? -1 : 1;

        Vector2 originTop = new Vector2(pos.x + direction * halfWidth, pos.y + halfHeight - wallRaycastSpacing);
        Vector2 originMiddleSide = new Vector2(pos.x + direction * halfWidth, pos.y);
        Vector2 originBottom = new Vector2(pos.x + direction * halfWidth, pos.y - halfHeight + wallRaycastSpacing);

        distance = Application.isPlaying ? velocity.x * adjDeltaTime : velocity.x * 0.02f;

        Gizmos.DrawLine(originTop, originTop + new Vector2(distance * direction, 0));
        Gizmos.DrawLine(originMiddleSide, originMiddleSide + new Vector2(distance * direction, 0));
        Gizmos.DrawLine(originBottom, originBottom + new Vector2(distance * direction, 0));

    }
}

