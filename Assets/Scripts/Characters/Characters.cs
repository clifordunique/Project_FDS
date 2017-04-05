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
    [HideInInspector]
    public Collider collider;
    #endregion

    #region external components
    CharactersSharedVariables sharedVariables;
    #endregion

    // Use this for initialization
    void Start ()
    {
        sharedVariables = GameObject.FindObjectOfType <CharactersSharedVariables>();
        controller = gameObject.GetComponent<CharacterController>();
        collider = gameObject.GetComponent<Collider>();

        if (sharedVariables == null)
            Debug.LogError ("The scene is missing the CharacterSharedVariables class, please check your current GameObjects");
	}

    bool CheckIfAgainstWall()
    {
        //TODO: Working only for the right side for now
        RaycastHit hit;

        Vector3 p1 = collider.bounds.center;
        Vector3 hitPoint = Vector3.zero;

        //Debug.DrawLine(p1, collider.bounds.min - (transform.up * groundTreshold), Color.gr);

        if (Physics.SphereCast(p1, (collider.bounds.size.y / 2) + .1f, transform.right, out hit, Mathf.Infinity))
        {
            hitPoint = hit.point;
            Debug.DrawLine(p1, hit.point, Color.green);
            if (hitPoint.x >= collider.bounds.max.x - groundTreshold && hitPoint.x <= collider.bounds.max.x)
            {
                //Debug.Log("I'M GROUNDED MOTHERFUCKER");
                return true;
            }
            else
            {
                //Debug.Log("I'M NOT GROUNDED MOTHERFUCKER");
                return false;
            }
        }
        else
        {
            //Debug.Log("I'M NOT GROUNDED MOTHERFUCKER");
            return false;
        }
    }

    bool CheckIfGrounded()
    {
        //Debug
        return false;

        RaycastHit hit;

        Vector3 p1 = collider.bounds.center;
        Vector3 hitPoint = Vector3.zero;

        Debug.DrawLine(p1, collider.bounds.min - (transform.up * groundTreshold), Color.red);

        // Cast a sphere wrapping character controller 10 meters downward
        // to see if it is about to hit anything.
        if (Physics.SphereCast(p1, (collider.bounds.size.x / 2) + .1f,  -transform.up, out hit, Mathf.Infinity))
        {
            hitPoint = hit.point;
            Debug.DrawLine(p1, hit.point, Color.green);
            if (hitPoint.y >= collider.bounds.min.y - groundTreshold && hitPoint.y <= collider.bounds.min.y)
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


        if (gameObject.GetComponent<CollisionTests>().MaxDownSideCount >= 4)
        {
            //Debug.Break();
            Debug.Log("Grounded");

            //Debug displayed when grounded
            Debug.DrawRay(transform.position, -transform.up * collider.bounds.size.y, Color.red);
            //Debug.Break(); 

            moveDirection = new Vector3(HorizontalDirection, VerticalDirection);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;

            if (jump)
            {
                moveDirection.y = jumpStrength;
                MomentumOnJump = collider.GetComponent<Rigidbody>().velocity.x; //Using real speed instead of calculated one in case we are jumping from against a wall
            }

            //gameObject.GetComponent<CollisionTests>().DownSideCount = 0; //TODO: Could work but... We actually can't jump with this line... Grounded is ok, but not jump.
        }
        else
        {
            AirControl(HorizontalDirection);

        }


        moveDirection.y -= sharedVariables.Gravity * Time.deltaTime;
        collider.GetComponent<Rigidbody>().velocity = moveDirection;
        //controller.Move(moveDirection * Time.deltaTime);

        //Debug.Log("Velocity X of " + gameObject.name + " is " + controller.velocity.x);
        //Debug.Log("Against wall = " + CheckIfAgainstWall());
    }

    void AirControl (float MomentumInfluenceBaseRate)
    {
        if (MomentumInfluenceBaseRate != 0) //This check is to prevent the character to just stop the momentum in middle-air
            MomentumOnJump = Mathf.Lerp(MomentumOnJump, MomentumInfluenceBaseRate * speed, jumpMomentumInfluenceRate);

        moveDirection.x = MomentumOnJump;
        moveDirection.x = Mathf.Clamp(moveDirection.x, -speed, speed);
    }

}
