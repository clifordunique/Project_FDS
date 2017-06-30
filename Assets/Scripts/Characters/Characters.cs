using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Spine;
using Spine.Unity;

//Sebastian Lague on Youtube, kudos to his 2D CharController tuto!
public class Characters : MonoBehaviour {
    [Header("---!DEV MODE!---")]
    public bool ImmediateTestMode = false;

    #region Basic Moves Inspector Variables
    [Header ("Basic moves options")]
    public float speed = 10;
    public float jumpHeight = 4;
    public float timeToJumpApex = .4f;
    #endregion

    #region Character Collision System
    [Header ("Collision Detection Options")]
    public float skinWidth = .015f;

    [HideInInspector]
    public RaycastOrigins raycastOrigins;

    [SerializeField]
    int horizontalRayCount = 4;
    [SerializeField]
    int verticalRayCount = 4;

    float horizontalRaySpacing;
    float verticalRaySpacing;

    public LayerMask collisionMask;
    public CollisionInfo collisions;

    [Header ("Slope options")]
    [SerializeField]
    float maxClimbAngle = 89f;
    [SerializeField]
    float maxDescendAngle = 75f;
    #endregion

    #region Moves Vars
    [HideInInspector]
    public bool jumping;
    [HideInInspector]
    public float CurrentYSpeedMaxClamp = 0f;

    [HideInInspector]
    public float calculatedJumpForce;
    [HideInInspector]
    public float calculatedGravity;
    #endregion

    #region Internal Components
    [HideInInspector]
    public Collider thisCollider;
    [HideInInspector]
    public Rigidbody thisRigidbody;
    [HideInInspector]
    public SpriteRenderer thisSprite;
    [HideInInspector]
    public Animator animator;
    [HideInInspector]
    public Skeleton SpineSkeleton;
    #endregion

    #region external components
    [HideInInspector]
    public CharactersSharedVariables sharedVariables;

    #endregion

    #region Collisions Structs
    public struct RaycastOrigins
    {
        public Vector3 topLeft, topRight;
        public Vector3 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public bool getThroughAbove, getThroughBelow;
        public Collider justDroppedPlatform;

        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAnglePreviousTick;
        public Vector3 moveDirPreviousTick;

        public float highestContact;
        public int highestContactNumber;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            getThroughAbove = getThroughBelow = false;

            climbingSlope = false;
            descendingSlope = false;

