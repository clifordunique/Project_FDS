using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    Collider collider;
    List <Collider> collidedObjects = new List<Collider>();
    List<ContactPointCounts> contactPointCounts = new List<ContactPointCounts>();
    public float treshold = .1f;

	// Use this for initialization
	void Start ()
    {
        collider = this.gameObject.GetComponent<Collider>();	
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public int LeftSideCount = 0;
    public int RightSideCount = 0;
    public int UpSideCount = 0;
    public int DownSideCount = 0;

    public int MaxLeftSideCount = 0;
    public int MaxRightSideCount = 0;
    public int MaxUpSideCount = 0;
    public int MaxDownSideCount = 0;

    void FixedUpdate ()
    {
        Debug.Log("Colliders hit in previous physic time tick = " + collidedObjects.Count);
        Debug.Log("ContactPointCounts = " + contactPointCounts.Count);

        collidedObjects.Clear();
        contactPointCounts.Clear();
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log("Collision happened");

        LeftSideCount = 0;
        RightSideCount = 0;
        UpSideCount = 0;
        DownSideCount = 0;

        collidedObjects.Add(collision.collider);


        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawLine(transform.position, contact.point, Color.blue);
            Debug.DrawRay(contact.point, contact.normal * 2f, Color.red);

            //Smoothing out contact point floating point

            Debug.Log("Dot Product for this contact point is = " + Vector3.Dot (contact.normal, Vector3.up));

            if (contact.point.y >= collider.bounds.max.y)
            {
                UpSideCount++;
                //Debug.Log("One Contact point is touching Up side");
            }

            if (contact.point.y <= collider.bounds.min.y + treshold)
            {
                //TODO: For some reasons, even if there's no more down point of collision, the count stays up.
                DownSideCount++;
                //Debug.Log("One Contact point is touching down side with contact point used =" + contact.point.y + " for bound " + collider.bounds.min.y + treshold);
            }

            if (contact.point.x <= gameObject.GetComponent<Collider>().bounds.min.x)
            {
                LeftSideCount++;
                //Debug.Log("One Contact point is touching left side");
            }

            if (contact.point.x >= gameObject.GetComponent<Collider>().bounds.max.x)
            {
                RightSideCount++;
                //Debug.Log("One Contact point is touching right side");
            }
        }


        if (collidedObjects.Count > contactPointCounts.Count)
        {
            contactPointCounts.Add(new ContactPointCounts(UpSideCount, DownSideCount, LeftSideCount, RightSideCount));
        }

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

        Debug.Log("Up = " + MaxUpSideCount + " Down = " + MaxDownSideCount + " Left = " + MaxLeftSideCount + " Right = " + MaxRightSideCount);
    }

    private void OnCollisionExit()
    {
        MaxLeftSideCount = 0;
        MaxRightSideCount = 0;
        MaxUpSideCount = 0;
        MaxDownSideCount = 0;
    }

    /*
    private void OnCollisionExit(Collision collision)
    {

        Debug.Log("Decollided");

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawLine(transform.position, contact.point, Color.blue, 5f);

            if (contact.point.y >= collider.bounds.max.y)
            {
                UpSideCount--;
                //Debug.Log("One Contact point has stopped touching Up side");
            }

            if (contact.point.y <= collider.bounds.min.y + treshold)
            {
                DownSideCount--;
                //Debug.Log("One Contact point has stopped touching down side");
            }

            if (contact.point.x <= gameObject.GetComponent<Collider>().bounds.min.x)
            {
                LeftSideCount--;
                //Debug.Log("One Contact point has stopped touching left side");
            }

            if (contact.point.x >= gameObject.GetComponent<Collider>().bounds.max.x)
            {
                RightSideCount--;
                //Debug.Log("One Contact point has stopped touching right side");
            }


        }

        Debug.Log("Up = " + UpSideCount + " Down = " + DownSideCount + " Left = " + LeftSideCount + " Right = " + RightSideCount);
    }
    */
}
