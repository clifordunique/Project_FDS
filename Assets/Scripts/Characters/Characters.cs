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
    [SerializeField]
    float UTurnTiming = 0.5f;
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

    void TouchingSmallStep ()
    {
        transform.position += Vector3.up * gameObject.GetComponent<CollisionTests>().yHighestDiff;

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
            Debug.Log("Grounded");
            return true;
        }
        else
        {
            //Debug.Log ("Not grounded because maxSideCount = " + gameObject.GetComponent<CollisionTests>().MaxDownSideCount " and xHighestDiff)
            return false;
        }
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

        }

        ApplyGravity();

        //Cancelling directions if Pauline is pushing solid walls (Avoid glitches with jumps)
        if ((TouchingWallOnLeft() && moveDirection.x < 0)
            || (TouchingWallOnRight() && moveDirection.x > 0))
            moveDirection.x = 0;

        if (gameObject.GetComponent<CollisionTests>().yHighestDiff < minimumWallSize && gameObject.GetComponent<CollisionTests>().yHighestDiff > .01f)
            TouchingSmallStep();

        Debug.Log("Highest Y Diff = " + gameObject.GetComponent<CollisionTests>().yHighestDiff);

        //Adding some time for Pauline to do a U-turn more naturally
        Brakes();

        //Flipping sprite to face latest direction
        if (previousTickHorizontalVelocity == 0f)
        {
            if (moveDirection.x < 0)
                gameObject.GetComponent<SpriteRenderer>().flipX = true;
            else if (moveDirection.x > 0)
                gameObject.GetComponent<SpriteRenderer>().flipX = false;
        }

        thisCollider.GetComponent<Rigidbody>().velocity = moveDirection;
        //Debug.Log("Applied gravity is = " + thisCollider.GetComponent<Rigidbody>().velocity.y);
        previousTickHorizontalVelocity = thisCollider.GetComponent<Rigidbody>().velocity.x;
        //Debug.Log(previousTickHorizontalVelocity);
    }

    bool braking = false;
    float waitBeforeMove = 0f;
    bool flipped = false;
    List<float> velocityHistory = new List<float>();

    void Brakes()
    {
        velocityHistory.Add(moveDirection.x);
        if(Time.time > .1f)
        {
            //Debug.Log("Velocity from .1 secondes = " + velocityHistory[0]);
            velocityHistory.RemoveAt(0);
        }

        if (!braking)
        {
                if ((velocityHistory[0] > 0 && moveDirection.x < 0)
                    || (velocityHistory[0] < 0 && moveDirection.x > 0))
                {
                    if(CheckIfGrounded())
                        braking = true;
                }
        }
        else
        {
            Debug.DrawRay(transform.position, transform.right * 5f, Color.blue);

            if (!flipped)
            {
                if (gameObject.GetComponent<SpriteRenderer>().flipX)
                    gameObject.GetComponent<SpriteRenderer>().flipX = false;
                else
                    gameObject.GetComponent<SpriteRenderer>().flipX = true;

                flipped = true;
            }

            if (waitBeforeMove > .1f)
            {
                //Debug.Log("BRAKE");

                waitBeforeMove = 0f;
                braking = false;
                flipped = false;
            }
            else
            {
                moveDirection.x = 0;
                waitBeforeMove += Time.deltaTime;
            }
        }
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
