using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Used for making lists of all contact points during a physic time tick.
//This'll be used to get the higher number for each side, which is the most accurate number of collision point that happened for each of the boundary box in a single physX tick.
public class ContactPointCounts
{
    public List <ContactPoint> LeftSideCount = new List<ContactPoint>();
    public List <ContactPoint> RightSideCount = new List<ContactPoint>();
    public List <ContactPoint> UpSideCount = new List<ContactPoint>();
    public List <ContactPoint> DownSideCount = new List<ContactPoint>();
    public List<ContactPoint> LowerLeftSideCount = new List<ContactPoint>();
    public List<ContactPoint> LowerRightSideCount = new List<ContactPoint>();

    public float _highestX = 0f;
    public float _lowestX = 0f;
    public float _highestY = 0f;
    public float _lowestY = 0f;
    public Vector2 _lowerLeft = Vector2.zero;
    public Vector2 _lowerRight = Vector2.zero;

    public ContactPointCounts (List<ContactPoint> up, List<ContactPoint> down, List<ContactPoint> left, List<ContactPoint> right, float highestX, float lowestX, float highestY, float lowestY, Vector2 lowestGroundX, Vector2 highestGroundX)
    {
        LeftSideCount = left;
        RightSideCount = right;
        UpSideCount = up;
        DownSideCount = down;

        _highestX = highestX;
        _highestY = highestY;
        _lowestX = lowestX;
        _lowestY = lowestY;
        _lowerLeft = lowestGroundX;
        _lowerRight = highestGroundX;
    }
}

public class CollisionTests : MonoBehaviour {

    Collider thisCollider;
    List <Collider> collidedObjects = new List<Collider>();
    Dictionary<GameObject, ContactPointCounts> uniqueCollisions = new Dictionary<GameObject, ContactPointCounts>();
    public float colliderSkinWidth = .1f;

    public int MaxLeftSideCount = 0;
    public int MaxRightSideCount = 0;
    public int MaxUpSideCount = 0;
    public int MaxDownSideCount = 0;
    public float yHighestDiff = 0f;
    public float xHighestDiff = 0f;

    public float _highestContact;
    public float _lowestContact;
    public float _leftMostContact;
    public float _rightMostContact;
    public float _leftMostGroundContact = 0f;
    public float _rightMostGroundContact = 0f;

    public float touchedGroundSurfaceLength = 0f;

    // Use this for initialization
    void Start ()
    {
        thisCollider = this.gameObject.GetComponent<Collider>();	
	}

    private void OnCollisionEnter(Collision collision)
    {
        UpdateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateCollision(collision);
    }

    void UpdateCollision (Collision collision)
    {
        List <ContactPoint> LeftSideCount = new List<ContactPoint>();
        List<ContactPoint> RightSideCount = new List<ContactPoint>();
        List<ContactPoint> UpSideCount = new List<ContactPoint>();
        List<ContactPoint> DownSideCount = new List<ContactPoint>();

    collidedObjects.Add(collision.collider);

        float highestContact = thisCollider.bounds.min.y;
        float lowestContact = thisCollider.bounds.max.y;
        float leftMostContact = thisCollider.bounds.max.x;
        float rightMostContact = thisCollider.bounds.min.x;
        Vector2 LowerLeftContact = new Vector2(leftMostContact, lowestContact);
        Vector2 LowerRightContact = new Vector2(rightMostContact, lowestContact);

        foreach (ContactPoint contact in collision.contacts)
        {
            //Debug.DrawLine(thisCollider.bounds.center, contact.point, Color.red);

            if (contact.point.y >= thisCollider.bounds.min.y + (thisCollider.bounds.size.y * .9f))
            {
                UpSideCount.Add (contact);
            }

            if (contact.point.y <= thisCollider.bounds.max.y - (thisCollider.bounds.size.y * .9f))
            {
                DownSideCount.Add(contact);

                if (contact.point.x < LowerLeftContact.x)
                    LowerLeftContact = contact.point;

                if (contact.point.x > LowerRightContact.x)
                    LowerRightContact = contact.point;
            }

            if (contact.point.x <= thisCollider.bounds.max.x - (thisCollider.bounds.size.x * .9f))
            {
                LeftSideCount.Add(contact);
            }

            if (contact.point.x >= thisCollider.bounds.min.x + (thisCollider.bounds.size.x * .9f))
            {
                RightSideCount.Add(contact);
            }

            if (contact.point.y > highestContact)
                highestContact = contact.point.y;

            if (contact.point.y < lowestContact)
                lowestContact = contact.point.y;

            if (contact.point.x > rightMostContact)
                rightMostContact = contact.point.x;

            if (contact.point.x < leftMostContact)
                leftMostContact = contact.point.x;

            /*if (contact.point.y < LowerLeftContact.y && contact.point.x < LowerLeftContact.x)
                LowerLeftContact = contact.point;

            if (contact.point.y < LowerRightContact.y && contact.point.x > LowerRightContact.x)
                LowerRightContact = contact.point;*/
        }

        if (!uniqueCollisions.ContainsKey(collision.gameObject))
        {
            uniqueCollisions.Add(collision.gameObject, new ContactPointCounts(UpSideCount, DownSideCount, LeftSideCount, RightSideCount,
                rightMostContact, leftMostContact, highestContact, lowestContact, LowerLeftContact, LowerRightContact));
        }

        //DEBUG STUFF
        /*if (LowerLeftContact.x > thisCollider.bounds.min.x)
            Debug.Log("Not touching left side I guess... " + transform.name + "Left Most Contact " + leftMostContact + " Left Most Ground Contact = " + LowerLeftContact.x + 
                " & bounds min x = " + thisCollider.bounds.min.x + " & downside count = " + DownSideCount.Count);*/

        //Debug.Log("Left = " + LowerLeftContact.x + " Right = " + LowerRightContact.x + " Real min pos = " + thisCollider.bounds.min.x + " Surface touched = " + Mathf.Abs(LowerLeftContact.x - LowerRightContact.x) + " real size = " + thisCollider.bounds.size.x);
        //TODO : Almost working, it just acts weird if you get stuck in a wall on the right...
    }

