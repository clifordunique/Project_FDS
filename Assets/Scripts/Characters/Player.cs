using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Characters {

    bool jump = false;

	// Update is called once per frame
	void Update ()
    {
        jump = Input.GetButtonDown("Jump");
        Move(Input.GetAxis("Horizontal"), jump);
	}
}
