using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    #region Player Specific Parameters
        [SerializeField]
        float dashDuration = 1f;
        [SerializeField]
        float dashSpeed = 10f;
    #endregion

    #region moves States
        bool jump = false;

        bool dashing = false;
        float dashTimer = 0f;
        Vector3 dashDirection;
    #endregion

    private void Start()
    {
    }

    void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();
        jump = Input.GetButtonDown("Jump");

        if(!dashing)
            Move(Input.GetAxisRaw("Horizontal"), jump);
	}

    private void Update()
    {
        if (Input.GetButtonDown("Dash") || dashing)
            Dash();
    }

    void Dash ()
    {
        if (dashTimer <= dashDuration)
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
        else
        {
            dashing = false;
            thisRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            //The rest of the time, a ContinuousDynamic detection mode can result in the player getting stuck. Besides, it's pretty expensive, so we switch back do Discrete detection.
            dashTimer = 0f;
        }

        thisRigidbody.velocity = dashDirection * dashSpeed;
    }

}
