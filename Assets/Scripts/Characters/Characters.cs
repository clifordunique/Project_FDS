using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Characters : MonoBehaviour {

    #region Inspector Variables
    public float speed = 10;
    [SerializeField]
    float jumpMomentumInfluenceRate;
    [SerializeField]
    float jumpStrength = 12;
    [SerializeField]
    float jumpSpeed = 50;
    [SerializeField]
    float groundTreshold = .1f;
    #endregion

    #region Moves Vars
    Vector3 moveDirection;
    float MomentumOnJump;
    private float airControlDir = 0;
    #endregion

    #region Internal Components
    [HideInInspector]
    public CharacterController controller;
    #endregion

    #region external components
    CharactersSharedVariables sharedVariables;
    #endregion

    bool CheckIfGrounded ()
    {
        RaycastHit hit;

        Vector3 p1 = transform.position + controller.center;
        float distanceToObstacle = 0;

        Debug.DrawRay(p1, -transform.up * groundTreshold, Color.red);

        // Cast a sphere wrapping character controller 10 meters downward
        // to see if it is about to hit anything.
        if (Physics.SphereCast(p1, controller.radius, -transform.up, out hit, Mathf.Infinity))
        { 
            distanceToObstacle = hit.distance;

            if (distanceToObstacle <= groundTreshold)
            {
                Debug.Log("I'M GROUNDED MOTHERFUCKER");
                return true;

            }
            else
            {
                Debug.Log("I'M NOT GROUNDED MOTHERFUCKER");
                return false;
            }
        }
        else
        {
            Debug.Log("I'M NOT GROUNDED MOTHERFUCKER");
            return false;
        }
    }

    // Use this for initialization
    void Start ()
    {
        sharedVariables = GameObject.FindObjectOfType <CharactersSharedVariables>();
        controller = gameObject.GetComponent<CharacterController>();

        if (sharedVariables == null)
            Debug.LogError ("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");
	}

    public void Move (float HorizontalDirection)
    {
        Move(HorizontalDirection, 0, false);
    } 

    public void Move(float HorizontalDirection, bool jump)
    {
        Move(HorizontalDirection, 0, jump);
    }

    public void Move (float HorizontalDirection, float VerticalDirection, bool jump)
    {

        if (CheckIfGrounded())
        {
            moveDirection = new Vector3(HorizontalDirection, VerticalDirection);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;

            if (jump)
            {
                moveDirection.y = jumpStrength;
                MomentumOnJump = controller.velocity.x; //Using real speed instead of calculated one in case we are jumping from against a wall
            }
        }
        else
        {
            AirControl(HorizontalDirection);
        }

        moveDirection.y -= sharedVariables.Gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);

        //Debug.Log("Velocity X of " + gameObject.name + " is " + controller.velocity.x);
    }

    void AirControl (float MomentumInfluenceBaseRate)
    {
        if (MomentumInfluenceBaseRate != 0) //This check is to prevent the character to just stop the momentum in middle-air
            MomentumOnJump = Mathf.Lerp(MomentumOnJump, MomentumInfluenceBaseRate * speed, jumpMomentumInfluenceRate);

        moveDirection.x = MomentumOnJump;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -speed, speed);
    }

}
