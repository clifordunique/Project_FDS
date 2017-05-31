using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondGroundDetectionTest : MonoBehaviour {

    Collider thisCollider;
    Rigidbody thisRigidBody;

    Vector3 moveDirection = Vector3.zero;

    bool isGrounded = false;

    public LayerMask ground;

    float distToGround;
    float gravityForce = 25f;

    // Use this for initialization
    void Start ()
    {
        thisRigidBody = gameObject.GetComponent<Rigidbody>();
        thisCollider = gameObject.GetComponent<Collider>();
        distToGround = thisCollider.bounds.extents.y;
    }

    void newFixedUpdate()
    {
        RaycastHit hit;
        RaycastHit hitLeft;
        RaycastHit hitRight;

        Physics.Raycast(thisCollider.bounds.center, -Vector3.up, out hit, Mathf.Infinity, ground);
        Physics.Raycast(new Vector3(thisCollider.bounds.min.x, thisCollider.bounds.min.y, thisCollider.bounds.center.z), -Vector3.up, out hitLeft, Mathf.Infinity, ground);
        Physics.Raycast(new Vector3(thisCollider.bounds.max.x, thisCollider.bounds.min.y, thisCollider.bounds.center.z), -Vector3.up, out hitRight, Mathf.Infinity, ground);

        RaycastHit[] groundTests = new RaycastHit[2];

        bool leftFirst = true;
        //groundTests[0] = hit;
        groundTests[0] = hitLeft;
        groundTests[1] = hitRight;

        //Debug.DrawLine(thisCollider.bounds.center, hit.point, Color.red);



        /*if(hitRight.collider != null)
            Debug.DrawLine(new Vector3(thisCollider.bounds.max.x, thisCollider.bounds.center.y, thisCollider.bounds.center.z), hitRight.point, Color.red);*/
        //IF you detect one of the cast is not touching ground... Maybe try to extend it during just one tick to check if there's a slope just below ?

        if (Input.GetAxisRaw("Horizontal") > 0)
        {
            //groundTests[0] = hit;
            groundTests[0] = hitRight;
            groundTests[1] = hitLeft;
            leftFirst = false;
        }

        foreach (RaycastHit a_hit in groundTests)
        {
            //Debug.Log("Grounded with distance from ground = " + a_hit.distance + " , using perpendicular");

            float referenceDistance = 0f;

            if (leftFirst && a_hit.distance > hitRight.distance)
            {
                referenceDistance = a_hit.distance;
            }
            else
                referenceDistance = hitRight.distance;

            if (!leftFirst && a_hit.distance > hitLeft.distance)
            {
                referenceDistance = a_hit.distance;
            }
            else
                referenceDistance = hitLeft.distance;

            if (a_hit.collider != null)
            {
                if (hitLeft.collider != null)
                    Debug.DrawLine(thisCollider.bounds.center, a_hit.point, Color.blue);

                //if (a_hit.distance > hitRight.distance)
                if (referenceDistance > 1f)
                {
                    isGrounded = false;
                    moveDirection.y -= gravityForce * Time.deltaTime; //TODO : Prevent from going through ground for a few frames. Calculate next position one tick forehead, if it's through ground, change position to right on ground.
                    break;
                }
                else
                    isGrounded = true;

                if (referenceDistance < 1f)
                {
                    transform.position += Vector3.up * (1 - referenceDistance);
                    Debug.Log("Too Much In Ground");
                }



                Vector3 perpendicularMoveDir;
                perpendicularMoveDir = new Vector3(-a_hit.normal.y, a_hit.normal.x, 0f) / Mathf.Sqrt((a_hit.normal.x * a_hit.normal.x) + (a_hit.normal.y * a_hit.normal.y));

                if (Input.GetAxisRaw("Horizontal") < 0)
                {
                    moveDirection = perpendicularMoveDir * 10;
                    Debug.Log("Going Left");
                }
                else if (Input.GetAxisRaw("Horizontal") > 0)
                {
                    moveDirection = -perpendicularMoveDir * 10;
                    Debug.Log("Going Right");
                }


                Debug.DrawRay(thisCollider.bounds.center, perpendicularMoveDir, Color.green);
                break;
            }
            else
            {
                isGrounded = false;
            }
        }

        if (groundTests[1].collider == null)
        {
            Debug.Log("Edge ray didn't touch anything");

            if(Input.GetAxisRaw("Horizontal") < 0)
            {
                if (Physics.Raycast(new Vector3(thisCollider.bounds.min.x, thisCollider.bounds.center.y, thisCollider.bounds.center.z), -Vector3.up, out hitLeft, Mathf.Infinity, ground))
                {
                    Vector3 perpendicularMoveDir = new Vector3(-hitLeft.normal.y, hitLeft.normal.x, 0f) / Mathf.Sqrt((hitLeft.normal.x * hitLeft.normal.x) + (hitLeft.normal.y * hitLeft.normal.y));

                    Debug.Log("Edge ray touched something with this perpendicular = " + perpendicularMoveDir);

                    if (groundTests[0].collider == null && groundTests[2].collider == null)
                        Debug.Log("Possibly on the edge atop a slope a dope dope");
                }
            }

        }
    }

	// Update is called once per frame
	void FixedUpdate ()
    {
        moveDirection.x = 0;

        if (!isGrounded)
        {

            Debug.Log("AIRBOOOOORNE");
        }
        else
            moveDirection.y = 0;
            
        newFixedUpdate();
        /*RaycastHit leftHit;
        RaycastHit horizontalLeftHit;
        RaycastHit rightHit;

        Ray leftRay = new Ray(new Vector3(thisCollider.bounds.min.x, thisCollider.bounds.max.y, thisCollider.bounds.center.z), -transform.up);
        Ray horizontalLeftRay = new Ray(new Vector3 (thisCollider.bounds.min.x, thisCollider.bounds.max.y, thisCollider.bounds.center.z), -transform.right);
        Ray rightRay = new Ray(new Vector3(thisCollider.bounds.max.x, thisCollider.bounds.max.y, thisCollider.bounds.center.z), -transform.up);

        Vector3 slopeDirection = Vector3.zero;

        if (Physics.Raycast(leftRay, out leftHit, thisCollider.bounds.size.y, ground))
        {
            //moveDirection.y = 0;
            Debug.Log("Grounded on left");
            groundedOnLeft = true;

            if (Physics.Raycast(horizontalLeftRay, out horizontalLeftHit, ground))
            {
                slopeDirection = new Vector3(-leftHit.normal.y, leftHit.normal.x, 0f) / Mathf.Sqrt((leftHit.normal.x * leftHit.normal.x) + (leftHit.normal.y * leftHit.normal.y));
                moveDirection = slopeDirection * -Input.GetAxisRaw("Horizontal") * 10;
            }
            else
                moveDirection.x = Input.GetAxisRaw("Horizontal") * 10;

            //previousTickOnSlope = true;

            //previousTickOneSideGrounded = true;
        }
        else*/
        /*if (!isGrounded)
        {
            Debug.Log("Applying gravity.");
            moveDirection.y -= 25 * Time.deltaTime;
            //groundedOnLeft = false;
        }
        else
            moveDirection.y = 0;*/
        /*

        if (thisCollider.bounds.min.y < leftHit.point.y)
        {
            float diff = Mathf.Abs(leftHit.point.y - thisCollider.bounds.min.y);
            Debug.Log("Getting through ground");
            transform.position = new Vector3(transform.position.x, transform.position.y + diff, transform.position.z);
        }

        //moveDirection.x = Input.GetAxisRaw("Horizontal") * 10;
        */
        transform.position += moveDirection * Time.deltaTime;
    }
}
