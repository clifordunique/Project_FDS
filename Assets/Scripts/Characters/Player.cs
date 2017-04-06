using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    bool jump = false;
    CollisionTests collisionTests;

    private void Start()
    {
        //QualitySettings.vSyncCount = 0;
        collisionTests = gameObject.GetComponent<CollisionTests>();
    }

    void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();
        jump = Input.GetButtonDown("Jump");
        Move(Input.GetAxisRaw("Horizontal"), jump);
	}

}
