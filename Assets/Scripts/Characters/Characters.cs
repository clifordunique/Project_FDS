using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Characters : MonoBehaviour {

    //TODO : Replace all the GetComponents into a single one for optimization


    #region Inspector Variables
    public float speed = 10;
    float skinWidth = .015f;
    #endregion

    #region Experimental
    //Sebastian Lague on Youtube, kudos to his tuto!
    [HideInInspector]
    public RaycastOrigins raycastOrigins;

    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    float horizontalRaySpacing;
    [HideInInspector]
    public float verticalRaySpacing;
    public LayerMask collisionMask;
    public CollisionInfo collisions;
    public float maxClimbAngle = 89f;
    float maxDescendAngle = 75f;

    public void UpdateRaycastOrigins()
    {
        Bounds bounds = thisCollider.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
        raycastOrigins.bottomRight = new Vector3(bounds.max.x, bounds.min.y, bounds.center.z);
        raycastOrigins.topLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
        raycastOrigins.topRight = new Vector3(bounds.max.x, bounds.max.y, bounds.center.z);

        Debug.DrawLine(raycastOrigins.bottomLeft, raycastOrigins.topRight, Color.blue);
        Debug.DrawLine(raycastOrigins.bottomRight, raycastOrigins.topLeft, Color.blue);
    }

    public void CalculateRaySpacing ()
    {
        Bounds bounds = thisCollider.bounds;
        bounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public struct RaycastOrigins
    {
        public Vector3 topLeft, topRight;
        public Vector3 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAnglePreviousTick;
        public Vector3 moveDirPreviousTick;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;

            slopeAnglePreviousTick = slopeAngle;
            slopeAngle = 0;
        }
    }
    #endregion

    #region Moves Vars
    public Vector3 _moveDirection;
        private bool jumping;
        [HideInInspector]
        public bool deactivateNormalGravity = false;
        bool climbingDropDownPlatform = false;

        [HideInInspector]
        public float MomentumOnJump;
        float previousTickHorizontalVelocity = 0f;
        float previousTickVerticalVelocity = 0f;
    [HideInInspector]
    public float MaxWallSlideSpeed = 0f;

        //Brakes Vars
        bool braking = false;
        bool flipped = false;
        float waitBeforeMove = 0f;
        List<float> velocityHistory = new List<float>();
    #endregion

    #region Internal Components
    [HideInInspector]
        public Collider thisCollider;
        [HideInInspector]
        public Rigidbody thisRigidbody;
        [HideInInspector]
        public CollisionTests collisionTests;
        [HideInInspector]
        public SpriteRenderer thisSprite;
        [HideInInspector]
        public Animator animator;
    #endregion

    #region external components
    [HideInInspector]
    public CharactersSharedVariables sharedVariables;
    public Collider justDroppedPlatform = null;
    #endregion

    #region Stairs & Slope Detection
    //Parameters
    public float stepMaxHeight = .5f;
        public float stepLengthDetection = .5f;
        [SerializeField]
        LayerMask stepCheckIgnoredLayers;

        List<Collider> ignoredColliders = new List<Collider>();

        float closeToStepPercent = 0f;
        float currentStepHeight = 0f;

        bool OnStep = false;

        //Slope
        [HideInInspector]
        public bool OnSlope = false;
        [HideInInspector]
        public Vector3 slopeDirection;
        /*[HideInInspector]
        public float SlopeAngle = 0f;*/
        [HideInInspector]
        public bool mirrorSlope = false;
    #endregion

    // Use this for initialization
    void Awake ()
    {
        sharedVariables = GameObject.Find ("CharactersManager").GetComponent<CharactersSharedVariables>();
        thisCollider = this.gameObject.GetComponent<Collider>();
        thisRigidbody = gameObject.GetComponent<Rigidbody>();
        collisionTests = gameObject.GetComponent<CollisionTests>();
        thisSprite = gameObject.GetComponentInChildren<SpriteRenderer>();
        animator = gameObject.GetComponentInChildren<Animator>();

        if (sharedVariables == null)
            Debug.LogError ("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");
	}

    public Vector3 GroundHitNormal = Vector3.zero;

    bool previousFrameOnSlope = false;

    void VerticalCollisions(ref Vector3 moveDirection)
    {
        float directionY = Mathf.Sign(moveDirection.y);
        float rayLength = Mathf.Abs(moveDirection.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector3 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector3.right * (verticalRaySpacing * i + moveDirection.x);
            RaycastHit hit;
            Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength, Color.red);

            if (Physics.Raycast(rayOrigin, Vector3.up * directionY, out hit, rayLength,  collisionMask))
            {
                //Debug.DrawLine(transform.position, hit.point, Color.green);
                moveDirection.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    moveDirection.x = moveDirection.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveDirection.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        if(collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(moveDirection.x);
            rayLength = Mathf.Abs(moveDirection.x) + skinWidth;
            Vector3 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector3.up * moveDirection.y;
            RaycastHit hit;

            if (Physics.Raycast(rayOrigin, Vector3.right * directionX, out hit, rayLength, collisionMask))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    moveDirection.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void HorizontalCollisions(ref Vector3 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        float rayLength = Mathf.Abs(moveDirection.x) + skinWidth;

        if(Mathf.Abs(moveDirection.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector3.up * (horizontalRaySpacing * i);
            RaycastHit hit;

            Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength, Color.red);

            if (Physics.Raycast(rayOrigin, Vector3.right * directionX, out hit, rayLength, collisionMask))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if(collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveDirection = collisions.moveDirPreviousTick;
                    }
                    float distanceToSlopeStart = 0f;
                    if (slopeAngle != collisions.slopeAnglePreviousTick)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveDirection.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveDirection, slopeAngle);
                    moveDirection.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    moveDirection.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        moveDirection.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveDirection.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    void ClimbSlope (ref Vector3 moveDirection, float a_slopeAngle)
    {
        float moveDistance = Mathf.Abs(moveDirection.x);
        float climbMoveDirectionY = Mathf.Sin(a_slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (moveDirection.y <= climbMoveDirectionY)
        {
            moveDirection.y = climbMoveDirectionY;
            moveDirection.x = Mathf.Cos(a_slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveDirection.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = a_slopeAngle;
        }
    }

    void DescendSlope (ref Vector3 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit hit;

        if (Physics.Raycast (rayOrigin, - Vector3.up, out hit, Mathf.Infinity, collisionMask))
        {
            float descendSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if(descendSlopeAngle != 0f && descendSlopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(descendSlopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveDirection.x))
                    {
                        float moveDistance = Mathf.Abs(moveDirection.x);
                        float descendDirectionY = Mathf.Sin(descendSlopeAngle * Mathf.Deg2Rad) * moveDistance;
                        moveDirection.x = Mathf.Cos(descendSlopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveDirection.x);
                        moveDirection.y -= descendDirectionY;

                        collisions.slopeAngle = descendSlopeAngle;
                        Debug.DrawRay(transform.position, moveDirection, Color.red);
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    //Main Move Method
    public void Move(Vector3 a_moveDirection, bool jump, bool dash)
    {
        #region Experimental
        UpdateRaycastOrigins();

        collisions.Reset();
        collisions.moveDirPreviousTick = a_moveDirection;

        if(a_moveDirection.y < 0)
        {
            DescendSlope(ref a_moveDirection);
        }

        if(a_moveDirection.x != 0)
            HorizontalCollisions(ref a_moveDirection);

        if (a_moveDirection.y != 0)
            VerticalCollisions(ref a_moveDirection);

        Debug.DrawRay(transform.position, -a_moveDirection, Color.green);

        if (a_moveDirection.x < 0)
            thisSprite.flipX = true;
        else if (a_moveDirection.x > 0)
            thisSprite.flipX = false;

        transform.Translate(a_moveDirection);
        #endregion

        #region OldStuff
        /*
        RaycastHit hit = SlopeDetection();

        if(!OnSlope && previousFrameOnSlope && !jump && !jumping) //Preventing Character from jumping off a slope when climbing it at full speed
        {//TODO : Not working when trying to jump for some reasons, and not working when going down a slope ever, fix it felix
            Debug.Log(transform.name + " exited slope.");
            moveDirection.y = -1;
            previousFrameOnSlope = false;
        }

        if (justDroppedPlatform != null && CheckIfGotPastDropDownPlatform())
        {
            Physics.IgnoreCollision(justDroppedPlatform, thisCollider, false);
            climbingDropDownPlatform = false;
            justDroppedPlatform = null;

            if (moveDirection.y > 0) //Cancelling upward move if we were climbing the platform
                moveDirection.y = 0;
        }



        if (CheckIfGrounded())
        {
            //Debug displayed when grounded
            //Debug.DrawRay(transform.position, -transform.up * thisCollider.bounds.size.y, Color.red);

            moveDirection = new Vector3(HorizontalDirection, VerticalDirection);
            moveDirection *= speed;

            if (jump)
            {
                OnSlope = false;
                moveDirection.y = jumpStrength;
                MomentumOnJump = thisRigidbody.velocity.x; //Using real speed instead of calculated one in case we are jumping from against a wall
                jumping = true;
            }
            else
                MomentumOnJump = 0;
        }
        else if (!OnSlope)
        {
            if (!climbingDropDownPlatform)
            {
                //Debug.Log(transform.name + " in Air");
                if (!deactivateNormalGravity)
                    ApplyGravity();

                if (TouchingHead())
                {
                    Collider touchedDropDownPlatform = CheckIfHeadTouchingDropDownPlatform();

                    if (touchedDropDownPlatform == null)
                        moveDirection.y = -sharedVariables.Gravity * Time.deltaTime;
                    else
                    {
                        justDroppedPlatform = touchedDropDownPlatform;
                        Physics.IgnoreCollision(touchedDropDownPlatform, thisCollider, true);
                        climbingDropDownPlatform = true;
                    }

                }

                AirControl(HorizontalDirection);
            }
            else
                moveDirection.y = speed;
        }

        //Jumping must be only true when the character is going UPWARD, it's flagged FALSE once the character starts going downward
        if (moveDirection.y <= 0 && jumping)
            jumping = false;

        //Adding some time for Pauline to do a U-turn more naturally
        Brakes();

        //Flipping sprite to face latest direction
        //TODO : Should be improved a bit during jump and U-turns
        if (previousTickHorizontalVelocity == 0f || !CheckIfGrounded())
        {
            if (moveDirection.x < 0)
                thisSprite.flipX = true;
            else if (moveDirection.x > 0)
                thisSprite.flipX = false;
        }

        if (OnSlope) //If the character is on a slope, then its direction should be deviated accordingly
        {
            Debug.Log(transform.name + " IS ON SLOOOOPE");

            slopeDirection = new Vector3(-hit.normal.y, hit.normal.x, 0f) / Mathf.Sqrt((hit.normal.x * hit.normal.x) + (hit.normal.y * hit.normal.y));

            if (HorizontalDirection < 0)
                moveDirection = slopeDirection * speed;
            else if (HorizontalDirection > 0)
                moveDirection = -slopeDirection * speed;

            SlopeAngle = Vector3.Angle(-transform.right, slopeDirection);

            if (hit.normal.x > 0)
                mirrorSlope = false;
            else
                mirrorSlope = true;

            previousFrameOnSlope = true;
        }

        //Applying Fall Drag if any
        if (moveDirection.y < 0 && FallDragMultiplier != 0)
            moveDirection.y /= FallDragMultiplier;



        thisRigidbody.velocity = moveDirection;

        //This is used for some calculations, for the natural U-turn for example...
        previousTickHorizontalVelocity = thisRigidbody.velocity.x;
        previousTickVerticalVelocity = thisRigidbody.velocity.y;
        */
        #endregion
    }

    public bool CheckIfGotPastDropDownPlatform()
    {
        if (thisCollider.bounds.max.y < justDroppedPlatform.bounds.min.y - .1f)
            return true;
        else if (thisCollider.bounds.min.y > justDroppedPlatform.bounds.max.y + .1f)
            return true;
        else
            return false;
    }

    //Move overrides
    public void Move (float HorizontalDirection)
    {
        Move(HorizontalDirection, 0, false);
    } 

    public void Move(float HorizontalDirection, bool jump)
    {
        Move(HorizontalDirection, 0, jump);
    }

    public void Move(Vector3 a_moveDirection)
    {
        Move(a_moveDirection, false, false);
    } 

    public void Move (float HorizontalDirection, float VerticalDirection, bool jump)
    {
        Move (new Vector3 (HorizontalDirection, VerticalDirection, 0f), jump, false);
    }

#region Other Moves
    void Brakes()
    {
        velocityHistory.Add(_moveDirection.x);
        if(Time.time > .1f)
        {
            //Debug.Log("Velocity from .1 secondes = " + velocityHistory[0]);
            velocityHistory.RemoveAt(0);
        }

        if (!braking)
        {
                if ((velocityHistory[0] > 0 && _moveDirection.x < 0)
                    || (velocityHistory[0] < 0 && _moveDirection.x > 0))
                {
                    if(collisions.below)
                        braking = true;
                }
        }
        else
        {
            //Debug.DrawRay(transform.position, transform.right * 5f, Color.blue);

            if (!flipped)
            {
                if (thisSprite.flipX)
                    thisSprite.flipX = false;
                else
                    thisSprite.flipX = true;

                flipped = true;
            }

            if (waitBeforeMove > .1f)
            {
                //Debug.Log("BRAKE");

                waitBeforeMove = 0f;
                braking = false;
                flipped = false;
            }
            else
            {
                _moveDirection.x = 0;
                waitBeforeMove += Time.deltaTime;
            }
        }
    }

    public void ApplyGravity ()
    {
        ApplyGravity(sharedVariables.Gravity);
    }



    public void ApplyGravity (float gravityOverride)
    {
        _moveDirection.y -= gravityOverride * Time.deltaTime;
    }

    public void CancelJump ()
    {
        jumping = false;
        _moveDirection.y = 0f;
    }
#endregion

#region Collision processing Methods

    void ClimbSmallStep(float stepHeight)
    {
        transform.position += Vector3.up * (Mathf.Abs(stepHeight) + .05f);
    }

    public Collider CheckIfGroundedInDropDownPlatform ()
    {
        RaycastHit hit;
        if (Physics.Raycast(thisCollider.bounds.center, -transform.up, out hit, Mathf.Infinity))
        {
            //Debug.DrawRay(thisCollider.bounds.center, -transform.up, Color.red);

            if (hit.collider.CompareTag("DropDownPlatform"))
                return hit.collider;
            else
                return null;
        }
        else
            return null;
    }

    public Collider CheckIfHeadTouchingDropDownPlatform()
    {
        RaycastHit hit;
        if (Physics.Raycast(thisCollider.bounds.center, transform.up, out hit, Mathf.Infinity))
        {
            //Debug.DrawRay(thisCollider.bounds.center, transform.up, Color.red);

            if (hit.collider.CompareTag("DropDownPlatform"))
                return hit.collider;
            else
                return null;
        }
        else
            return null;

    }

    public void CornerStuck()
    {
        if (collisionTests.MaxLeftSideCount >= 1)
            transform.position = transform.position + transform.right * .01f;
    }

    RaycastHit SlopeDetection()
    {
        RaycastHit hit = new RaycastHit();
        List<Ray> ray = new List<Ray>();
        ray.Add(new Ray(thisCollider.bounds.min, -transform.right));
        ray.Add(new Ray(new Vector3(thisCollider.bounds.max.x, thisCollider.bounds.min.y, transform.position.z), transform.right));

        float RayRange = .1f;

        foreach (Ray singleRay in ray)
        {
            //Debug.DrawRay(thisCollider.bounds.min, -transform.right, Color.cyan);
            if (Physics.Raycast(singleRay, out hit, RayRange))
            {
                Debug.DrawRay(singleRay.origin, singleRay.direction * RayRange, Color.black);

                float NormalAngle = Vector3.Angle(transform.right, hit.normal);
                if (Mathf.Abs(NormalAngle - 90) > .1f && Mathf.Abs(NormalAngle) > .1f && Mathf.Abs(NormalAngle - 180) > .1f)
                {
                    OnSlope = true;

                    /*Ray secondRay = new Ray();
                    switch (ray.IndexOf(singleRay))
                    {
                        case 0:
                            secondRay = ray[1];
                            break;
                        case 1:
                            secondRay = ray[0];
                            break;
                    }

                    Physics.Raycast(secondRay, out secondHit, .2f);*/

                    break;
                }
                else
                    OnSlope = false;
            }
            else
                OnSlope = false;
        }

        return hit;
    }
#endregion

#region Stairs and Step Detection Methods
    void VerticalCheckStep(RaycastHit horizontalHit, bool LeftCheck)
    {
        RaycastHit verticalHit; //This ray will be cast against the upper step
        RaycastHit secondVerticalHit; //While this one will be cast against the lower step or ground to calculate the step's height

        //Seems so! Now, we prepare another Raycast, whose origin is set up to be above the hit point of the previous cast
        //This is to make sure it's a small step we hit, and not a mid-air platform or a wall
        Vector3 verticalOrigin;

        if (LeftCheck)
            verticalOrigin = new Vector3(horizontalHit.point.x - .01f, thisCollider.bounds.max.y - .05f, transform.position.z);
        else
            verticalOrigin = new Vector3(horizontalHit.point.x + .01f, thisCollider.bounds.max.y - .05f, transform.position.z);

        if (Physics.Raycast(verticalOrigin, -transform.up, out verticalHit, thisCollider.bounds.size.y, stepCheckIgnoredLayers))
        {
            //Okay, we hit something, but it can still be the ground... We'll have to measure the distance between what we just hit and what's on the other
            //side of the step (or whatever we just hit). If the distance is greater than the step max height we set up, then... We don't consider this as a step, and just give up.

            if (LeftCheck)
                Physics.Raycast(verticalOrigin + transform.right * .1f, -transform.up, out secondVerticalHit, Mathf.Infinity, stepCheckIgnoredLayers);
            else
                Physics.Raycast(verticalOrigin - transform.right * .1f, -transform.up, out secondVerticalHit, Mathf.Infinity, stepCheckIgnoredLayers);

            float distanceBetweenStepAndGround = verticalHit.point.y - secondVerticalHit.point.y;

            //Debug.DrawLine(verticalOrigin, verticalOrigin - transform.up * thisCollider.bounds.size.y);

            //So if the distance we just measured is below the stepMaxHeight we set up...
            if (distanceBetweenStepAndGround <= stepMaxHeight)
            {
                //Then congratulations, it's a step Pauline can climb! \o/

                //The next two line are just used for debugging purpose...
                //Vector3 stepEdge = new Vector3(horizontalHit.point.x, verticalHit.point.y, transform.position.z);
                //Debug.DrawLine(transform.position, stepEdge, Color.red);

                //All right, now we have to calculate the percentage of proximity between Pauline and the step, so we can calculate how high she'll need to be.
                if (LeftCheck)
                    closeToStepPercent = Mathf.Abs(thisCollider.bounds.min.x - horizontalHit.point.x) / stepLengthDetection;
                else
                    closeToStepPercent = Mathf.Abs(thisCollider.bounds.max.x - horizontalHit.point.x) / stepLengthDetection;

                closeToStepPercent = Mathf.Abs(1 - closeToStepPercent);

                //Btw, we should deactivate any physic calculation between the stairs and Pauline, or else, it's going to be buggy as hell...
                Physics.IgnoreCollision(verticalHit.collider, thisCollider, true);
                ignoredColliders.Add(verticalHit.collider); //this list will come in handy later, when Pauline gets off or gets on top of the stairs to make
                                                            //sure the physic engine will get to work on those stairs again. 


                //Debug.DrawLine(transform.position, secondVerticalHit.point, Color.blue);

                //Make sure the height we calculated before is a positive number, it'll come handy later.
                currentStepHeight = Mathf.Abs(distanceBetweenStepAndGround);
                //Debug.Log("Step ahead. Proximity percent = " + closeToStepPercent + ". Step height = " + currentStepHeight);

                //Then, we use all those previous datas to calculate the new height for Pauline.
                //To summarize, the closer Pauline will be to the step, the higher she'll need to be, so that it's like she's walking up a ramp.
                float targetHeight = Mathf.Lerp(secondVerticalHit.point.y + thisCollider.bounds.size.y / 2, verticalHit.point.y + currentStepHeight + thisCollider.bounds.size.y / 2, closeToStepPercent);

                //Then let's apply all this directly to the transform component
                transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
                //Debug.DrawLine(transform.position, new Vector3(transform.position.x, targetHeight, transform.position.z), Color.red);

                //And flag the global bool OnStep to true, so that we can apply some exceptions to Pauline's moves
                //(Like overriding the fact she's grounded even if technically, her collider can't detect anything below her feet as we deactivated the
                //collisions between her collider and the stairs, remember?)
                OnStep = true;
            }
            else
                OnStep = false;
        }
        else
        {
            OnStep = false;
        }
    }

    public void CheckStep() //Checks if we're facing a step from a staircase that Pauline can climb easily
    {
        RaycastHit horizontalHit;

        //Setting up the first Raycast Origin : Roughly starts from Pauline's lowest collider boundary
        Vector3 horizontalLeftOrigin = new Vector3(thisCollider.bounds.min.x + .01f, thisCollider.bounds.min.y + .01f, transform.position.z);

        //There we go, let's check if there's something in front of Pauline's feet (left side)
        if (Physics.Raycast(horizontalLeftOrigin, -transform.right, out horizontalHit, stepLengthDetection, stepCheckIgnoredLayers))
        {
            VerticalCheckStep(horizontalHit, true);
        }
        else //If there's nothing on her left side, let's check on the right side
        {
            Vector3 horizontalRightOrigin = new Vector3(thisCollider.bounds.max.x - .01f, thisCollider.bounds.min.y + .01f, transform.position.z);

            if (Physics.Raycast(horizontalRightOrigin, transform.right, out horizontalHit, stepLengthDetection, stepCheckIgnoredLayers))
            {
                VerticalCheckStep(horizontalHit, false);
            }
            else
                OnStep = false;
        }

        if (!OnStep) //If we aren't on a step, or on top of the stairs, we should reactivate the collisions between them and Pauline.
        {
            foreach (Collider ignoredCollider in ignoredColliders)
            {
                Physics.IgnoreCollision(thisCollider, ignoredCollider, false);
            }

            ignoredColliders.Clear();
        }
        else
        {
            //Debug.Log(transform.name + " near step.");
        }
    }
    #endregion
}
