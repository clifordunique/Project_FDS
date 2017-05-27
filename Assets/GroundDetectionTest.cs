using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundDetectionTest : MonoBehaviour {

    Rigidbody thisRigidBody;
    Collider thisCollider;

    bool groundedOnLeft = false;
    bool groundedOnRight = false;

    Vector3 moveDirection = Vector3.zero;

    public bool GoRight = false;

    bool jumping = false;
    bool previousTickOnSlope = false;
    bool previousTickOneSideGrounded = false;

	// Use this for initialization
	void Start () {
        thisRigidBody = gameObject.GetComponent<Rigidbody>();
        thisCollider = gameObject.GetComponent<Collider>();
	}
	
    //TODO : Try to enhance detection on strong slopes
    //TODO : Enhance behaviour when on a slope's edge and or a pointy slope
	// Update is called once per frame
	void FixedUpdate ()
    {
        RaycastHit leftHit;
        RaycastHit rightHit;

        Ray leftRay = new Ray(new Vector3 (thisCollider.bounds.min.x, thisCollider.bounds.min.y + .1f, thisCollider.bounds.center.z), -transform.up);
        Ray rightRay = new Ray(new Vector3(thisCollider.bounds.max.x, thisCollider.bounds.min.y + .1f, thisCollider.bounds.center.z), -transform.up);

        if (Physics.Raycast(leftRay, out leftHit, .15f))
        {
            //Debug.Log("Grounded on left");
            groundedOnLeft = true;
            previousTickOneSideGrounded = true;
        }
        else
            groundedOnLeft = false;

        if (Physics.Raycast(rightRay, out rightHit, .15f))
        {
            //Debug.Log("Grounded on right");
            groundedOnRight = true;
        }
        else
            groundedOnRight = false;



        if (!groundedOnLeft && !groundedOnRight && (!previousTickOnSlope || jumping) && (!previousTickOneSideGrounded || jumping))
        {
            Debug.Log("Applying gravity.");
            moveDirection.y -= 25 * Time.deltaTime;
        }
        else
            moveDirection.y = 0;

        Vector3 slopeDirection = Vector3.zero;

        if (!groundedOnLeft && groundedOnRight && !jumping)
        {
            slopeDirection = new Vector3(-rightHit.normal.y, rightHit.normal.x, 0f) / Mathf.Sqrt((rightHit.normal.x * rightHit.normal.x) + (rightHit.normal.y * rightHit.normal.y));
            previousTickOnSlope = true;
            moveDirection = slopeDirection * -Input.GetAxisRaw("Horizontal") * 10;
        }
        else if (groundedOnLeft && !groundedOnRight && !jumping)
        {
            slopeDirection = new Vector3(-leftHit.normal.y, leftHit.normal.x, 0f) / Mathf.Sqrt((leftHit.normal.x * leftHit.normal.x) + (leftHit.normal.y * leftHit.normal.y));
            previousTickOnSlope = true;
            moveDirection = slopeDirection * -Input.GetAxisRaw("Horizontal") * 10;
        }
        else
        {
            previousTickOnSlope = false;
            moveDirection.x = Input.GetAxisRaw("Horizontal") * 10;
        }

        if (Input.GetButtonDown("Jump") && (groundedOnLeft || groundedOnRight))
        {
            //OnSlope = false;
            moveDirection.y = 12;
            moveDirection.x = Input.GetAxisRaw("Horizontal") * 10;
            //MomentumOnJump = thisRigidbody.velocity.x; //Using real speed instead of calculated one in case we are jumping from against a wall
            jumping = true;
            Debug.Log("Jumped");
        }

        if (moveDirection.y <= 0 && jumping)
            jumping = false;

        if ((!groundedOnLeft && groundedOnRight) || (groundedOnLeft && !groundedOnRight))
        {
            Debug.Log("One Grounded");
            previousTickOneSideGrounded = true;
        }
        else
            previousTickOneSideGrounded = false;

        thisRigidBody.velocity = moveDirection;
    }

         /*   if (OnSlope) //If the character is on a slope, then its direction should be deviated accordingly
        {
            Debug.Log(transform.name + " IS ON SLOOOOPE");





    SlopeAngle = Vector3.Angle(-transform.right, slopeDirection);

            if (hit.normal.x > 0)
                mirrorSlope = false;
            else
                mirrorSlope = true;

            previousFrameOnSlope = true;
        }*/

}
