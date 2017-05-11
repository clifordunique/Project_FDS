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
        [HideInInspector]
        bool alreadyDashedInAir = false;
    #endregion

    #region moves States
        bool jump = false;
        [HideInInspector]
        public bool canDashFromAttachment = false;
        [HideInInspector]
        public GameObject dashAttachment = null;
        [HideInInspector]
        public List<Collider> attachmentColliders = new List<Collider>();

        //Dash state vars
        [HideInInspector]
        public bool dashing = false;
        [HideInInspector]
        public float dashTimer = 0f;
        [HideInInspector]
        public float dashCoolDownTimer = 0f;
        Vector3 dashDirection;

        //Ledge grab state vars
        [SerializeField]
        float PaulineHeightPercentToGrabLedge = .7f;
        [SerializeField]
        float ledgeGrabSpeed = 10f;
        bool ClimbingLedge = false;
        #endregion

    private void Start()
    {
        dashCoolDownTimer = dashCoolDown;
    }

    void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();
        jump = Input.GetButtonDown("Jump");

        if (!dashing && dashAttachment == null && !ClimbingLedge)
        {
            Move(Input.GetAxisRaw("Horizontal"), jump);
            Debug.Log("Regular moves");
        }

        if (ClimbingLedge)
            LedgeGrab();

        //Check for the ledge grab
        if (!CheckIfGrounded() && (collisionTests.MaxRightSideCount >= 4 || collisionTests.MaxLeftSideCount >= 4))
        {
            Vector3 localLedgePosition = transform.InverseTransformPoint(new Vector3(transform.position.x, collisionTests._highestContact, transform.position.z));
            float ledgeGrabHeight = thisCollider.bounds.size.y * PaulineHeightPercentToGrabLedge;

            //Debug.Log("Touching something on the right side, highest local = " + transform.InverseTransformPoint(new Vector3(thisCollider.bounds.min.x, collisionTests._highestContact, thisCollider.bounds.min.z)));
            Debug.DrawLine(thisCollider.bounds.min, new Vector3(transform.position.x, collisionTests._highestContact, transform.position.z), Color.red);

            if (Mathf.Abs (thisCollider.bounds.min.y + ledgeGrabHeight - collisionTests._highestContact) < .1f)
            {
                Debug.Log("READY TO LEDGE GRAB");
                CancelJump();
                LedgeGrab();
            }
        }
	}

    private void Update()
    {


        if (dashing && !CheckIfGrounded())
        {
            alreadyDashedInAir = true;
        }

        if (CheckIfGrounded())
            alreadyDashedInAir = false;

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
        }

        if (Input.GetButtonDown("Dash") && !dashing && !alreadyDashedInAir)
        {
            //Debug.Log("Dash Button Pressed...");
            if (canDashFromAttachment)
            {
                Debug.Log("... from " + dashAttachment.name + ".");
                canDashFromAttachment = false;
                dashAttachment.GetComponentInChildren<DashGrabPointOrientation>().coolDownTimer = 0f;
                dashAttachment = null;

                foreach (Collider collider in attachmentColliders)
                {
                    Physics.IgnoreCollision(thisCollider, collider, false);
                }

                attachmentColliders.Clear();
                StartDashFromAttachment();
            }
            else
            {
                //Debug.Log("... from nothing");
                StartRegularDash();
            }
        }
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

    void LedgeGrab ()
    {
        if (collisionTests.MaxRightSideCount >= 4 || collisionTests.MaxLeftSideCount >= 4)
        {
            ClimbingLedge = true;
            thisRigidbody.velocity = transform.up * ledgeGrabSpeed;
            Debug.Log("Climbing Edge");
        }
        else
        {
            Debug.Log("End Ledge Climb");
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
