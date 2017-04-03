using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour {

    public Transform Target;
    public float distanceFromTarget;

    Vector3 targetPosition;

	// Use this for initialization
	void Start ()
    {
        MoveToTargetPoint();
	}

    void MoveToTargetPoint()
    {
        targetPosition = Target.position;
        targetPosition.z -= distanceFromTarget;
        transform.position = targetPosition;
    }

	// Update is called once per frame
	void LateUpdate ()
    {
        MoveToTargetPoint();
    }
}
