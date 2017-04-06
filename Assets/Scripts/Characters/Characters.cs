using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Characters : MonoBehaviour {

    //TODO : Replace all the GetComponents into a single one for optimization


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
    [SerializeField]
    float minimumWallSize = .1f;
    #endregion

    #region Moves Vars
    Vector3 moveDirection;
    float MomentumOnJump;
    private float airControlDir = 0;
    float previousTickHorizontalVelocity = 0f;
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

    bool TouchingHead()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxUpSideCount >= 4)
            return true;
        else
            return false;
    }

    bool TouchingWallOnLeft()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxLeftSideCount >= 4 && gameObject.GetComponent<CollisionTests>().yHighestDiff >= minimumWallSize)     
            //TODO: Replace minimumWallSize with a percentage of Pauline's collider height for better control
            return true;
        else
            return false;
    }

    bool TouchingWallOnRight()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxRightSideCount >= 4 && gameObject.GetComponent<CollisionTests>().yHighestDiff >= minimumWallSize)
            //TODO: Replace minimumWallSize with a percentage of Pauline's collider height for better control
            return true;
        else
            return false;
    }

    bool CheckIfGrounded()
    {
        if (gameObject.GetComponent<CollisionTests>().MaxDownSideCount >= 4 && gameObject.GetComponent<CollisionTests>().xHighestDiff >= .01f)
        //TODO: Replace the .01f to a percentage of Pauline's collider width, just in case we modify the collider's width and this gets broken
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
            //Debug.DrawRay(transform.position, -transform.up * thisCollider.bounds.size.y, Color.red);

            moveDirection = new Vector3(HorizontalDirection, VerticalDirection);
            moveDirection *= speed;

            if (jump)
            {
                moveDirection.y = jumpStrength;
                MomentumOnJump = thisCollider.GetComponent<Rigidbody>().velocity.x; //Using real speed instead of calculated one in case we are jumping from against a wall
            }
        }
        else
        {
            if (TouchingHead())
                moveDirection.y = -sharedVariables.Gravity * Time.deltaTime;

            AirControl(HorizontalDirection);
            ApplyGravity();
        }

        //Cancelling directions if Pauline is pushing solid walls (Avoid glitches with jumps)
        if ( (TouchingWallOnLeft() && moveDirection.x < 0) 
            || (TouchingWallOnRight() && moveDirection.x > 0))
            moveDirection.x = 0;

        thisCollider.GetComponent<Rigidbody>().velocity = moveDirection;
        //Debug.Log("Applied gravity is = " + thisCollider.GetComponent<Rigidbody>().velocity.y);
        previousTickHorizontalVelocity = thisCollider.GetComponent<Rigidbody>().velocity.x;
        //Debug.Log(previousTickHorizontalVelocity);
    }

    void ApplyGravity ()
    {
        /*if ( (gameObject.GetComponent<CollisionTests>().MaxDownSideCount < 4 && gameObject.GetComponent<CollisionTests>().MaxLeftSideCount < 2)
            || (gameObject.GetComponent<CollisionTests>().MaxDownSideCount < 4 && gameObject.GetComponent<CollisionTests>().MaxRightSideCount < 2))*/
            moveDirection.y -= sharedVariables.Gravity * Time.deltaTime;
    }

    void AirControl (float MomentumInfluenceBaseRate)
    {
        if (MomentumInfluenceBaseRate != 0) //This check is to prevent the character to just stop the momentum in middle-air
            MomentumOnJump = Mathf.Lerp(previousTickHorizontalVelocity, MomentumInfluenceBaseRate * speed, jumpMomentumInfluenceRate);

        moveDirection.x = MomentumOnJump;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -speed, speed);
    }

}
