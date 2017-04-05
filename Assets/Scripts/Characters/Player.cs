using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    bool jump = false;

	// TODO : Try to find the best between Update and FixedUpdate
	void Update ()
    {
        jump = Input.GetButtonDown("Jump");
        Move(Input.GetAxisRaw("Horizontal"), jump);
	}
}
