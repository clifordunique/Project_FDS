using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Characters : MonoBehaviour {

    //TODO : Replace all the GetComponents into a single one for optimization


    #region Inspector Variables
    public float speed = 10;
    [SerializeField]
    float jumpMomentumInfluenceRate;
    [SerializeField]
    float jumpStrength = 12;
    [SerializeField]
    float jumpSpeed = 50;
    [SerializeField]
    float groundTreshold = .1f;
    [SerializeField]
    float minimumWallSize = .1f;
    [SerializeField]
    float UTurnTiming = 0.5f;
    #endregion

    #region Moves Vars
    Vector3 moveDirection;
    float MomentumOnJump;
    private float airControlDir = 0;
    float previousTickHorizontalVelocity = 0f;
    #endregion

    #region Internal Components
    [HideInInspector]
    public Collider thisCollider;
    #endregion

    #region external components
    CharactersSharedVariables sharedVariables;
    #endregion

    #region experimental
    public float stepMaxHeight;
    public float stepLengthDetection = 1f;
    float closeToStepPercent = 0f;
    bool OnStep = false;
    float groundPointForStep = 0f;
    #endregion

    void CheckStep ()
    {
        RaycastHit horizontalHit;
        RaycastHit verticalHit;
        RaycastHit secondVerticalHit;
        Vector3 horizontalOrigin = new Vector3(thisCollider.bounds.min.x, thisCollider.bounds.min.y + .01f, transform.position.z);

        if (Physics.Raycast(horizontalOrigin, -transform.right, out horizontalHit, stepLengthDetection))
        {
            //Debug.Log("Hit = " + horizontalHit.point);
            //Debug.DrawLine(horizontalOrigin, horizontalHit.point, Color.red);
            Vector3 verticalOrigin = new Vector3(horizontalHit.point.x, thisCollider.bounds.max.y - .01f, transform.position.z);

            if (Physics.Raycast(verticalOrigin, -transform.up, out verticalHit, thisCollider.bounds.size.y))
            {
                Physics.Raycast(verticalOrigin + transform.right * .1f, -transform.up, out secondVerticalHit, Mathf.Infinity);
                float distanceBetweenStepAndGround = verticalHit.point.y - secondVerticalHit.point.y;

                if (distanceBetweenStepAndGround <= stepMaxHeight)
                {
                    //Debug.DrawLine(verticalOrigin, verticalHit.point, Color.red);
                    Vector3 stepEdge = new Vector3(horizontalHit.point.x, verticalHit.point.y, transform.position.z);
                    Debug.DrawLine(transform.position, stepEdge, Color.red);
                    closeToStepPercent = Mathf.Abs(thisCollider.bounds.min.x - horizontalHit.point.x) / stepLengthDetection;
                    closeToStepPercent = Mathf.Abs(1 - closeToStepPercent);
                    Debug.Log("STAIRS OR STEP RIGHT AHEAD CAPTAIN =D " + closeToStepPercent);
                    Physics.IgnoreCollision(verticalHit.collider, thisCollider, true);


                    Debug.DrawLine(transform.position, secondVerticalHit.point, Color.blue);

                    groundPointForStep = Mathf.Abs(distanceBetweenStepAndGround);

                    //Physics.Raycast (verticalOrigin);

                    float targetHeight = Mathf.Lerp(secondVerticalHit.point.y + thisCollider.bounds.size.y / 2, verticalHit.point.y + groundPointForStep + thisCollider.bounds.size.y / 2, closeToStepPercent);
                    transform.position = new Vector3(transform.position.x, targetHeight, transform.position.z);
                    Debug.DrawLine(transform.position, new Vector3(transform.position.x, targetHeight, transform.position.z), Color.red);
                    OnStep = true;
                    Debug.Log(targetHeight);
                }
                else
                    OnStep = false;
            }
            else
            {
                OnStep = false;
            }
        }
        else
            OnStep = false;
    }

    // Use this for initialization
    void Awake ()
    {
        sharedVariables = GameObject.Find ("CharactersManager").GetComponent<CharactersSharedVariables>();
        thisCollider = this.gameObject.GetComponent<Collider>();

        if (sharedVariables == null)
            Debug.LogError ("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");
	}

    bool TouchingHead()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxUpSideCount >= 4)
            return true;
        else
            return false;
    }

    bool TouchingWallOnLeft()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxLeftSideCount >= 4)
        {

            //The vertical cast will check the step's height
            //The horizontal cast is there to double-check if the vertical cast origin point was inside a collider.
            RaycastHit verticalHit;
            RaycastHit horizontalHit;

            Vector3 verticalRaycastOrigin = new Vector3(thisCollider.bounds.min.x - .05f, thisCollider.bounds.max.y, transform.position.z);
            Vector3 horizontalRaycastOrigin = new Vector3(thisCollider.bounds.max.x - .05f, thisCollider.bounds.max.y - .05f, transform.position.z);
            Vector3 horizontalRayCastDirection = verticalRaycastOrigin - horizontalRaycastOrigin;
            float horizontalRaycastDistance = horizontalRayCastDirection.magnitude;

            //Raycasting the vertical cast
            Physics.Raycast(verticalRaycastOrigin, -transform.up, out verticalHit, Mathf.Infinity);
            //Debug.DrawRay(horizontalRaycastOrigin, horizontalRayCastDirection * horizontalRaycastDistance, Color.blue);

            //If the horizontal cast hits something, then it is a wall, let's return true.
            if (Physics.Raycast(horizontalRaycastOrigin, horizontalRayCastDirection, out horizontalHit, horizontalRaycastDistance))
            {
                Debug.Log("HorizontalHit = " + horizontalHit.collider.gameObject.name);
                return true;
            }
            //Else, it may be a step small enough for Pauline to climb it, let's check this
            else if (verticalHit.distance >= 1.5f && gameObject.GetComponent<CollisionTests>().xHighestDiff > .1f)
            {
                //Debug.Log("Not touching left wall because hit.distance = " + verticalHit.distance);

                //Yup, it was! If Pauline is moving, we make her climb the step right now
                if (moveDirection.x != 0)
                    ClimbSmallStep(verticalHit.distance - thisCollider.bounds.size.y);

                return false;
            }
            else
                return true;
        }
        else //Not enough contact point on Pauline's side, so we return immediately false without any more verifications
        {
            Debug.Log("Not touching left wall because MaxLeftSideCount is " + gameObject.GetComponent<CollisionTests>().MaxLeftSideCount);
            return false;
        }
    }

    void ClimbSmallStep (float stepHeight)
    {
        transform.position += Vector3.up * (Mathf.Abs (stepHeight) + .05f);
    }

    bool TouchingWallOnRight()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxRightSideCount >= 4 && gameObject.GetComponent<CollisionTests>().yHighestDiff >= minimumWallSize)
            //TODO: Replace minimumWallSize with a percentage of Pauline's collider height for better control
            return true;
        else
            return false;
    }

    bool CheckIfGrounded()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxDownSideCount >= 4 && gameObject.GetComponent<CollisionTests>().xHighestDiff >= .01f
            || OnStep)
        //TODO: Replace the .01f to a percentage of Pauline's collider width, just in case we modify the collider's width and this gets broken
        {
            Debug.Log("Grounded");
            return true;
        }
        else
        {
            //Debug.Log ("Not grounded because maxSideCount = " + gameObject.GetComponent<CollisionTests>().MaxDownSideCount " and xHighestDiff)
            return false;
        }
    }

    public void Move (float HorizontalDirection)
    {
        Move(HorizontalDirection, 0, false);
    } 

    public void Move(float HorizontalDirection, bool jump)
    {
        Move(HorizontalDirection, 0, jump);
    }

    public void CornerStuck ()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxLeftSideCount >= 1)
            transform.position = transform.position + transform.right * .01f;

        Debug.Log("DESTUCKING OKER");
    }

    //Main Move Method
    public void Move (float HorizontalDirection, float VerticalDirection, bool jump)
    {
        CheckStep();

        if (CheckIfGrounded())
        {
            //Debug displayed when grounded
            //Debug.DrawRay(transform.position, -transform.up * thisCollider.bounds.size.y, Color.red);

            moveDirection = new Vector3(HorizontalDirection, VerticalDirection);
            moveDirection *= speed;

            if (jump)
            {
                //Debug.Break();
                moveDirection.y = jumpStrength;
                MomentumOnJump = thisCollider.GetComponent<Rigidbody>().velocity.x; //Using real speed instead of calculated one in case we are jumping from against a wall
            }
        }
        else
        {
            if (TouchingHead())
                moveDirection.y = -sharedVariables.Gravity * Time.deltaTime;

            AirControl(HorizontalDirection);
        }

        ApplyGravity();

        //Cancelling directions if Pauline is pushing solid walls (Avoid glitches with jumps)
        /*if ((TouchingWallOnLeft() && moveDirection.x < 0)
            || (TouchingWallOnRight() && moveDirection.x > 0))
            moveDirection.x = 0;*/ //TODO: Turn this back on when the step detection is finished

        //Debug.Log("Highest Y Diff = " + gameObject.GetComponent<CollisionTests>().yHighestDiff);

        //Adding some time for Pauline to do a U-turn more naturally
        Brakes();

        //Flipping sprite to face latest direction
        if (previousTickHorizontalVelocity == 0f)
        {
            if (moveDirection.x < 0)
                gameObject.GetComponent<SpriteRenderer>().flipX = true;
            else if (moveDirection.x > 0)
                gameObject.GetComponent<SpriteRenderer>().flipX = false;
        }

        thisCollider.GetComponent<Rigidbody>().velocity = moveDirection;
        //Debug.Log("Applied gravity is = " + thisCollider.GetComponent<Rigidbody>().velocity.y);
        previousTickHorizontalVelocity = thisCollider.GetComponent<Rigidbody>().velocity.x;
        //Debug.Log(previousTickHorizontalVelocity);
    }

    bool braking = false;
    float waitBeforeMove = 0f;
    bool flipped = false;
    List<float> velocityHistory = new List<float>();

    void Brakes()
    {
        velocityHistory.Add(moveDirection.x);
        if(Time.time > .1f)
        {
            //Debug.Log("Velocity from .1 secondes = " + velocityHistory[0]);
            velocityHistory.RemoveAt(0);
        }

        if (!braking)
        {
                if ((velocityHistory[0] > 0 && moveDirection.x < 0)
                    || (velocityHistory[0] < 0 && moveDirection.x > 0))
                {
                    if(CheckIfGrounded())
                        braking = true;
                }
        }
        else
        {
            //Debug.DrawRay(transform.position, transform.right * 5f, Color.blue);

            if (!flipped)
            {
                if (gameObject.GetComponent<SpriteRenderer>().flipX)
                    gameObject.GetComponent<SpriteRenderer>().flipX = false;
                else
                    gameObject.GetComponent<SpriteRenderer>().flipX = true;

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
                moveDirection.x = 0;
                waitBeforeMove += Time.deltaTime;
            }
        }
    }

    void ApplyGravity ()
    {
        /*if ( (gameObject.GetComponent<CollisionTests>().MaxDownSideCount < 4 && gameObject.GetComponent<CollisionTests>().MaxLeftSideCount < 2)
            || (gameObject.GetComponent<CollisionTests>().MaxDownSideCount < 4 && gameObject.GetComponent<CollisionTests>().MaxRightSideCount < 2))*/
            moveDirection.y -= sharedVariables.Gravity * Time.deltaTime;

        Debug.Log("Gravity applied");
    }

    void AirControl (float MomentumInfluenceBaseRate)
    {
        if (MomentumInfluenceBaseRate != 0) //This check is to prevent the character to just stop the momentum in middle-air
            MomentumOnJump = Mathf.Lerp(previousTickHorizontalVelocity, MomentumInfluenceBaseRate * speed, jumpMomentumInfluenceRate);

        moveDirection.x = MomentumOnJump;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -speed, speed);
    }

}
