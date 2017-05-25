using System.Collections;
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

    Transform mdr;

    private void Start()
    {
        dashCoolDownTimer = dashCoolDown;
        swallJmupTimer = swallJmupDuration;
        standingColliderSize = gameObject.GetComponent<BoxCollider>().size;
        standingColliderPos = gameObject.GetComponent<BoxCollider>().center;
        mdr = transform.Find("mdr");
        mdr.gameObject.SetActive(false);
    }

    void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();

        CheckForSwallJmup();
        ContinueSwallJmup();

        //regular moves
        if (!dashing && dashAttachment == null && !ClimbingLedge && !swallJmuping)
        {
            Move(Input.GetAxisRaw("Horizontal"), jump);

            Debug.Log("Jump = " + jump);

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

	}

    private void Update()
    {
        //Input.GetButtonDown("Jump") = Input.GetButtonDown("Input.GetButtonDown("Jump")");
        UpdateAnimator();
        jump = Input.GetButtonDown("Jump");

        if (dashing && !CheckIfGrounded())
        {
            alreadyDashedInAir = true;
        }

        if (CheckIfGrounded())
        {
            alreadyDashedInAir = false;
        }

        if (!dashing)
        {
            dashCoolDownTimer += Time.deltaTime;

            if (dashCoolDownTimer > dashCoolDown)
                dashCoolDownTimer = dashCoolDown;
        }

        //If attached to an enemy after a dash
        if (dashAttachment != null)
        {
            thisSprite.flipX = dashAttachment.GetComponentInParent<SpriteRenderer>().flipX;
        }
    }

    private void LateUpdate()
    {
        //Debug.Log("Dash Attachment = " + dashAttachment);
        if (dashing)
            ContinueDash();

        //Special moves when attached to an enemy
        if (dashAttachment != null)
        {
            transform.position = dashAttachment.transform.position;

            //Jumping from attachement
            if (jump) //TODO: Kinda works but can't jump without a direction...
            {
                moveDirection.y = jumpStrength;
                MomentumOnJump = 0;
                Move(0);
                PostGrabDetach();
            }
            else if (Input.GetAxisRaw("Vertical") < -.5f)
            {
                PostGrabDetach();
            }
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
    }

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
        if ((TouchingWallOnRight() || TouchingWallOnLeft()) && Mathf.Abs(collisionTests.yHighestDiff - thisCollider.bounds.size.y) < .2f && !CheckIfGrounded())
        {
            FallDragMultiplier = 1.05f;

            if (jump && !justJumped)
            {
                FallDragMultiplier = 0f;
                if (TouchingWallOnLeft())
                {
                    swallJmupDirection = 1;
                    thisSprite.flipX = false;
                }
                else if (TouchingWallOnRight())
                {
                    swallJmupDirection = -1;
                    thisSprite.flipX = true;
                }

                swallJmupTimer = 0f;
                moveDirection.y = jumpStrength;
                swallJmuping = true;
                jump = false;
            }
            //Debug.Log("Ready to Swall Jmup");
        }
        else
        {
            FallDragMultiplier = 0f;
            //deactivateNormalGravity = false;
        }
    }

    void ContinueSwallJmup ()
    {
        if (swallJmupTimer < swallJmupDuration)
        {
            swallJmupTimer += Time.deltaTime;
            Move(swallJmupDirection, false);
            Debug.Log("Swall Jmuping");
            swallJmuping = true;
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

        if (OnSlope)
        {
            //Debug.Log("Pauline Sprite Rotation Euler = " + thisSprite.transform.eulerAngles + " & Slope Angle = " + SlopeAngle);
            //thisSprite.transform.rotation = new Quaternion(thisSprite.transform.rotation.x, thisSprite.transform.rotation.y, -SlopeAngle, thisSprite.transform.rotation.w);
            if(!mirrorSlope)
                thisSprite.transform.eulerAngles = new Vector3(thisSprite.transform.eulerAngles.x, thisSprite.transform.eulerAngles.y, -SlopeAngle);
            else
                thisSprite.transform.eulerAngles = new Vector3(thisSprite.transform.eulerAngles.x, thisSprite.transform.eulerAngles.y, SlopeAngle);
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

        if (dashTimer <= dashDuration && dashCoolDownTimer >= dashCoolDown)
        {

            dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, 0);

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
        thisRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        //Dash can be pretty fast, so it's better to use ContinuousDynamic to prevent some noclip glitches.

        thisRigidbody.velocity = dashDirection * dashSpeed;
        MomentumOnJump = thisRigidbody.velocity.x; //To make sure Pauline will face the right way when a dash is ending in midair

        //Finish Dash
        if (dashTimer > dashDuration)
        {
            StopAndResetDashNGrab(false);
        }
    }

    void LedgeGrabCheck ()
    {
        if (!CheckIfGrounded() &&
        ((collisionTests.MaxRightSideCount >= 4 && !thisSprite.flipX) ||
        (collisionTests.MaxLeftSideCount >= 4 && thisSprite.flipX)))
        {
            Vector3 localLedgePosition = transform.InverseTransformPoint(new Vector3(transform.position.x, collisionTests._highestContact, transform.position.z));
            float ledgeGrabHeight = thisCollider.bounds.size.y * PaulineHeightPercentToGrabLedge;

            //Debug.Log("Touching something on the right side, highest local = " + transform.InverseTransformPoint(new Vector3(thisCollider.bounds.min.x, collisionTests._highestContact, thisCollider.bounds.min.z)));
            Debug.DrawLine(thisCollider.bounds.min, new Vector3(transform.position.x, collisionTests._highestContact, transform.position.z), Color.red);

            if (Mathf.Abs(thisCollider.bounds.min.y + ledgeGrabHeight - collisionTests._highestContact) < .1f)
            {
                //Debug.Log("READY TO LEDGE GRAB");
                CancelJump();
                LedgeGrab();
            }
        }
    }

    void LedgeGrab ()
    {
        if (collisionTests.MaxRightSideCount >= 4 || collisionTests.MaxLeftSideCount >= 4)
        {
            ClimbingLedge = true;
            thisRigidbody.velocity = transform.up * ledgeGrabSpeed;
            //Debug.Log("Climbing Edge");
        }
        else
        {
            //Debug.Log("End Ledge Climb");
            ClimbingLedge = false;
        }
    }

    public void StopAndResetDashNGrab (bool calledByEnemy)
    {
        //Debug.Log("Finished dash with timer = " + dashTimer + " called by enemy = " + calledByEnemy);
        dashing = false;
        dashCoolDownTimer = 0f;
        dashTimer = 0f;
        thisRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        alreadyDashedInAir = false;
        //The rest of the time, a ContinuousDynamic detection mode can result in the player getting stuck. Besides, it's pretty expensive, so we switch back do Discrete detection.
    }
}
