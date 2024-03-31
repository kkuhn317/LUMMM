using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;

// This script is for all objects that need to move around like enemies, items, etc.
// It is a custom simplified physics engine that handles collisions and movement
// Objects move at a constant velocity horizontally and bounce off walls
// These objects can also be carried by mario
public class ObjectPhysics : MonoBehaviour
{
    [Header("Object Physics")]

    public bool movingLeft = true;

    /// <summary>
    /// <para>Does NOT take direction into account.</para>
    /// <para>Even if moving left, the x velocity should always be positive</para>
    /// </summary>
    public Vector2 velocity = new Vector2(2, 0);

    /// <summary>
    /// <para>the velocity with the direction taken into account.</para>
    /// <para>if the object is moving left, this will be negative</para>
    /// </summary>
    public Vector2 realVelocity {
        get {
            return new Vector2(velocity.x * (movingLeft ? -1 : 1), velocity.y);
        }
        set {
            velocity = new Vector2(Mathf.Abs(value.x), value.y);
            movingLeft = value.x < 0 ? true : false;
        }
    }

    public float gravity = 60f;

    public float width = 1;
    public float height = 1;

    // THIS IS DISTANCE AWAY FROM SIDES
    public float floorRaycastSpacing = 0.2f;
    public float wallRaycastSpacing = 0.2f;

    public LayerMask floorMask;
    public LayerMask wallMask;

    private float floorAngle = 0f;  // -45 = \, 0 = _, 45 = /

    // should mostly be true, except for things like moving koopa shells
    public bool checkObjectCollision = true;
    public bool ceilingDetection = true;
    public bool DontFallOffLedges = false;
    public bool bounceOffWalls = true;  // if false, will stop moving when it hits a wall
    private bool onMovingPlatform = false;
    private Transform ogParent;    // for stacks of enemies

    public bool flipObject = true;  // if true, the object will flip when moving right
    private Vector2 normalScale;
    protected float adjDeltaTime;

    public bool FrequentMovement = false; // if true, will update position every frame instead of every physics update
    private bool firstframe = true;
    public bool lavaKill = true;
    public float sinkSpeed = 20f;
    public LayerMask lavaMask;
    private GameObject touchedLava;

    [Header("Carrying")]

    public bool carryable = false;
    public bool carried = false;
    private int oldOrderInLayer;
    public Vector2 throwVelocity = new Vector2(12, 10);
    private bool hasBeenThrown = false;

    [Header("Knock Away")]

    public Vector2 knockAwayVelocity = new Vector2(5, 5);
    public bool overrideKnockAwayGravity = false;
    public float knockAwayGravity = 60f;
    public KnockAwayType knockAwayType = KnockAwayType.flip;

