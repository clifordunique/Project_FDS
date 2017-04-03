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

    float margin = 0f;

    Vector3 targetPosition;

    GameObject player;
    Player playerScript;

	// Use this for initialization
	void Start ()
    {
        player = GameObject.Find("Pauline");
        playerScript = player.GetComponent<Player>();
        MoveToTargetPoint(false);
    }

    void MoveToTargetPoint(bool Smooth)
    {
        if (Smooth)
        {
            if (Input.GetAxisRaw("Horizontal") > 0)
            {
                goingLeft = true;
            }
            else if (Input.GetAxisRaw("Horizontal") < 0)
            {
                goingLeft = false;
            }

            if (Mathf.Abs(playerScript.controller.velocity.x) >= playerScript.speed * .9f)
            {
                MaxSpeedMomentumPercent += Time.deltaTime * MomentumRate;
            }
            else
            {
                MaxSpeedMomentumPercent -= Time.deltaTime * MomentumRate;
            }

            MaxSpeedMomentumPercent = Mathf.Clamp(MaxSpeedMomentumPercent, 0f, 1f);

            if (MaxSpeedMomentumPercent >= .5f) //Depending on the player's total momentum, the alignment of the camera won't go as fast
            {
                MarginDamp = MaxMarginDamp;
                Debug.Log("MaxDamp used");
            }
            else if (MaxSpeedMomentumPercent <= 0f)
            {
                MarginDamp = MinMarginDamp;
                Debug.Log("MinDamp used");
            }



            if (goingLeft)
                lateralDistanceFromTarget = Mathf.Lerp(lateralDistanceFromTarget, 3, MarginDamp);
            else
                lateralDistanceFromTarget = Mathf.Lerp(lateralDistanceFromTarget, -3, MarginDamp);
        }

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
