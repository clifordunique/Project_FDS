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
    Dictionary<GameObject, ContactPointCounts> uniqueCollisions;
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

    public void FixedUpdate ()
    {
        MaxLeftSideCount = 0;
        MaxRightSideCount = 0;
        MaxUpSideCount = 0;
        MaxDownSideCount = 0;

        collidedObjects.Clear();
        contactPointCounts.Clear();
    }

    private void OnCollisionStay(Collision collision)
    {
        int LeftSideCount = 0;
        int RightSideCount = 0;
        int UpSideCount = 0;
        int DownSideCount = 0;

        collidedObjects.Add(collision.collider);

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawLine(transform.position, contact.point, Color.blue);
            Debug.DrawRay(contact.point, contact.normal * 2f, Color.red);

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

        if (!uniqueCollisions.ContainsKey (collision.collider.gameObject)) //TODO : NOT WORKING BITCH
        {
            uniqueCollisions.Add(gameObject, new ContactPointCounts(UpSideCount, DownSideCount, LeftSideCount, RightSideCount));
        }

        GetRealContactPointsCount();

        Debug.Log("Up = " + MaxUpSideCount + " Down = " + MaxDownSideCount + " Left = " + MaxLeftSideCount + " Right = " + MaxRightSideCount);
        //Debug.Break();
    }

    void GetRealContactPointsCount ()
    {
        int iterator = 0;
        foreach (Collider collidedObject in collidedObjects)
        {
            if (contactPointCounts[iterator].UpSideCount > MaxUpSideCount)
            {
                MaxUpSideCount = contactPointCounts[iterator].UpSideCount;
            }

            if (contactPointCounts[iterator].DownSideCount > MaxDownSideCount)
            {
                MaxDownSideCount = contactPointCounts[iterator].DownSideCount;
            }

            if (contactPointCounts[iterator].LeftSideCount > MaxLeftSideCount)
            {
                MaxLeftSideCount = contactPointCounts[iterator].LeftSideCount;
            }

            if (contactPointCounts[iterator].RightSideCount > MaxRightSideCount)
            {
                MaxRightSideCount = contactPointCounts[iterator].RightSideCount;
            }

            iterator++;
        }
    }

    //If the collision is over, let's reset the maxCounts
    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("Exited collider = " + collision.collider);

        MaxLeftSideCount = 0;
        MaxRightSideCount = 0;
        MaxUpSideCount = 0;
        MaxDownSideCount = 0;
    }
}
