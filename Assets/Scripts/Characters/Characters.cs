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
    public Collider thisCollider;
    #endregion

    #region external components
    CharactersSharedVariables sharedVariables;
    #endregion

    // Use this for initialization
    void Awake ()
    {
        sharedVariables = GameObject.Find ("CharactersManager").GetComponent<CharactersSharedVariables>();
        thisCollider = this.gameObject.GetComponent<Collider>();

        if (sharedVariables == null)
            Debug.LogError ("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");
	}

    bool TouchingWallOnLeft()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxLeftSideCount >= 4)
            return true;
        else
            return false;
    }

    bool TouchingWallOnRight()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxRightSideCount >= 4)
            return true;
        else
            return false;
    }

    bool CheckIfGrounded()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxDownSideCount >= 4)
        {
            //Debug.Break();
            return true;
        }
        else
            return false;
    }

    public void Move (float HorizontalDirection)
    {
        Move(HorizontalDirection, 0, false);
    } 

    public void Move(float HorizontalDirection, bool jump)
    {
        Move(HorizontalDirection, 0, jump);
    }

    //Main Move Method
    public void Move (float HorizontalDirection, float VerticalDirection, bool jump)
    {
        if (CheckIfGrounded())
        {
            //Debug displayed when grounded
            Debug.DrawRay(transform.position, -transform.up * thisCollider.bounds.size.y, Color.red);

            moveDirection = new Vector3(HorizontalDirection, VerticalDirection);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;

            if (jump)
            {
                //Debug.Break();
                moveDirection.y = jumpStrength;
                MomentumOnJump = thisCollider.GetComponent<Rigidbody>().velocity.x; //Using real speed instead of calculated one in case we are jumping from against a wall
            }
        }
        else
        {
            AirControl(HorizontalDirection);
        }


        moveDirection.y -= 25 * Time.deltaTime; //TODO: Replace 25 by sharedVariables gravity var, which is fucked up for some reasons

        //Cancelling directions if Pauline is pushing solid walls (Avoid glitches with jumps)
        if ( (TouchingWallOnLeft() && moveDirection.x < 0) 
            || (TouchingWallOnRight() && moveDirection.x > 0))
            moveDirection.x = 0;

        thisCollider.GetComponent<Rigidbody>().velocity = moveDirection;
    }

    void AirControl (float MomentumInfluenceBaseRate)
    {
        if (MomentumInfluenceBaseRate != 0) //This check is to prevent the character to just stop the momentum in middle-air
            MomentumOnJump = Mathf.Lerp(MomentumOnJump, MomentumInfluenceBaseRate * speed, jumpMomentumInfluenceRate);

        moveDirection.x = MomentumOnJump;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -speed, speed);
    }

}
