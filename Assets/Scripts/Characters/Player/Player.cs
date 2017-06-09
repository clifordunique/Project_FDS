using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    #region Inspector Customization
    //Dash Parameters
    [SerializeField]
    float dashDuration = 1f;     //TODO : Maybe replace this by travel distance?
    [SerializeField]
    float dashSpeed = 10f;
    [SerializeField]
    float dashCoolDown = .5f;

    //Basic moves Params
    [SerializeField]
    float accelerationTimeAir = .2f;
    [SerializeField]
    float accelerationTimeGrounded = .1f;

    //Swall Jmup
    [SerializeField]
    float swallJmupDuration = .2f;
    public float WallSlideSpeed = 4.5f;
    #endregion

    #region Status Vars
    //Dash state
    [HideInInspector]
    public bool canDashFromAttachment = false;
    [HideInInspector]
    public GameObject dashAttachment = null;
    [HideInInspector]
    public List<Collider> attachmentColliders = new List<Collider>();

    //Basic moves state
    bool crouching = false;
    Vector3 standingColliderSize;
    Vector3 standingColliderPos;

    //Swall Jmup State
    float swallJmupTimer = 0f;
    bool swallJmuping = false;
    bool jump = false;
    float swallJmupDirection = 0;

    //Dash state vars
    [HideInInspector]
    public bool dashing = false;
    [HideInInspector]
    public float dashTimer = 0f;
    [HideInInspector]
    public float dashCoolDownTimer = 0f;
    Vector3 dashDirection;
    [HideInInspector]
    bool alreadyDashedInAir = false;

    //Ledge grab state vars
    bool ClimbingLedge = false;
    float CurrentLedgeYPos = 0;
    bool ledgeOnLeft = false;

    //Misc state
    bool climbingDropDownPlatform = false;
    #endregion

    #region Processing Vars
    public Vector3 _moveDirection;
    float velocityXSmoothing;
    Vector3 input;

    //Hehehe...
    Transform mdr;
    #endregion

    private void Start()
    {
        //timers init
        dashCoolDownTimer = dashCoolDown;
        swallJmupTimer = swallJmupDuration;

        //Start collider size
        standingColliderSize = gameObject.GetComponent<BoxCollider>().size;
        standingColliderPos = gameObject.GetComponent<BoxCollider>().center;

        //Just to troll Abby, because I CAN THAT'S WHY
        mdr = transform.Find("mdr");
        mdr.gameObject.SetActive(false);

        //Collisions & Physics base calculation
        CalculateRaySpacing();

        calculatedGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        calculatedJumpForce = Mathf.Abs(calculatedGravity) * timeToJumpApex;
    }

    private void Update()
    {
        UpdateAnimator();

        //Walkin' and strollin' (but not on the beach) + some other basic moves
        if (!swallJmuping)
            input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        float targetVelocityX = input.x * speed;

        if (!swallJmuping && attachmentColliders.Count == 0)
            _moveDirection.x = Mathf.SmoothDamp(_moveDirection.x, targetVelocityX, ref velocityXSmoothing, collisions.below ? accelerationTimeGrounded : accelerationTimeAir);

        jump = Input.GetButtonDown("Jump");

        //Swall Jmup
        if (swallJmuping)
            _moveDirection.x = targetVelocityX;

        if (!swallJmuping)
            CheckForSwallJmup();
        else
            ContinueSwallJmup();

        //Ledge Grab
        if (!ClimbingLedge)
            LedgeGrabCheck();
        else
            LedgeGrab();

        //Dashing stuff
        if (!dashing)
        {
            //Dashing cooldown
            dashCoolDownTimer += Time.deltaTime;

            if (dashCoolDownTimer > dashCoolDown)
                dashCoolDownTimer = dashCoolDown;

            if (Input.GetButtonDown("Dash") && !alreadyDashedInAir && dashCoolDownTimer >= dashCoolDown)
            {
                CancelDropDownClimb();
                //Debug.Log("Dash Button Pressed...");
                if (canDashFromAttachment)
                {
                    PostGrabDetach();
                    StartDashFromAttachment();
                }
                else if (!swallJmuping)
                    StartRegularDash();
            }
        }
        else
            ContinueDash();

        if (dashAttachment != null)
            PostDashAttached();

        //Neutralizing Y moves when grounded, or head hitting ceiling or dashing
        if ( (collisions.above && !collisions.getThroughAbove) || collisions.below || attachmentColliders.Count > 0)
        {
            _moveDirection.y = 0;
        }
        else
        {
            //Drag against a wall
            if (_moveDirection.y < 0 && CurrentYSpeedMaxClamp != 0)
            {
                _moveDirection.y = -CurrentYSpeedMaxClamp;
            }
        }

        //Crouch & Stand
        if (input.y < 0 && collisions.below && _moveDirection.y == 0)
            Crouch();
        else
            Stand();

        if (collisions.getThroughBelow && crouching && jump) //Drop Down Platforms
        {
            CancelJump();
            justDroppedPlatform.gameObject.layer = 0;
        }
        else if (jump && collisions.below && attachmentColliders.Count == 0) //Regular Jump
        {
            Debug.Log("JUMP BITCH");
            _moveDirection.y = calculatedJumpForce;
            jumping = true;
        }

        //Applying Gravity
        if( attachmentColliders.Count == 0)
            _moveDirection.y += calculatedGravity * Time.deltaTime;

        DropDownPlatformsBehaviour(ref _moveDirection);

        //And finally, let's call the final method that will process collision before moving the player =D
        ApplyMoveAndCollisions(_moveDirection * Time.deltaTime);
    }

    #region Swall Jmup
    void CheckForSwallJmup ()
    {
        if ((collisions.left || collisions.right) && !collisions.below)
        {
            CurrentYSpeedMaxClamp = WallSlideSpeed;

            if (jump)
            {
                CurrentYSpeedMaxClamp = 0f;
                if (collisions.left)
                    swallJmupDirection = 1;
                else if (collisions.right)
                    swallJmupDirection = -1;

                swallJmupTimer = 0f;
                _moveDirection.y = calculatedJumpForce;
                swallJmuping = true;
                jump = false;
            }
        }
        else
            CurrentYSpeedMaxClamp = 0f;
    }

    //This will actually force the X direction for a small amount of time while Pauline is swall jmuping
    void ContinueSwallJmup ()
    {
        if (swallJmupTimer < swallJmupDuration)
        {
            swallJmupTimer += Time.deltaTime;
            input.x = swallJmupDirection;
            Debug.Log("Swall Jmuping");
            jump = false;
            mdr.gameObject.SetActive(true);
        }
        else
        {
            swallJmuping = false;
            mdr.gameObject.SetActive(false);
        }
    }
    #endregion

    #region StandNCrouch
    void Crouch ()
    {
        thisCollider.GetComponent<BoxCollider>().center = new Vector3(0, -0.8156404f, 0);
        thisCollider.GetComponent<BoxCollider>().size = new Vector3(.43f, 0.4187193f, .2f);

        if (collisions.climbingSlope || collisions.descendingSlope)
        {
            thisSprite.transform.rotation = Quaternion.identity; //This is to remove once I'm going to start working on the crouch orientation again...
            /*
            //TODO : Fix this with the new slope detection system
            if(!mirrorSlope)
                thisSprite.transform.eulerAngles = new Vector3(thisSprite.transform.eulerAngles.x, thisSprite.transform.eulerAngles.y, -collisions.slopeAngle);
            else
                thisSprite.transform.eulerAngles = new Vector3(thisSprite.transform.eulerAngles.x, thisSprite.transform.eulerAngles.y, collisions.slopeAngle);
                */
        }
        else
        {
            thisSprite.transform.rotation = Quaternion.identity;
        }

        crouching = true;
    }

    void Stand()
    {
        thisCollider.GetComponent<BoxCollider>().center = standingColliderPos;
        thisCollider.GetComponent<BoxCollider>().size = standingColliderSize;
        crouching = false;
        thisSprite.transform.rotation = Quaternion.identity;
    }
    #endregion

    #region Dash & Grab
    void StartDashFromAttachment()
    {
        dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, 0);

        //If no direction was given by input
        if (Mathf.Abs(dashDirection.x) < 0.5f)
        {
            if (thisSprite.flipX == true)
            {
                thisSprite.flipX = false;
                dashDirection = transform.right;
            }
            else
            {
                thisSprite.flipX = true;
                dashDirection = -transform.right;
            }
        }

        dashing = true;
        ContinueDash();
    }

    void StartRegularDash()
    {
        dashTimer = 0f;

        dashDirection = new Vector3(input.x, 0, 0);

        //If no direction given by input
        if (Mathf.Abs(dashDirection.x) < .5f)
        {
            if (thisSprite.flipX == true)
                dashDirection = -transform.right;
            else
                dashDirection = transform.right;
        }

        dashing = true;
        ContinueDash();
    }

    void ContinueDash()
    {
        CancelJump();

        dashTimer += Time.deltaTime;
        _moveDirection = dashDirection * dashSpeed;

        //Finish Dash
        if (dashTimer > dashDuration)
        {
            Debug.Log("Finishing dash");
            StopAndResetDashNGrab(false);
        }
    }

    public void StopAndResetDashNGrab(bool calledByEnemy)
    {
        //Debug.Log("Finished dash with timer = " + dashTimer + " called by enemy = " + calledByEnemy);
        dashing = false;
        dashCoolDownTimer = 0f;
        dashTimer = 0f;
        alreadyDashedInAir = false;
    }

    void PostGrabDetach()
    {
        //Debug.Log("... from " + dashAttachment.name + ".");
        canDashFromAttachment = false;
        dashAttachment.GetComponentInChildren<DashGrabPointOrientation>().coolDownTimer = 0f;
        dashAttachment = null;

        foreach (Collider collider in attachmentColliders)
        {
            Physics.IgnoreCollision(thisCollider, collider, false);
        }

        attachmentColliders.Clear();
    }

    void PostDashAttached ()
    {
        thisSprite.flipX = dashAttachment.GetComponentInParent<SpriteRenderer>().flipX;
        _moveDirection.x = 0;
        transform.position = dashAttachment.transform.position;

        //Jumping from attachement
        if (jump)
        {
            _moveDirection.y = calculatedJumpForce;
            jumping = true;
            PostGrabDetach();
        }
        else if (Input.GetAxisRaw("Vertical") < -.5f)
        {
            PostGrabDetach();
        }
    }

    void DashAirChecks()
    {
        if (dashing && !collisions.below)
        {
            alreadyDashedInAir = true;
        }

        if (collisions.below)
        {
            alreadyDashedInAir = false;
        }
    }
    #endregion

    #region LedgeGrab
    void LedgeGrabCheck ()
    {
        if(!collisions.below && !dashing)
        {
            RaycastHit hit;
            Vector3 origin;

            if (collisions.right)
            {
                origin = raycastOrigins.topRight + transform.right * skinWidth;
                ledgeOnLeft = false;
            }
            else
            {
                ledgeOnLeft = true;
                origin = raycastOrigins.topLeft - transform.right * skinWidth;
            }

            Debug.DrawRay(origin, -Vector3.up * thisCollider.bounds.size.y * .15f, Color.red);

            if (Physics.Raycast(origin, -Vector3.up, out hit, thisCollider.bounds.size.y * .15f, collisionMask))
            {
                if (!hit.transform.CompareTag("GoThroughPlatform"))
                {
                    Debug.Log("Start climbing ledge");
                    ClimbingLedge = true;
                    CurrentLedgeYPos = hit.point.y;
                    CancelJump();
                    LedgeGrab();
                }
            }
        }
    }

    void LedgeGrab ()
    {
        if (raycastOrigins.bottomLeft.y <= CurrentLedgeYPos)
        {
            ClimbingLedge = true;
            _moveDirection = transform.up * speed;
            //Debug.Log("Climbing Edge");
        }
        else
        {
            if(ledgeOnLeft)
                _moveDirection = -transform.right * speed * .5f;
            else
                _moveDirection = transform.right * speed * .5f;

            //Debug.Log("End Ledge Climb");
            ClimbingLedge = false;
        }

        if (Input.GetButtonDown("Jump")) //Cancelling Ledge Grab with a wall jump
        {
            CurrentYSpeedMaxClamp = 0f;
            if (thisSprite.flipX)
            {
                swallJmupDirection = 1;
                thisSprite.flipX = false;
            }
            else
            {
                swallJmupDirection = -1;
                thisSprite.flipX = true;
            }

            swallJmupTimer = 0f;
            _moveDirection.y = calculatedJumpForce;
            swallJmuping = true;
            jump = false;

            ClimbingLedge = false;
        }
    }
    #endregion

    #region Misc Methods
    void UpdateAnimator()
    {
        animator.SetBool("Crouching", crouching);
        animator.SetBool("Dashing", dashing);
    }

    void DropDownPlatformsBehaviour(ref Vector3 a_moveDirection)
    {
        if (justDroppedPlatform != null && jumping)
        {
            Debug.Log("Climbing for real lol");
            climbingDropDownPlatform = true;
        }

        if (climbingDropDownPlatform)
        {
            //TODO: Add a custom variable for speed
            _moveDirection.y = speed;
        }

        if (justDroppedPlatform != null && CheckIfGotPastDropDownPlatform(ref a_moveDirection))
        {
            Debug.Log("Got through drop down");
            justDroppedPlatform.gameObject.layer = LayerMask.NameToLayer("Ground");
            _moveDirection.y = 0;
            climbingDropDownPlatform = false;
            justDroppedPlatform = null;
        }
    }

    void CancelDropDownClimb()
    {
        justDroppedPlatform = null;
        climbingDropDownPlatform = false;
        CancelJump();
    }

    void CancelJump()
    {
        jumping = false;
        _moveDirection.y = 0f;
    }
    #endregion
}
