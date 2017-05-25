using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Characters : MonoBehaviour {

    //TODO : Replace all the GetComponents into a single one for optimization


    #region Inspector Variables
    public float speed = 10;
    [SerializeField]
    float jumpMomentumInfluenceRate = .1f;
    public float jumpStrength = 12f;
    [SerializeField]
    float minimumWallSize = .25f;
    #endregion

    #region Moves Vars
        public Vector3 moveDirection;
        private bool jumping;
        [HideInInspector]
        public bool deactivateNormalGravity = false;
        bool climbingDropDownPlatform = false;

        [HideInInspector]
        public float MomentumOnJump;
        float previousTickHorizontalVelocity = 0f;
        float previousTickVerticalVelocity = 0f;
    [HideInInspector]
    public float FallDragMultiplier = 0f;

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
        [HideInInspector]
        public float SlopeAngle = 0f;
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

    //Main Move Method
    public void Move(float HorizontalDirection, float VerticalDirection, bool jump, bool dash)
    {
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

    public void Move (float HorizontalDirection, float VerticalDirection, bool jump)
    {
        Move(HorizontalDirection, VerticalDirection, jump, false);
    }

#region Other Moves
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
                moveDirection.x = 0;
                waitBeforeMove += Time.deltaTime;
            }
        }
    }

    void ApplyGravity ()
    {
        ApplyGravity(sharedVariables.Gravity);
    }

    public void ApplyGravity (float gravityOverride)
    {
        moveDirection.y -= gravityOverride * Time.deltaTime;
    }

    void AirControl (float MomentumInfluenceBaseRate)
    {
        //MomentumOnJump = 0;
        //MomentumInfluenceBaseRate = 0;

        if (MomentumInfluenceBaseRate != 0) //This check is to prevent the character to just stop the momentum in middle-air
            MomentumOnJump = Mathf.Lerp(previousTickHorizontalVelocity, MomentumInfluenceBaseRate * speed, jumpMomentumInfluenceRate);

        moveDirection.x = MomentumOnJump;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -speed, speed);
    }

    public void CancelJump ()
    {
        jumping = false;
        moveDirection.y = 0f;
    }
#endregion

#region Collision processing Methods
    bool TouchingHead()
    {
        if (collisionTests.MaxUpSideCount >= 4)
            return true;
        else
            return false;
    }

    public bool TouchingWallOnLeft()
    {
        if (collisionTests.MaxLeftSideCount >= 4)
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
                //Debug.Log("HorizontalHit = " + horizontalHit.collider.gameObject.name);
                return true;
            }
            else
                return false;
        }
        else //Not enough contact point on Pauline's side, so we return immediately false without any more verifications
        {
            //Debug.Log("Not touching left wall because MaxLeftSideCount is " + collisionTests.MaxLeftSideCount);
            return false;
        }
    }

    public bool TouchingWallOnRight() //TODO: is there a reason the left wall detection has more stuff? Plz investigate and clean up.
    {
        if (collisionTests.MaxRightSideCount >= 4 && collisionTests.yHighestDiff >= minimumWallSize)
            //TODO: Replace minimumWallSize with a percentage of Pauline's collider height for better control
            return true;
        else
            return false;
    }

    void ClimbSmallStep(float stepHeight)
    {
        transform.position += Vector3.up * (Mathf.Abs(stepHeight) + .05f);
    }

    public bool CheckIfGrounded()
    {
        if (OnSlope)
            return true;

        if (moveDirection.y >= 0 && jumping)
            return false;

        if (collisionTests.MaxDownSideCount >= 4 && collisionTests.xHighestDiff >= .01f
            || OnStep)
        //TODO: Replace the .01f to a percentage of Pauline's collider width, just in case we modify the collider's width and this gets broken
        {
            //Debug.Log(transform.name + " is Grounded");
            return true;
        }
        else
        {
            return false;
        }
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

        /*float RayRange = .1f;

        if (CheckIfGrounded() && !OnSlope)
            RayRange = Mathf.Infinity;*/

        foreach (Ray singleRay in ray)
        {
            //Debug.DrawRay(thisCollider.bounds.min, -transform.right, Color.cyan);
            if (Physics.Raycast(singleRay, out hit, .1f))
            {
                float NormalAngle = Vector3.Angle(transform.right, hit.normal);
                if (Mathf.Abs(NormalAngle - 90) > .1f && Mathf.Abs(NormalAngle) > .1f && Mathf.Abs(NormalAngle - 180) > .1f)
                {
                    OnSlope = true;
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