            slopeAnglePreviousTick = slopeAngle;
            highestContact = 0;
            highestContactNumber = 0;
            slopeAngle = 0;
        }
    }
    #endregion

    // Use this for initialization
    void Awake ()
    {
        //External stuff retrieving
        sharedVariables = GameObject.Find ("CharactersManager").GetComponent<CharactersSharedVariables>();
        thisCollider = this.gameObject.GetComponent<Collider>();
        thisRigidbody = gameObject.GetComponent<Rigidbody>();
        thisSprite = gameObject.GetComponentInChildren<SpriteRenderer>();
        animator = gameObject.GetComponentInChildren<Animator>();
        SpineSkeleton = gameObject.GetComponentInChildren<SkeletonAnimator>().skeleton;

        if (sharedVariables == null)
            Debug.LogError ("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");

        CalculateGravityAndJump();
	}

    void Start ()
    {

    } 

    void CalculateGravityAndJump ()
    {
        calculatedGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        calculatedJumpForce = Mathf.Abs(calculatedGravity) * timeToJumpApex;
    }

    private void LateUpdate()
    {
    #if UNITY_EDITOR
        if (ImmediateTestMode)
            CalculateGravityAndJump();
    #endif
    }

    //Main Move Method, this will effectively translate the position of the character
    public void ApplyMoveAndCollisions(Vector3 a_moveDirection)
    {
        //Sprite flipping depending on direction
        //Used before collisions calculations to avoid the wall pushing changing Pauline's direction
        if (SpineSkeleton != null)
        {
            if (a_moveDirection.x < 0)
                SpineSkeleton.FlipX = true;
            else if (a_moveDirection.x > 0)
                SpineSkeleton.FlipX = false;
        }

        //Setting up raycasts and collisions infos for this frame, starting from a blank slate
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.moveDirPreviousTick = a_moveDirection;

        //Slope check if we're going down, the method will automatically apply the slope move if a slope is detected
        if(a_moveDirection.y < 0)
        {
            DescendSlope(ref a_moveDirection);
        }

        //Checking and applying Horizontal Collisions if the character is moving on the X axis
        if(a_moveDirection.x != 0)
            CheckAndApplyHorizontalCollisions(ref a_moveDirection);

        //Checking vertical collisions if the character is moving on the Y axis
        //It'll call the APPLY Vertical Collisions Method on its own after some other checks
        if (a_moveDirection.y != 0)
            CheckVerticalCollisions(ref a_moveDirection);

        //Not jumping anymore if we're going down
        if (a_moveDirection.y <= 0 && jumping)
        {
            jumping = false;
        }

        //Debug.Log(transform.name +  " moveDir before Translate = " + a_moveDirection);

        //THAT'S IT, LET'S MOOOOVE \o/ =D
        transform.Translate(a_moveDirection);
    }

    #region Collision System Methods
    void ApplyVerticalCollision(ref Vector3 moveDirection, ref float directionY, ref float rayLength, ref RaycastHit hit)
    {
        Debug.Log("VERTICAL IT FOR " + transform.name);
        moveDirection.y = (hit.distance - skinWidth) * directionY;
        rayLength = hit.distance;

        if (collisions.climbingSlope)
        {
            moveDirection.x = moveDirection.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveDirection.x);
        }

        collisions.below = directionY == -1;
        collisions.above = directionY == 1;
    }

    void CheckVerticalCollisions(ref Vector3 moveDirection)
    {
        float directionY = Mathf.Sign(moveDirection.y);
        float rayLength = Mathf.Abs(moveDirection.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector3 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector3.right * (verticalRaySpacing * i + moveDirection.x);
            RaycastHit hit;
            Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength, Color.red);

            if (Physics.Raycast(rayOrigin, Vector3.up * directionY, out hit, rayLength, collisionMask))
            {
                if (hit.collider.transform.CompareTag("GoThroughPlatform"))
                {
                    collisions.getThroughBelow = directionY == -1;
                    collisions.getThroughAbove = directionY == 1;

                    if (collisions.justDroppedPlatform == null)
                        collisions.justDroppedPlatform = hit.collider;
                    else
                    {
                        collisions.justDroppedPlatform.gameObject.layer = LayerMask.NameToLayer("Ground");
                        collisions.justDroppedPlatform = hit.collider;
                    }

                    if (!collisions.getThroughAbove && collisions.getThroughBelow)
                        ApplyVerticalCollision(ref moveDirection, ref directionY, ref rayLength, ref hit);
                }
                else
                {
                    ApplyVerticalCollision(ref moveDirection, ref directionY, ref rayLength, ref hit);
                }
            }
        }

        if (collisions.climbingSlope)
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

    void CheckAndApplyHorizontalCollisions(ref Vector3 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        float rayLength = Mathf.Abs(moveDirection.x) + skinWidth;

        if (Mathf.Abs(moveDirection.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector3.up * (horizontalRaySpacing * i);
            RaycastHit hit;

            Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength, Color.red);

            if (Physics.Raycast(rayOrigin, Vector3.right * directionX, out hit, rayLength, collisionMask) && !hit.transform.CompareTag("GoThroughPlatform"))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                if (hit.point.y > collisions.highestContact)
                    collisions.highestContact = hit.point.y;

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
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
    #endregion

    #region Sloped
    void ClimbSlope(ref Vector3 moveDirection, float a_slopeAngle)
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

    void DescendSlope(ref Vector3 moveDirection)
    {
        float directionX = Mathf.Sign(moveDirection.x);
        Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, -Vector3.up, out hit, Mathf.Infinity, collisionMask))
        {
            float descendSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (descendSlopeAngle != 0f && descendSlopeAngle <= maxDescendAngle)
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
    #endregion

    #region Other Moves


    void Brakes()
    {
        //TODO : Brakes should be easier to do with the new smoothed out moves
    }

    public bool CheckIfGotPastDropDownPlatform(ref Vector3 moveDirection)
    {
        if (thisCollider.bounds.max.y <= collisions.justDroppedPlatform.bounds.min.y - .01f && moveDirection.y < 0)
            return true;
        else if (thisCollider.bounds.min.y >= collisions.justDroppedPlatform.bounds.max.y + .01f && moveDirection.y > 0)
            return true;
        else
            return false;
    }
    #endregion

    #region Main Raycasts Methods
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

    public void CalculateRaySpacing()
    {
        Bounds bounds = thisCollider.bounds;
        bounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
    #endregion

}
