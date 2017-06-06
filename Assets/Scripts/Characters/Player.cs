﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    #region Player Specific Parameters
        [SerializeField]
        float dashDuration = 1f;
        [SerializeField]
        float dashSpeed = 10f;
        [SerializeField]
        float dashCoolDown = .5f;
        [SerializeField]
        float swallJmupDuration = .2f;
        [SerializeField]
        float minimumHeightTouchForSwallJmup = .99f;
    #endregion

    #region moves States
    //bool Input.GetButtonDown("Jump") = false;
    [HideInInspector]
        public bool canDashFromAttachment = false;
        [HideInInspector]
        public GameObject dashAttachment = null;
        [HideInInspector]
        public List<Collider> attachmentColliders = new List<Collider>();
        bool crouching = false;
        Vector3 standingColliderSize;
        Vector3 standingColliderPos;
        float swallJmupTimer = 0f;

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

        //Swall Jmup state vars
        bool swallJmuping = false;
        bool jump = false;
        bool justJumped = false;
        float swallJmupDirection = 0;

        //Ledge grab state vars
        [SerializeField]
        float PaulineHeightPercentToGrabLedge = .7f;
        [SerializeField]
        float ledgeGrabSpeed = 10f;
        bool ClimbingLedge = false;
    #endregion

    #region Experimental
    [SerializeField]
    float jumpHeight = 4;
    [SerializeField]
    float timeToJumpApex = .4f;
    float calculatedJumpForce;
    float calculatedGravity;
    float velocityXSmoothing;

    float accelerationTimeAir = .2f;
    float accelerationTimeGrounded = .1f;
    #endregion

    Transform mdr;


    private void Start()
    {
        dashCoolDownTimer = dashCoolDown;
        swallJmupTimer = swallJmupDuration;
        standingColliderSize = gameObject.GetComponent<BoxCollider>().size;
        standingColliderPos = gameObject.GetComponent<BoxCollider>().center;
        mdr = transform.Find("mdr");
        mdr.gameObject.SetActive(false);

        #region experimental
        CalculateRaySpacing();

        calculatedGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        calculatedJumpForce = Mathf.Abs(calculatedGravity) * timeToJumpApex;
        #endregion
    }

    /*void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();

        CheckForSwallJmup();
        ContinueSwallJmup();

        //regular moves
        if (!dashing && dashAttachment == null && !ClimbingLedge && !swallJmuping)
        {
            Move(Input.GetAxisRaw("Horizontal"), jump);

            //Debug.Log("Jump = " + jump);

            if (jump) //JustJumped is used for the SwallJmup
                justJumped = true;
            else
                justJumped = false;
        }

        if (Input.GetAxisRaw("Vertical") <= -.5f && (CheckIfGrounded() || OnSlope))
        {
            Collider DropDownPlatform = CheckIfGroundedInDropDownPlatform();

            if (Input.GetButtonDown("Jump") && DropDownPlatform != null)
            {
                Physics.IgnoreCollision(thisCollider, DropDownPlatform, true);
                justDroppedPlatform = DropDownPlatform;
            }
            else
                Crouch();
        }
        else
            Stand();

        if (ClimbingLedge)
            LedgeGrab();

        //Check for the ledge grab
        LedgeGrabCheck();

	}*/

    Vector3 input;

    private void Update()
    {
        #region Experimental
        UpdateAnimator();

        jump = Input.GetButtonDown("Jump");

        if (!swallJmuping)
            input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input.y < 0 && collisions.below && _moveDirection.y == 0)
            Crouch();
        else
            Stand();


        //Debug.Log("Through below = " + collisions.getThroughBelow + " crouch = " + crouching + " jump = " + jump);
        if (collisions.getThroughBelow && crouching && jump)
        {
            Debug.Log("Yeah" + justDroppedPlatform.name);
            CancelJump();
            justDroppedPlatform.gameObject.layer = 0;
        }
        else if (jump && collisions.below && attachmentColliders.Count == 0)
        {
            _moveDirection.y = calculatedJumpForce;
            jumping = true;
        }



        //if (justJumped)

        float targetVelocityX = input.x * speed;

        if (!swallJmuping)
            _moveDirection.x = Mathf.SmoothDamp(_moveDirection.x, targetVelocityX, ref velocityXSmoothing, collisions.below ? accelerationTimeGrounded : accelerationTimeAir);
        else
            _moveDirection.x = targetVelocityX;

        _moveDirection.y += calculatedGravity * Time.deltaTime;
        //ApplyGravity();

        if (!swallJmuping)
            CheckForSwallJmup();
        else
            ContinueSwallJmup();

        if (_moveDirection.y < 0 && MaxWallSlideSpeed != 0) //Drag against a wall
        {
            _moveDirection.y = -MaxWallSlideSpeed;
            //Debug.Log("DRAG");
        }

        if (Input.GetButtonDown("Dash") && !dashing && !alreadyDashedInAir)
        {
            //Debug.Log("Dash Button Pressed...");
            if (canDashFromAttachment)
            {
                PostGrabDetach();
                StartDashFromAttachment();
            }
            else if (!swallJmuping)
            {
                //Debug.Log("... from nothing");
                StartRegularDash();
            }
        }

        if (!ClimbingLedge)
            LedgeGrabCheck();
        else
            LedgeGrab();

        Move(_moveDirection * Time.deltaTime);

        if (collisions.above || collisions.below || dashing)
        {
            _moveDirection.y = 0;
        }

        if (!dashing) //Dashing cooldown
        {
            dashCoolDownTimer += Time.deltaTime;

            if (dashCoolDownTimer > dashCoolDown)
                dashCoolDownTimer = dashCoolDown;
        }
        else
            ContinueDash();

        //If attached to an enemy after a dash
        if (dashAttachment != null)
        {
            thisSprite.flipX = dashAttachment.GetComponentInParent<SpriteRenderer>().flipX;
        }

        if (dashing && !collisions.below)
        {
            alreadyDashedInAir = true;
        }

        if (collisions.below)
        {
            alreadyDashedInAir = false;
        }

        if (dashAttachment != null)
        {
            transform.position = dashAttachment.transform.position;

            //Jumping from attachement
            if (jump)
            {
                _moveDirection.y = calculatedJumpForce;
                jumping = true;
                //Move(Vector3.zero);
                PostGrabDetach();
            }
            else if (Input.GetAxisRaw("Vertical") < -.5f)
            {
                PostGrabDetach();
            }
        }

        #endregion

        /*
        //Input.GetButtonDown("Jump") = Input.GetButtonDown("Input.GetButtonDown("Jump")");
        UpdateAnimator();
        jump = Input.GetButtonDown("Jump");


        */
    }

