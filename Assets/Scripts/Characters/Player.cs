using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    [SerializeField]
    float dashDuration = 1f;
    [SerializeField]
    float dashSpeed = 10f;
    float dashTimer = 0f;
    bool jump = false;
    bool dashing = false;
    CollisionTests collisionTests;
    Vector3 dashDirection;
    SpriteRenderer thisSprite;
    Rigidbody thisRigidbody;

    private void Start()
    {
        collisionTests = gameObject.GetComponent<CollisionTests>();
        thisSprite = gameObject.GetComponent<SpriteRenderer>();
        thisRigidbody = gameObject.GetComponent<Rigidbody>();
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

            Debug.Log("Dashin'");
            if (thisSprite.flipX)
                dashDirection = -transform.right;
            else
                dashDirection = transform.right;

            dashTimer += Time.deltaTime;
            thisRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            //CheckStep();
        }
        else
        {
            dashing = false;
            thisRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            dashTimer = 0f;
        }

        thisCollider.GetComponent<Rigidbody>().velocity = dashDirection * dashSpeed;
    }

}
