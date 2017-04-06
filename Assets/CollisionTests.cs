using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Used for making lists of all contact points during a physic time tick.
//This'll be used to get the higher number for each side, which is the most accurate number
public class ContactPointCounts
{
    public int LeftSideCount = 0;
    public int RightSideCount = 0;
    public int UpSideCount = 0;
    public int DownSideCount = 0;

    public ContactPointCounts (int up, int down, int left, int right)
    {
        LeftSideCount = left;
        RightSideCount = right;
        UpSideCount = up;
        DownSideCount = down;
    }
}

public class CollisionTests : MonoBehaviour {

    Collider thisCollider;
    List <Collider> collidedObjects = new List<Collider>();
    List<ContactPointCounts> contactPointCounts = new List<ContactPointCounts>();
    Dictionary<GameObject, ContactPointCounts> uniqueCollisions = new Dictionary<GameObject, ContactPointCounts>();
    public float colliderSkinWidth = .1f;

    public int MaxLeftSideCount = 0;
    public int MaxRightSideCount = 0;
    public int MaxUpSideCount = 0;
    public int MaxDownSideCount = 0;

    // Use this for initialization
    void Start ()
    {
        thisCollider = this.gameObject.GetComponent<Collider>();	
	}

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("Collision Happening");

        int LeftSideCount = 0;
        int RightSideCount = 0;
        int UpSideCount = 0;
        int DownSideCount = 0;

    collidedObjects.Add(collision.collider);

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawLine(transform.position, contact.point, Color.white);
            //Debug.DrawRay(contact.point, contact.normal * 2f, Color.red);

            if (contact.point.y >= thisCollider.bounds.max.y - colliderSkinWidth)
            {
                UpSideCount++;
            }

            if (contact.point.y <= thisCollider.bounds.min.y + colliderSkinWidth)
            {
                DownSideCount++;
            }

            if (contact.point.x <= thisCollider.bounds.min.x + colliderSkinWidth)
            {
                LeftSideCount++;
            }

            if (contact.point.x >= thisCollider.bounds.max.x - colliderSkinWidth)
            {
                RightSideCount++;
            }
        }

        if (!uniqueCollisions.ContainsKey (collision.gameObject))
        {
            uniqueCollisions.Add(collision.gameObject, new ContactPointCounts(UpSideCount, DownSideCount, LeftSideCount, RightSideCount));
        }
    }

    public void GetRealContactPointsCount ()
    {
        MaxUpSideCount = 0;
        MaxDownSideCount = 0;
        MaxLeftSideCount = 0;
        MaxRightSideCount = 0;

        int iterator = 0;
        foreach (KeyValuePair<GameObject,ContactPointCounts> collision in uniqueCollisions)
        {
            if (collision.Value.UpSideCount > MaxUpSideCount)
            {
                MaxUpSideCount = collision.Value.UpSideCount;
            }

            if (collision.Value.DownSideCount > MaxDownSideCount)
            {
                MaxDownSideCount = collision.Value.DownSideCount;
            }

            if (collision.Value.LeftSideCount > MaxLeftSideCount)
            {
                MaxLeftSideCount = collision.Value.LeftSideCount;
            }

            if (collision.Value.RightSideCount > MaxRightSideCount)
            {
                MaxRightSideCount = collision.Value.RightSideCount;
            }

            iterator++;
        }

        //DEBUG
        /*Debug.Log("Previous physic time tick collider count = " + uniqueCollisions.Count);

        int debugIterator = 0;
        foreach (KeyValuePair<GameObject, ContactPointCounts> collision in uniqueCollisions)
        {
            Debug.Log ("Collision " + debugIterator + " : Up " + collision.Value.UpSideCount + " : Down " + collision.Value.DownSideCount + " : Left " + collision.Value.LeftSideCount + " : Right " + collision.Value.RightSideCount);

            iterator++;
        }*/

        //Debug.Log("Up = " + MaxUpSideCount + " Down = " + MaxDownSideCount + " Left = " + MaxLeftSideCount + " Right = " + MaxRightSideCount);

    }

    //If the collision is over, let's reset the maxCounts
    private void OnCollisionExit(Collision collision)
    {
        //Debug.Log("Exited collider = " + collision.collider);

        if (uniqueCollisions.ContainsKey(collision.gameObject))
        {
            uniqueCollisions.Remove (collision.gameObject);
        }
    }
}