    public void GetRealContactPointsCount ()
    {
        MaxUpSideCount = 0;
        MaxDownSideCount = 0;
        MaxLeftSideCount = 0;
        MaxRightSideCount = 0;
        MaxRightSideCount = 0;

        _highestContact = thisCollider.bounds.min.y;
        _lowestContact = thisCollider.bounds.max.y;
        _leftMostContact = thisCollider.bounds.max.x;
        _rightMostContact = thisCollider.bounds.min.x;
        _leftMostGroundContact = _leftMostContact;
        _rightMostGroundContact = _rightMostContact;

        //Debug.Log("Collision happened in the latest tick = " + uniqueCollisions.Count);

        int iterator = 0;
        foreach (KeyValuePair<GameObject,ContactPointCounts> collision in uniqueCollisions)
        {
            if (collision.Value.UpSideCount.Count > MaxUpSideCount)
            {
                MaxUpSideCount = collision.Value.UpSideCount.Count;
            }

            if (collision.Value.DownSideCount.Count > MaxDownSideCount)
            {
                MaxDownSideCount = collision.Value.DownSideCount.Count;
            }

            if (collision.Value.LeftSideCount.Count > MaxLeftSideCount)
            {
                MaxLeftSideCount = collision.Value.LeftSideCount.Count;
            }

            if (collision.Value.RightSideCount.Count > MaxRightSideCount)
            {
                MaxRightSideCount = collision.Value.RightSideCount.Count;
            }

            if (collision.Value._highestY > _highestContact)
                _highestContact = collision.Value._highestY;

            if (collision.Value._highestX > _rightMostContact)
                _rightMostContact = collision.Value._highestX;

            if (collision.Value._lowestY < _lowestContact)
                _lowestContact = collision.Value._lowestY;

            if (collision.Value._lowestX < _leftMostContact)
                _leftMostContact = collision.Value._lowestX;

            /*if (collision.Value._lowestGroundX < _leftMostGroundContact)
                _leftMostGroundContact = collision.Value._lowestGroundX;

            if (collision.Value._lowestGroundX > _rightMostGroundContact)
                _rightMostGroundContact = collision.Value._HighestGroundX;*/

            iterator++;
        }

        yHighestDiff = Mathf.Abs (_lowestContact - _highestContact);
        xHighestDiff = Mathf.Abs (_leftMostContact - _rightMostContact);
        touchedGroundSurfaceLength = Mathf.Abs(_leftMostGroundContact - _rightMostGroundContact);

        //If the highest diff between the different contact points is too low, let's ignore it. EXCEPT if this is a slope.
        if (yHighestDiff <= .05f)
        {
            if (xHighestDiff <= .05f)
            {
                gameObject.GetComponent<Characters>().CornerStuck();
            }

            MaxLeftSideCount = 0;
            MaxRightSideCount = 0;
        }

        if (xHighestDiff <= .05f)
        {
            MaxUpSideCount = 0; 
            MaxDownSideCount = 0; //Better make it like it's actually grounded to avoid glitches on stairs...
        }

        uniqueCollisions.Clear(); //Let's make sure we have the most up to date datas by clearing the previous collisions that occured on this tick

        #region Debugging Stuff
        //Debug.Log("LeftMost = " + _leftMostContact);
        //Debug.Log("RightMost = " + _rightMostContact);
        //Debug.DrawLine(new Vector3(_leftMostContact, _highestContact, transform.position.z), new Vector3(_rightMostContact, _lowestContact, transform.position.z), Color.red);
        //Debug.DrawLine(thisCollider.bounds.center, new Vector3(_leftMostGroundContact, thisCollider.bounds.min.y, transform.position.z), Color.cyan);

        //DEBUG
        /*Debug.Log("Previous physic time tick collider count = " + uniqueCollisions.Count);

        int debugIterator = 0;
        foreach (KeyValuePair<GameObject, ContactPointCounts> collision in uniqueCollisions)
        {
            Debug.Log ("Collision " + debugIterator + " : Up " + collision.Value.UpSideCount + " : Down " + collision.Value.DownSideCount + " : Left " + collision.Value.LeftSideCount + " : Right " + collision.Value.RightSideCount);

            iterator++;
        }*/

        //Debug.Log("Up = " + MaxUpSideCount + " Down = " + MaxDownSideCount + " Left = " + MaxLeftSideCount + " Right = " + MaxRightSideCount);
        //Debug.Log("Diff Between highest and lowest collision = " + Mathf.Abs(yHighestDiff));
        #endregion
    }

    //If a collision is over, let's remove it from the uniqueCollisions list
    private void OnCollisionExit(Collision collision)
    {
        //Debug.Log("Exited collider = " + collision.collider);
        if (uniqueCollisions.ContainsKey(collision.gameObject))
        {
            uniqueCollisions.Remove (collision.gameObject);
        }
    }
}
