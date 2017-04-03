using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour {

    public Transform Target;
    public float distanceFromTarget;
    public float lateralDistanceFromTarget = 3f;
    public float GlobalDamp = .1f;
    public float MarginDamp = 5f;

    bool goingLeft = false;

    float margin = 0f;

    Vector3 targetPosition;

	// Use this for initialization
	void Start ()
    {
        MoveToTargetPoint(false);
	}

    void MoveToTargetPoint(bool Smooth)
    {
        if (Input.GetAxisRaw("Horizontal") > 0)
            goingLeft = true;
        else if (Input.GetAxisRaw("Horizontal") < 0)
            goingLeft = false;

        if (goingLeft)
            lateralDistanceFromTarget = Mathf.Lerp(lateralDistanceFromTarget, 3, MarginDamp);
        else
            lateralDistanceFromTarget = Mathf.Lerp(lateralDistanceFromTarget, -3, MarginDamp);

        targetPosition = Target.position;
        targetPosition.x += lateralDistanceFromTarget;
        targetPosition.y += 1f;
        targetPosition.z -= distanceFromTarget;

        if (Smooth)
        {
            transform.position = Vector3.Slerp(transform.position, targetPosition, GlobalDamp);   
        }
        else
        {
            transform.position = targetPosition;
        }
    }

	// Update is called once per frame
	void LateUpdate ()
    {
        MoveToTargetPoint(true);
    }
}