    public float knockAwayRotationSpeed = 10f;  // Used for rotating knock away
    public float KnockAwayDissapearTime = -1f;  // < 0 means don't dissapear

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
        bouncing,    // falling and bouncing
        special,    // special movement (like a bullet bill)
    }

    public enum KnockAwayType
    {
        flip,   // Immediately flip upside down
        rotate // Constantly Rotate backwards
    }

    public ObjectState objectState = ObjectState.falling;
    public ObjectMovement movement = ObjectMovement.sliding;

    public bool stopAfterLand = false;  // do we set the horizontal velocity to 0 after landing?

    [Header("Bouncing")]

    public float bounceHeight;
    public float minHeightToBounce = 1f;

    private float peakHeight;

    public bool testme = false;

    protected virtual void Start()
    {
        normalScale = transform.localScale;
        adjDeltaTime = Time.fixedDeltaTime;
        lavaMask = LayerMask.GetMask("Lava");
        peakHeight = transform.position.y;
        ogParent = transform.parent;

        // temporary fix for objects that don't have the sprite renderer in the parent
        // TODO: change this so all sprite renderers are affected by carrying
        if (GetComponent<SpriteRenderer>() != null) {
            oldOrderInLayer = GetComponent<SpriteRenderer>().sortingOrder;
        } else {
            oldOrderInLayer = 69;
        }
        //oldOrderInLayer = GetComponent<SpriteRenderer>().sortingOrder;
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

        if (firstframe)
            firstframe = false;

        // Knock Away Logic
        if (objectState == ObjectState.knockedAway)
        {
            // rotate knock away
            if (knockAwayType == KnockAwayType.rotate) {
                // rotate
                transform.Rotate(0, 0, knockAwayRotationSpeed * Time.deltaTime * (movingLeft ? 1 : -1));
            }

            // Fade out
            if (KnockAwayDissapearTime > 0) {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                Color color = spriteRenderer.color;
                color.a -= Time.deltaTime / KnockAwayDissapearTime;
                spriteRenderer.color = color;
                if (color.a <= 0) {
                    Destroy(gameObject);
                }
            }
            
        }


    }

    protected virtual void FixedUpdate()
    {
        if (!FrequentMovement)
        {
            if (!(movement == ObjectMovement.still) || objectState == ObjectState.knockedAway)
                UpdatePosition();
        }
    }

    public virtual void UpdatePosition()
    {
        // don't move if carried
        if (carried)
        {
            return;
        }

        Vector3 pos = transform.position;

        // check walls first
        if (objectState != ObjectState.knockedAway)
        {
            if (velocity.x != 0)
            {
                CheckWalls(pos, movingLeft ? -1 : 1);
            }
        }

        // vertical movement
        if (objectState == ObjectState.falling || objectState == ObjectState.knockedAway || objectState == ObjectState.onLava)
        {
            pos = VerticalMovement(pos);
        }

        // horizontal movement
        pos = HorizontalMovement(pos);
        
        // fix bug where object has y velocity but walking
        // making it walk in the air
        if (objectState == ObjectState.grounded)
        {
            velocity.y = 0;
        }

        onMovingPlatform = false;   // reset moving platform flag
        if (objectState != ObjectState.knockedAway && objectState != ObjectState.onLava)
        {

            if (velocity.y <= 0)
            {
                pos = CheckGround(pos);
            }
            if (velocity.y > 0 && ceilingDetection)
            {
                pos = CheckCeiling(pos);
            }

            // Check for lava collision
            if (lavaKill)
            {
                CheckLava(pos);
            }

            if (DontFallOffLedges && objectState == ObjectState.grounded)
            {
                CheckLedges(pos);
            }
        }

        Vector3 scale = transform.localScale;

        // flipping object sprite
        if (flipObject)
        {
            scale.x = movingLeft ? normalScale.x : -normalScale.x;
        }

        transform.position = pos;
        transform.localScale = scale;

        Physics.SyncTransforms();

        // are we deep enough in lava to die?
        if (objectState == ObjectState.onLava) {
            if (touchedLava.transform.position.y > transform.position.y + (height / 2)) {
                Destroy(gameObject);
            }
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
        Collider2D[] thisColliders = GetComponents<Collider2D>();

        foreach (RaycastHit2D[] groundCols in groundCollides)
        {
            foreach (RaycastHit2D hitRay in groundCols)
            {
                if (Array.IndexOf(thisColliders, hitRay.collider) != -1)
                {
                    continue;
                }
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

        if (shortestRay)
        {
            // We hit the ground

            pos.y = shortestRay.point.y + halfHeight;
            velocity.y = 0;

            GameObject groundObject = shortestRay.transform.gameObject;

            if (groundObject.CompareTag("Slope") && groundObject.TryGetComponent(out Slope slope))
            {
                // Slope
                floorAngle = slope.angle;
            }
            else
            {
                floorAngle = 0;
            }

            if (movement == ObjectMovement.sliding)
            {
                Land();
            }
            else if (movement == ObjectMovement.bouncing)
            {
                if (peakHeight - pos.y >= minHeightToBounce)
                {
                    // bounce
                    velocity.y = bounceHeight;
                    OnBounced();
                }
                else
                {
                    // we're done bouncing
                    Land();
                }
            }

            peakHeight = pos.y;

            // moving platform
            if (groundObject.CompareTag("MovingPlatform"))
            {
                onMovingPlatform = true;
                transform.parent = groundObject.transform;
            }
            else
            {
                onMovingPlatform = false;
                if (ogParent != null) {
                    transform.parent = ogParent;
                } else {
                    transform.parent = null;
                }
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

    protected virtual void OnBounced()
    {
        // override me for custom behavior after bouncing
    }

    Vector3 CheckCeiling(Vector3 pos)
    {
        // combine floor and wall masks
        int ceilingMask = floorMask | wallMask;

        float halfHeight = height / 2;
        float halfWidth = width / 2;

        Vector2 originLeft = new Vector2(pos.x - halfWidth + floorRaycastSpacing, pos.y + halfHeight - 0.02f);
        Vector2 originMiddle = new Vector2(pos.x, pos.y + halfHeight - 0.02f);
        Vector2 originRight = new Vector2(pos.x + halfWidth - floorRaycastSpacing, pos.y + halfHeight - 0.02f);

        RaycastHit2D[] ceilingLeft = Physics2D.RaycastAll(originLeft, Vector2.up, velocity.y * adjDeltaTime, ceilingMask);
        RaycastHit2D[] ceilingMiddle = Physics2D.RaycastAll(originMiddle, Vector2.up, velocity.y * adjDeltaTime, ceilingMask);
        RaycastHit2D[] ceilingRight = Physics2D.RaycastAll(originRight, Vector2.up, velocity.y * adjDeltaTime, ceilingMask);

        RaycastHit2D[][] ceilingCollides = { ceilingLeft, ceilingMiddle, ceilingRight };

        // get shortest distance
        float shortestDistance = float.MaxValue;
        RaycastHit2D shortestRay = new RaycastHit2D();
        Collider2D[] thisColliders = GetComponents<Collider2D>();

        foreach (RaycastHit2D[] ceilingCols in ceilingCollides)
        {
            foreach (RaycastHit2D hitRay in ceilingCols)
            {
                if (Array.IndexOf(thisColliders, hitRay.collider) != -1)
                {
                    continue;
                }
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

        if (shortestRay)
        {
            // We hit the ceiling

            pos.y = shortestRay.point.y - halfHeight;
            velocity.y = -velocity.y * 0.5f;    // bounce off the ceiling a little bit

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
        Collider2D[] thisColliders = GetComponents<Collider2D>();

        foreach (RaycastHit2D[] wallCols in wallCollides)
        {
            foreach (RaycastHit2D hitRay in wallCols)
            {
                if (Array.IndexOf(thisColliders, hitRay.collider) != -1)
                {
                    continue;
                }
                if (hitRay.collider.gameObject.CompareTag("Slope")) {
                    continue;
                }
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

        return shortestRay;
    }

    protected virtual Vector3 HorizontalMovement(Vector3 pos) {
        // move along the slope if we're on one
        Vector2 slopeVector = new Vector2(1, 0);
        if (objectState == ObjectState.grounded && floorAngle != 0)
        {
            slopeVector = new Vector2(Mathf.Cos(floorAngle * Mathf.Deg2Rad), Mathf.Sin(floorAngle * Mathf.Deg2Rad));
        }
        pos += (movingLeft ? -1 : 1) * adjDeltaTime * velocity.x * (Vector3)slopeVector;

        return pos;
    }

    protected virtual Vector3 VerticalMovement(Vector3 pos) {
         pos.y += velocity.y * adjDeltaTime;

            if (pos.y > peakHeight)
            {
                peakHeight = pos.y;
            }
            
            if (objectState == ObjectState.onLava) {
                // sinking in lava
                velocity.y = -sinkSpeed * adjDeltaTime;
            } else {
                // regular falling
                velocity.y -= gravity * adjDeltaTime;
            }
        return pos;
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
            touchedLava = lavaHits[0].collider.gameObject;
            GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
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
        
        return true;
    }

    protected virtual void onTouchWall(GameObject other)
    {
        if (bounceOffWalls) {
            // flip direction
            movingLeft = !movingLeft;
        } else {
            velocity.x = 0;
        }

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

    public void Fall()
    {

        velocity.y = 0;

        objectState = ObjectState.falling;

        onMovingPlatform = false;
        
        if (ogParent != null) {
            transform.parent = ogParent;
        } else {
            transform.parent = null;
        }

    }

    public virtual void Land()
    {
        objectState = ObjectState.grounded;
        if (stopAfterLand)
        {
            velocity.x = 0;
        }
    }

    public virtual void KnockAway(bool direction, bool sound = true, KnockAwayType? overrideType = null, Vector2? overrideVelocity = null) {
        if (objectState != ObjectState.knockedAway) {
            objectState = ObjectState.knockedAway;

            // Physics
            velocity = overrideVelocity ?? knockAwayVelocity;
            if (overrideKnockAwayGravity) {
                gravity = knockAwayGravity;
            }

            // Play sound
            if (sound && knockAwaySound != null && GetComponent<AudioSource>() != null)
            {
                GetComponent<AudioSource>().PlayOneShot(knockAwaySound);
            }

            movingLeft = direction;

            if (overrideType != null) {
                knockAwayType = overrideType.Value;
            }
            
            switch (knockAwayType) {
                case KnockAwayType.flip:
                    GetComponent<SpriteRenderer>().flipY = true;
                    break;
                case KnockAwayType.rotate:
                    // Handled in update
                    break;
            }

            GetComponent<Collider2D>().enabled = false;
            transform.rotation = Quaternion.identity;

            
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

    protected virtual void OnDrawGizmosSelected()
    {

        if (movement == ObjectMovement.still)
        {
            return;
        }

        float distance;
        Gizmos.color = Color.red;
        
        Vector2 pos = transform.position;
        float halfHeight = height / 2;
        float halfWidth = width / 2;

        // Floor Raycasts
        if (!Application.isPlaying || velocity.y <= 0) {
            Vector2 originLeft = new Vector2(pos.x - halfWidth + floorRaycastSpacing, pos.y - halfHeight + 0.02f);
            Vector2 originMiddle = new Vector2(pos.x, pos.y - halfHeight + 0.02f);
            Vector2 originRight = new Vector2(pos.x + halfWidth - floorRaycastSpacing, pos.y - halfHeight + 0.02f);

            distance = Application.isPlaying ? -velocity.y * adjDeltaTime + .04f : 0.2f;

            Gizmos.DrawLine(originLeft, originLeft + new Vector2(0, -distance));
            Gizmos.DrawLine(originMiddle, originMiddle + new Vector2(0, -distance));
            Gizmos.DrawLine(originRight, originRight + new Vector2(0, -distance));
        }

        // Wall Raycasts
        float direction = movingLeft ? -1 : 1;

        Vector2 originTop = new Vector2(pos.x + direction * halfWidth, pos.y + halfHeight - wallRaycastSpacing);
        Vector2 originMiddleSide = new Vector2(pos.x + direction * halfWidth, pos.y);
        Vector2 originBottom = new Vector2(pos.x + direction * halfWidth, pos.y - halfHeight + wallRaycastSpacing);

        distance = Application.isPlaying ? velocity.x * adjDeltaTime : velocity.x * 0.02f;

        Gizmos.DrawLine(originTop, originTop + new Vector2(distance * direction, 0));
        Gizmos.DrawLine(originMiddleSide, originMiddleSide + new Vector2(distance * direction, 0));
        Gizmos.DrawLine(originBottom, originBottom + new Vector2(distance * direction, 0));

        // Ceiling Raycasts
        if (ceilingDetection && (!Application.isPlaying || velocity.y > 0))
        {
            Vector2 originLeftCeiling = new Vector2(pos.x - halfWidth + floorRaycastSpacing, pos.y + halfHeight - 0.02f);
            Vector2 originMiddleCeiling = new Vector2(pos.x, pos.y + halfHeight - 0.02f);
            Vector2 originRightCeiling = new Vector2(pos.x + halfWidth - floorRaycastSpacing, pos.y + halfHeight - 0.02f);

            distance = Application.isPlaying ? velocity.y * adjDeltaTime + .04f : 0.2f;

            Gizmos.DrawLine(originLeftCeiling, originLeftCeiling + new Vector2(0, distance));
            Gizmos.DrawLine(originMiddleCeiling, originMiddleCeiling + new Vector2(0, distance));
            Gizmos.DrawLine(originRightCeiling, originRightCeiling + new Vector2(0, distance));
        }
    }

    // call this after mario picks up the object
    public virtual void getCarried()
    {
        carried = true;
        transform.localPosition = new Vector3(0, 0, 0);
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().sortingOrder = 100;
    }

    public virtual void getDropped(bool direction)
    {
        carried = false;

        velocity = new Vector2(0, 0);
        movingLeft = direction;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<SpriteRenderer>().sortingOrder = oldOrderInLayer;
        objectState = ObjectState.falling;
    }

    public virtual void GetThrown(bool direction)
    {
        carried = false;

        velocity = throwVelocity;
        movingLeft = !direction;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<SpriteRenderer>().sortingOrder = oldOrderInLayer;
        objectState = ObjectState.falling;

        // Set a flag to indicate that the enemy has been thrown
        hasBeenThrown = true;
    }
    
   private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the enemy has been thrown before handling the collision
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (hasBeenThrown && objectState == ObjectState.falling)
            {
                EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.KnockAway(transform.position.x > enemy.transform.position.x);
                }
                GameManager.Instance.AddScorePoints(100);
            }
        }  

        // TODO: This was code for throwing a POW block. Move it to the POW block script probably
        //if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Default"))
        //{
            // if (hasBeenThrown && objectState == ObjectState.falling)
            // {
            //     POWBlock pow = collision.gameObject.GetComponent<POWBlock>();
            //     if (pow != null)
            //     {
            //         pow.ActivatePOWBlock();
            //     }
            // }
        //}    
    }

    public virtual void escapeMario()
    {
        // find mario's script (mario is 2 levels up hopefully lol)
        MarioMovement marioScript = transform.parent.parent.gameObject.GetComponent<MarioMovement>();
        if (marioScript != null)
        {
            marioScript.dropCarry();
        }
        
    }

    public virtual void Flip()
    {
        movingLeft = !movingLeft;
    }

    public virtual void SetDirection(bool direction)
    {
        movingLeft = direction;
    }
}

