using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour {

    public Transform Target;
    public float distanceFromTarget;
    public float lateralDistanceFromTarget = 3f;
    public float GlobalDamp = .1f;
    public float MinMarginDamp = .05f;
    public float MaxMarginDamp = .1f;

    float MarginDamp = 0f;
    float MaxSpeedMomentumPercent = 0f;
    public float MomentumRate = .5f;
    public float MarginDampReactionTime = .5f; 

    bool goingLeft = false;

    Vector3 targetPosition;

    GameObject player;
    Player playerScript;

	// Use this for initialization
	void Start ()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerScript = player.GetComponent<Player>();
        MoveToTargetPoint(false);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        MoveToTargetPoint(true);
    }

    void MoveToTargetPoint(bool Smooth)
    {
        if (Smooth)
        {
            SmoothCameraMovement();
        }

        SetTargetPosition();

        if (Smooth)
        {
            transform.position = Vector3.Slerp(transform.position, targetPosition, GlobalDamp);   
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    void SetTargetPosition ()
    {
        targetPosition = Target.position;
        targetPosition.x += lateralDistanceFromTarget;
        targetPosition.y += 1f;
        targetPosition.z -= distanceFromTarget;
    }

    void SmoothCameraMovement()
    {
        //Tracking which direction the player is looking in
        if (Input.GetAxisRaw("Horizontal") > 0)
        {
            goingLeft = true;
        }
        else if (Input.GetAxisRaw("Horizontal") < 0)
        {
            goingLeft = false;
        }

        //Tracking the current velocity of the player. If it's at more than 90% of its maximum speed, we'll trigger the maximum horizontal speed for the Camera horizontal aligment.
        if (Mathf.Abs(player.GetComponent<Rigidbody>().velocity.x) >= playerScript.speed * .9f)
        {
            MaxSpeedMomentumPercent += Time.deltaTime * MomentumRate;
        }
        else
        {
            MaxSpeedMomentumPercent -= Time.deltaTime * MomentumRate;
        }

        MaxSpeedMomentumPercent = Mathf.Clamp(MaxSpeedMomentumPercent, 0f, 1f);

        if (MaxSpeedMomentumPercent >= .5f) //Depending on the player's total momentum, the horizontal alignment of the camera won't go as fast
        {
            MarginDamp = MaxMarginDamp;
            //Debug.Log("MaxDamp used");
        }
        else if (MaxSpeedMomentumPercent <= 0f)
        {
            MarginDamp = MinMarginDamp;
            //Debug.Log("MinDamp used");
        }

        if (goingLeft) //Now applying all previous calculations, depending on the direction
            lateralDistanceFromTarget = Mathf.Lerp(lateralDistanceFromTarget, 3, MarginDamp);
        else
            lateralDistanceFromTarget = Mathf.Lerp(lateralDistanceFromTarget, -3, MarginDamp);
    }


}
