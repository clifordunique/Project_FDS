using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionTests : MonoBehaviour {

    Collider collider;
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

    private void OnCollisionStay(Collision collision)
    {
        ////Debug.Log("Collision happened");

        LeftSideCount = 0;
        RightSideCount = 0;
        UpSideCount = 0;
        DownSideCount = 0;

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawLine(transform.position, contact.point, Color.blue, 5f);
            Debug.DrawRay(contact.point, contact.normal * 2f, Color.red, 5f);

            //Smoothing out contact point floating point

            if (contact.point.y >= collider.bounds.max.y)
            {
                UpSideCount++;
                //Debug.Log("One Contact point is touching Up side");
            }

            if (contact.point.y <= collider.bounds.min.y + treshold)
            {
                //TODO: For some reasons, even if there's no more down point of collision, the count stays up.
                DownSideCount++;
                Debug.Log("One Contact point is touching down side with contact point used =" + contact.point.y + " for bound " + collider.bounds.min.y + treshold);
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

        Debug.Log("Up = " + UpSideCount + " Down = " + DownSideCount + " Left = " + LeftSideCount + " Right = " + RightSideCount);
    }

    /*
    private void OnCollisionExit(Collision collision)
    {

        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawLine(transform.position, contact.point, Color.blue, 5f);

            if (contact.point.y >= collider.bounds.max.y)
            {
                UpSideCount--;
                //Debug.Log("One Contact point has stopped touching Up side");
            }

            if (contact.point.y >= collider.bounds.min.y)
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

            //Debug.Log("Up = " + UpSideCount + " Down = " + DownSideCount + " Left = " + LeftSideCount + " Right = " + RightSideCount);
        }
    }
    */
}
