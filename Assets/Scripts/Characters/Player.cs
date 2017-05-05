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
        public GameObject dashAttachment = null;
        [HideInInspector]
        public List<Collider> attachmentColliders = new List<Collider>();

        //Dash state vars
        [HideInInspector]
        public bool dashing = false;
        float dashTimer = 0f;
        float dashCoolDownTimer = 0f;
        Vector3 dashDirection;
    #endregion

    private void Start()
    {
        dashCoolDownTimer = dashCoolDown;
    }

    void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();
        jump = Input.GetButtonDown("Jump");

        if (!dashing && dashAttachment == null)
            Move(Input.GetAxisRaw("Horizontal"), jump);
        

	}

    private void Update()
    {
        if (Input.GetButtonDown("Dash") || dashing)
            Dash();

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
    }

    private void LateUpdate()
    {
        Debug.Log("Dash Attachment = " + dashAttachment);

        if (dashAttachment != null)
        {
            transform.position = dashAttachment.transform.position;

            if (Input.GetButtonDown("Dash"))
            {
                dashAttachment.GetComponentInChildren<DashGrabPointOrientation>().coolDownTimer = 0f;
                dashAttachment = null;
                
                foreach (Collider collider in attachmentColliders)
                {
                    Physics.IgnoreCollision(thisCollider, collider, false);
                }

                attachmentColliders.Clear();
                Dash();
            }
        }
    }

    void Dash()
    {
        if (!alreadyDashedInAir || dashing)
        {
            if (dashTimer <= dashDuration && dashCoolDownTimer >= dashCoolDown)
            {
                dashing = true;

                //Debug.Log("Dashin'");
                if (!OnSlope)
                {
                    if (thisSprite.flipX)
                        dashDirection = -transform.right;
                    else
                        dashDirection = transform.right;
                }
                else
                {
                    if (thisSprite.flipX)
                        dashDirection = slopeDirection;
                    else
                        dashDirection = -slopeDirection;
                }

                dashTimer += Time.deltaTime;
                thisRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                //Dash can be pretty fast, so it's better to use ContinuousDynamic to prevent some noclip glitches.
            }
            else if (dashTimer > dashDuration)
            {
                dashing = false;
                thisRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                //The rest of the time, a ContinuousDynamic detection mode can result in the player getting stuck. Besides, it's pretty expensive, so we switch back do Discrete detection.
                dashTimer = 0f;
                dashCoolDownTimer = 0f;
            }

            thisRigidbody.velocity = dashDirection * dashSpeed;
        }
    }
}
