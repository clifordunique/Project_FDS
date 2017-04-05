using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    bool jump = false;
    CollisionTests collisionTests;

    private void Start()
    {
        collisionTests = gameObject.GetComponent<CollisionTests>();
    }

    // TODO : Try to find the best between Update and FixedUpdate
	void FixedUpdate ()
    {
        jump = Input.GetButtonDown("Jump");
        Move(Input.GetAxisRaw("Horizontal"), jump);

        //collisionTests.ClearTests();
	}
}
