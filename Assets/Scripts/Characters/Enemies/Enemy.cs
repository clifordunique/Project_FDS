using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Enemy : Characters {

    Vector3 moveDirection = Vector3.zero;

    [Header ("---ENEMY SPECIFIC---")]
    #region Setup
    [SerializeField]
    int maxHealthPoints = 3;
    [SerializeField]
    GameObject LinkedPath;
    #endregion

    #region Player Related Vars
    [HideInInspector]
    public bool PlayerInSight = false;
    Player player;
    #endregion

    #region Patrol
    List<Transform> wayPoints = new List<Transform>();
    Transform currentWayPoint = null;
    #endregion

    #region Player Search
    [SerializeField]
    float searchForPlayerDuration = 3f;
    float searchForPlayerTimer = 0f;
    Vector3 targetLastKnownPosition = Vector3.zero;
    #endregion

    #region State Machine Vars
    [HideInInspector]
    public bool energized = true;

    int currentHealthPoints;
    enum BehaviourStates { Patrol, Chase, LostTrack, OutOfReach };
    BehaviourStates currentBehaviour = BehaviourStates.Patrol;
    #endregion

    #region other Components
    DashGrabPointOrientation grabbedScript;
    Transform ExclamationPoint;
    Transform QuestionMark;
    #endregion

    void Start ()
    {
        //Initializing waypoints...
        if (LinkedPath == null)
            Debug.LogWarning("No Path Set for " + transform.name + " ! Deactivating Patrol behaviour...");
        else
        {
            UpdateWayPointList();
            GetNextWayPoint();
        }

        //Getting external stuff
        grabbedScript = gameObject.GetComponentInChildren<DashGrabPointOrientation>();
        player = GameObject.FindObjectOfType<Player>().GetComponent<Player>();

        ExclamationPoint = transform.Find("ExclamationPoint");
        QuestionMark = transform.Find("QuestionMark");

        //Set the current health to the max possible
        currentHealthPoints = maxHealthPoints;

        //Avoid getting collision messages from other child colliders
        Physics.IgnoreCollision(thisCollider, transform.FindChild("DashGrabPoint").GetComponent<Collider>());
        Physics.IgnoreCollision(thisCollider, transform.FindChild("HitBox").GetComponent<Collider>());

        //Collisions & Physics base calculation
        CalculateRaySpacing();

        calculatedGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        calculatedJumpForce = Mathf.Abs(calculatedGravity) * timeToJumpApex;


    }
	
	// Update is called once per frame
	void Update ()
    {
        if (!player.dashAttachment == grabbedScript.gameObject && energized)
        {
            //TODO: Add Out Of Reach Behaviour
            switch (currentBehaviour)
            {
                case BehaviourStates.Patrol:
                    if (LinkedPath != null)
                        Patrol();
                    else
                        moveDirection.x = 0f;

                    ExclamationPoint.gameObject.SetActive(false);
                    QuestionMark.gameObject.SetActive(false);
                    break;
                case BehaviourStates.Chase:
                    Chase();
                    ExclamationPoint.gameObject.SetActive(true);
                    QuestionMark.gameObject.SetActive(false);
                    break;
                case BehaviourStates.LostTrack:
                    GoToLastKnownPosition();
                    searchForPlayerTimer += Time.deltaTime;

                    ExclamationPoint.gameObject.SetActive(false);
                    QuestionMark.gameObject.SetActive(true);

                    //Abandoning chase behaviour after a certain amount of time
                    if (searchForPlayerTimer >= searchForPlayerDuration)
                    {
                        searchForPlayerTimer = 0f;
                        currentBehaviour = BehaviourStates.Patrol;
                    }
                    break;
            }
        }

        UpdateStateTriggers();

        moveDirection.x *= speed;

        //Neutralizing Y moves when grounded, or head hitting ceiling or dashing
        if ((collisions.above && !collisions.getThroughAbove) || collisions.below)
        {
            moveDirection.y = 0;
        }

        moveDirection.y += calculatedGravity * Time.deltaTime;


        if(!player.dashAttachment == grabbedScript.gameObject)
            ApplyMoveAndCollisions(moveDirection * Time.deltaTime);
    }

    void UpdateStateTriggers()
    {
        //Energized state depending if Health depleted or not
        if (currentHealthPoints <= 0)
            energized = false;
        else
            energized = true;

        if (PlayerInSight)
        {
            currentBehaviour = BehaviourStates.Chase;
            targetLastKnownPosition = player.transform.position;
        }

        //Just lost sight of player
        if (currentBehaviour == BehaviourStates.Chase && !PlayerInSight)
            currentBehaviour = BehaviourStates.LostTrack;
    }

    #region Moving Around
    void UpdateWayPointList()
    {
        wayPoints.Clear();
        wayPoints = LinkedPath.GetComponentsInChildren<Transform>().ToList();
        wayPoints.RemoveAt(0);
    }

    void GetNextWayPoint()
    {
        if (currentWayPoint == null)
        {
            currentWayPoint = wayPoints[0];
        }
        else
        {
            try
            {
                currentWayPoint = wayPoints[wayPoints.IndexOf(currentWayPoint) + 1];
            }
            catch (System.ArgumentOutOfRangeException)
            {
                currentWayPoint = wayPoints[0];
            }
        }
    }

    void Patrol()
    {
        Vector3 targetMove = currentWayPoint.transform.position - transform.position;
        targetMove = targetMove.normalized;

        moveDirection.x = targetMove.x;
    }

    void Chase()
    {
        Vector3 targetMove = Vector3.zero;

        if (Mathf.Abs(player.transform.position.x - transform.position.x) > 3f)
        {
            targetMove = player.transform.position - transform.position;
            targetMove = targetMove.normalized;
            moveDirection.x = targetMove.x;
        }
        else
        {
            moveDirection.x = 0;
            //Debug.Log("Close to Player");
        }
    }

    void GoToLastKnownPosition()
    {
        Vector3 targetMove = targetLastKnownPosition - transform.position;
        targetMove = targetMove.normalized;

        moveDirection.x = targetMove.x;
    }
    #endregion

    public void ReEnergize ()
    {
        energized = true;
        currentHealthPoints = maxHealthPoints;
    }

    public void GetDamage (int damageAmount)
    {
        currentHealthPoints -= damageAmount;

        if (currentHealthPoints < 0)
            currentHealthPoints = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Enemy collider triggered = " + other.transform.name + " current waypoint = " + currentWayPoint.transform.name);
        if (other.transform == currentWayPoint)
        {
            GetNextWayPoint();
        }
    }
}