/*private void LateUpdate()
{
   //Debug.Log("Dash Attachment = " + dashAttachment);
   if (dashing)
       ContinueDash();

   //Special moves when attached to an enemy
 

   if (Input.GetButtonDown("Dash") && !dashing && !alreadyDashedInAir)
   {
       //Debug.Log("Dash Button Pressed...");
       if (canDashFromAttachment)
       {
           PostGrabDetach();
           StartDashFromAttachment();
       }
       else if (!swallJmuping)
       {
           //Debug.Log("... from nothing");
           StartRegularDash();
       }
   }
}*/

   void PostGrabDetach ()
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

    void CheckForSwallJmup ()
    {
        if ((collisions.left || collisions.right) && !collisions.below)
        {
            MaxWallSlideSpeed = 4.5f;

            if (jump && !justJumped)
            {
                MaxWallSlideSpeed = 0f;
                if (collisions.left)
                {
                    swallJmupDirection = 1;
                    //thisSprite.flipX = false;
                }
                else if (collisions.right)
                {
                    swallJmupDirection = -1;
                    //thisSprite.flipX = true;
                }

                swallJmupTimer = 0f;
                _moveDirection.y = calculatedJumpForce;
                swallJmuping = true;
                jump = false;
            }
            //Debug.Log("Ready to Swall Jmup");
        }
        else
        {
            MaxWallSlideSpeed = 0f;
            //deactivateNormalGravity = false;
        }
    }

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

    void UpdateAnimator ()
    {
        animator.SetBool("Crouching", crouching);
        animator.SetBool("Dashing", dashing);
    }

    void Crouch ()
    {
        thisCollider.GetComponent<BoxCollider>().center = new Vector3(0, -0.8156404f, 0);
        thisCollider.GetComponent<BoxCollider>().size = new Vector3(.43f, 0.4187193f, .2f);

        if (collisions.climbingSlope || collisions.descendingSlope)
        {
            //Debug.Log("Pauline Sprite Rotation Euler = " + thisSprite.transform.eulerAngles + " & Slope Angle = " + SlopeAngle);
            //thisSprite.transform.rotation = new Quaternion(thisSprite.transform.rotation.x, thisSprite.transform.rotation.y, -SlopeAngle, thisSprite.transform.rotation.w);
            //Debug.Log(collisions.slopeAngle);
            //TODO : Fix this with the new slope detection system
            if(!mirrorSlope)
                thisSprite.transform.eulerAngles = new Vector3(thisSprite.transform.eulerAngles.x, thisSprite.transform.eulerAngles.y, -collisions.slopeAngle);
            else
                thisSprite.transform.eulerAngles = new Vector3(thisSprite.transform.eulerAngles.x, thisSprite.transform.eulerAngles.y, collisions.slopeAngle);
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

    void StartDashFromAttachment ()
    {
            //Debug.Log("Started dash from attachment");

            dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, 0);

            //Debug.Break();
            //Debug.Log("DASHING From attachment");
            if (Mathf.Abs(dashDirection.x) < 0.5f)
            {
                //Debug.Log("No direction given");
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
        //Debug.Log("Start regular Dash");
        dashTimer = 0f;

        if (!dashing && dashCoolDownTimer >= dashCoolDown)
        {

            dashDirection = new Vector3(input.x, 0, 0);

            if (Mathf.Abs(dashDirection.x) < .5f)
            {
                if (thisSprite.flipX == true)
                {
                    dashDirection = -transform.right;
                }
                else
                {
                    dashDirection = transform.right;
                }
            }

            dashing = true;
            ContinueDash();
        }
    }

    void ContinueDash()
    {
        CancelJump();

        //Continuity of dash
        //Debug.Log("Dashing");
        dashTimer += Time.deltaTime;
        //thisRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        //Dash can be pretty fast, so it's better to use ContinuousDynamic to prevent some noclip glitches.

        _moveDirection = dashDirection * dashSpeed;
        //MomentumOnJump = thisRigidbody.velocity.x; //To make sure Pauline will face the right way when a dash is ending in midair

        //Finish Dash
        if (dashTimer > dashDuration)
        {
            Debug.Log("Finishing dash");
            StopAndResetDashNGrab(false);
        }
    }

    float LedgeYPos = 0;
    bool ledgeLeft = false;

    //TODO HERE OKAY
    void LedgeGrabCheck ()
    {

        if(!collisions.below)
        {
            if (collisions.right)
            {
                RaycastHit hit;
                Vector3 origin = raycastOrigins.topRight + transform.right * skinWidth;

                Debug.DrawRay(origin, -Vector3.up * thisCollider.bounds.size.y * .15f, Color.red);

                if (Physics.Raycast(origin, -Vector3.up, out hit, thisCollider.bounds.size.y * .15f, collisionMask))
                {
                    ledgeLeft = false;
                    LedgeYPos = hit.point.y;
                    Debug.Log("Can grab ledge !");
                    CancelJump();
                    LedgeGrab();
                }
            }
            else if (collisions.left)
                {
                    RaycastHit hit;
                    Vector3 origin = raycastOrigins.topLeft - transform.right * skinWidth;

                    Debug.DrawRay(origin, -Vector3.up * thisCollider.bounds.size.y * .15f, Color.red);

                    if (Physics.Raycast(origin, -Vector3.up, out hit, thisCollider.bounds.size.y * .15f, collisionMask))
                    {
                        ledgeLeft = true;
                        LedgeYPos = hit.point.y;
                        Debug.Log("Can grab ledge !");
                        CancelJump();
                        LedgeGrab();
                    }
                }
        }

        /*if (!collisions.below &&
        ((collisions.right /*&& /*!thisSprite.flipX*//*) ||
        ((collisions.left /*&& /*thisSprite.flipX*//*))))
        {
            Vector3 localLedgePosition = transform.InverseTransformPoint(new Vector3(transform.position.x, collisions.highestContact, transform.position.z));
            float ledgeGrabHeight = thisCollider.bounds.size.y * PaulineHeightPercentToGrabLedge;

            Debug.Log("Touching something on the right side, highest local = " + transform.InverseTransformPoint(new Vector3(thisCollider.bounds.min.x, collisions.highestContact, thisCollider.bounds.min.z)));
            Debug.DrawLine(thisCollider.bounds.min, new Vector3(transform.position.x, collisions.highestContact, transform.position.z), Color.red);

            if (collisions.highestContact < raycastOrigins.topLeft.y && collisions.highestContact > raycastOrigins.bottomLeft.y + thisCollider.bounds.size.y * .8f)
            {
                Debug.Log("READY TO LEDGE GRAB");
                CancelJump();
                LedgeGrab();
            }
        } */
    }

    void LedgeGrab ()
    {
        if (raycastOrigins.bottomLeft.y <= LedgeYPos)
        {
            ClimbingLedge = true;
            _moveDirection = transform.up * speed;
            Debug.Log("Climbing Edge");
        }
        else
        {
            if(ledgeLeft)
                _moveDirection = -transform.right * speed * .5f;
            else
                _moveDirection = transform.right * speed * .5f;

            Debug.Log("End Ledge Climb");
            ClimbingLedge = false;
        }

        if (Input.GetButtonDown("Jump")) //Cancelling Ledge Grab with a wall jump
        {
            MaxWallSlideSpeed = 0f;
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

    public void StopAndResetDashNGrab (bool calledByEnemy)
    {
        //Debug.Log("Finished dash with timer = " + dashTimer + " called by enemy = " + calledByEnemy);
        dashing = false;
        dashCoolDownTimer = 0f;
        dashTimer = 0f;
        //thisRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        alreadyDashedInAir = false;
        //The rest of the time, a ContinuousDynamic detection mode can result in the player getting stuck. Besides, it's pretty expensive, so we switch back do Discrete detection.
    }
}
