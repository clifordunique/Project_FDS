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
    #endregion

    #region Moves Vars
    Vector3 moveDirection;
    float MomentumOnJump;
    #endregion

    #region Internal Components
    [HideInInspector]
    public CharacterController controller;
    #endregion

    private float airControlDir = 0;

    #region external components
    CharactersSharedVariables sharedVariables;
    #endregion

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

        if (controller.isGrounded)
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
