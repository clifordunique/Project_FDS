using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    bool jump = false;

	// Update is called once per frame
	void FixedUpdate ()
    {
        jump = Input.GetButtonDown("Jump");
        Move(Input.GetAxisRaw("Horizontal"), jump);
	}
}
