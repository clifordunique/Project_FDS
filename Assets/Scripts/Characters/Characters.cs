using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Characters : MonoBehaviour {

    #region Inspector Variables
    [SerializeField]
    float speed = 10;
    [SerializeField]
    float jumpMomentumInfluenceRate;
    [SerializeField]
    float jumpStrength = 12;
    [SerializeField]
    float jumpSpeed = 50;
    #endregion

    #region Moves Vars
    Vector3 moveDirection;
    #endregion

    #region Internal Components
    CharacterController controller;
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
            }
        }
        else
        {
            AirControl(); //TODO : Write the air control. We have to keep a max momentum that matches Pauline max speed on foot (Maybe a little more)
            //We have to influence on the momentum when hitting the directional keys in mid-air
        }

        moveDirection.y -= sharedVariables.Gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    void AirControl ()
    {

    }

}
