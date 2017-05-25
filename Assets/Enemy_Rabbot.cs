using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Enemy_Rabbot : Characters {

    bool jump = false;

    [SerializeField]
    GameObject LinkedPath;
    [SerializeField]
    int maxHealthPoints = 3;
    int currentHealthPoints;
    List<Transform> wayPoints = new List<Transform>();
    Transform currentWayPoint = null;
    Vector3 targetLastKnownPosition = Vector3.zero;
    [SerializeField]
    float searchForPlayerDuration = 3f;
    float searchForPlayerTimer = 0f;
    Transform ExclamationPoint;
    Transform QuestionMark;

    #region State Machine Vars
    public bool PlayerInSight = false;
    DashGrabPointOrientation grabbedScript;
    [HideInInspector]
    public bool energized = true;
    Player player;
    enum BehaviourStates { Patrol, Chase, LostTrack, OutOfReach };
    BehaviourStates currentBehaviour = BehaviourStates.Patrol;
    #endregion

    // Use this for initialization
    void Start ()
    {
        UpdateWayPointList();
        GetNextWayPoint();
        grabbedScript = gameObject.GetComponentInChildren<DashGrabPointOrientation>();
        player = GameObject.FindObjectOfType<Player>().GetComponent<Player>();
        ExclamationPoint = transform.Find("ExclamationPoint");
        QuestionMark = transform.Find("QuestionMark");
        currentHealthPoints = maxHealthPoints;
    }
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();
        //jump = Input.GetButtonDown("Jump");

        if (!player.dashAttachment == grabbedScript.gameObject && energized)
        {
            //TODO: Add Out Of Reach Behaviour
            switch (currentBehaviour)
            {
                case BehaviourStates.Patrol:
                    Patrol();
                    ExclamationPoint.gameObject.SetActive(false);
                    QuestionMark.gameObject.SetActive(false);
                    break;
                case BehaviourStates.Chase:
                    Chase();
                    ExclamationPoint.gameObject.SetActive(true);
                    QuestionMark.gameObject.SetActive(false);
                    //Debug.Log(transform.name + " chasing");
                    break;
                case BehaviourStates.LostTrack:
                    GoToLastKnownPosition();
                    searchForPlayerTimer += Time.deltaTime;

                    ExclamationPoint.gameObject.SetActive(false);
                    QuestionMark.gameObject.SetActive(true);

                    if (searchForPlayerTimer >= searchForPlayerDuration)
                    {
                        searchForPlayerTimer = 0f;
                        currentBehaviour = BehaviourStates.Patrol;
                    }

                    //Debug.Log(transform.name + " Lost Track");
                    break;
            }
        }
        else
            Move(0, 0, false, false);
    }

    public void ReEnergize ()
    {
        energized = true;
        currentHealthPoints = maxHealthPoints;
    }

    private void Update()
    {
        if (currentHealthPoints <= 0)
        {
            energized = false;
        }
        else
        {
            energized = true;
        }

        if (PlayerInSight)
        {
            currentBehaviour = BehaviourStates.Chase;
            targetLastKnownPosition = player.transform.position;
        }

        if (currentBehaviour == BehaviourStates.Chase && !PlayerInSight)
            currentBehaviour = BehaviourStates.LostTrack;
    }

    public void GetDamage (int damageAmount)
    {
        currentHealthPoints -= damageAmount;

        if (currentHealthPoints < 0)
            currentHealthPoints = 0;
    }

    void UpdateWayPointList ()
    {
        wayPoints.Clear();
        wayPoints = LinkedPath.GetComponentsInChildren<Transform>().ToList();
        wayPoints.RemoveAt(0);
    }

    void GetNextWayPoint ()
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
        //Debug.Log("Current WayPoint for " + transform.name + " is " + currentWayPoint.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == currentWayPoint)
        {
            GetNextWayPoint();
        }
    }

    void Patrol ()
    {
        Vector3 moveDirection;
        moveDirection = currentWayPoint.transform.position - transform.position;
        moveDirection = moveDirection.normalized;

        //Debug.Log("Patrol direction = " + moveDirection);

        Move(moveDirection.x, jump);
    }

    void Chase ()
    {
        Vector3 moveDirection;
        moveDirection = player.transform.position - transform.position;
        moveDirection = moveDirection.normalized;

        //Debug.Log("Patrol direction = " + moveDirection);

        //Debug.Log("X distance from player to enemy = " + Mathf.Abs(player.transform.position.x - transform.position.x));

        if (Mathf.Abs(player.transform.position.x - transform.position.x) > 3f)
            Move(moveDirection.x, jump);
        else
        {
            Move(0);
            //Debug.Log("Close to player");
        }
    }

    void GoToLastKnownPosition ()
    {
        Vector3 moveDirection;
        moveDirection = targetLastKnownPosition - transform.position;
        moveDirection = moveDirection.normalized;

        //Debug.Log("Patrol direction = " + moveDirection);

        Move(moveDirection.x, jump);
    }

}
