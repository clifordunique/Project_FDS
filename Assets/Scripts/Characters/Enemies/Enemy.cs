using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Enemy : Characters {

    Vector3 rawDirection = Vector3.zero;
    Vector3 moveDirection = Vector3.zero;

    [Header ("---ENEMY SPECIFIC---")]
    #region Setup
    [SerializeField]
    int maxHealthPoints = 3;
    [SerializeField]
    GameObject LinkedPath;
    [SerializeField]
    bool FlyingEnemy = false;
    [SerializeField]
    float flightYSpeed;
    [SerializeField]
    bool useSweepSight = false;
    [SerializeField]
    float minSpaceBetweenTargetWhileChasing = 1f;
    [SerializeField]
    float slowDownTreshold = 1.5f;
    #endregion

    #region Player Related Vars
    [HideInInspector]
    public bool PlayerInSight = false;
    [HideInInspector]
    public bool touchingPlayer = false;
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
    [HideInInspector]
    public Vector3 targetLastKnownPosition = Vector3.zero;
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
    EnnemyVision vision;
    private float velocityYSmoothing;
    #endregion

    void Start ()
    {
        //Initializing waypoints...
        if (LinkedPath == null)
            Debug.LogWarning("No Path Set for " + transform.name + " ! Deactivating Patrol behaviour...");
        else
        {
            UpdateWayPointList();


            if (wayPoints.Count == 0)
                Debug.LogWarning("No waypoints in " + transform.name + ", deactivating patrol.");
            else
                GetNextWayPoint();
        }

        //Getting external stuff
        grabbedScript = gameObject.GetComponentInChildren<DashGrabPointOrientation>();
        player = GameObject.FindObjectOfType<Player>().GetComponent<Player>();

        ExclamationPoint = transform.Find("ExclamationPoint");
        QuestionMark = transform.Find("QuestionMark");
        vision = gameObject.GetComponentInChildren<EnnemyVision>();

        //Set the current health to the max possible
        currentHealthPoints = maxHealthPoints;

        //Avoid getting collision messages from other child colliders
        Physics.IgnoreCollision(thisCollider, transform.FindChild("DashGrabPoint").GetComponent<Collider>());
        Physics.IgnoreCollision(thisCollider, transform.FindChild("HitBox").GetComponent<Collider>());

        //Collisions & Physics base calculation
        CalculateRaySpacing();
    }
	
	// Update is called once per frame
	void LateUpdate ()
    {
        if (!player.dashAttachment == grabbedScript.gameObject && energized)
        {
            //TODO: Add Out Of Reach Behaviour
            switch (currentBehaviour)
            {
                case BehaviourStates.Patrol:
                    if (!useSweepSight)
                        vision.currentSightMode = EnnemyVision.SightMode.Standard;
                    else
                        vision.currentSightMode = EnnemyVision.SightMode.SweepRotation;

                    if (LinkedPath != null && wayPoints.Count > 0)
                        Patrol();
                    else
                    {
                        rawDirection = Vector3.zero;
                    }

                    ExclamationPoint.gameObject.SetActive(false);
                    QuestionMark.gameObject.SetActive(false);
                    break;

                case BehaviourStates.Chase:
                    vision.currentSightMode = EnnemyVision.SightMode.Dynamic;
                    Chase();
                    ExclamationPoint.gameObject.SetActive(true);
                    QuestionMark.gameObject.SetActive(false);
                    break;

                case BehaviourStates.LostTrack:
                    vision.currentSightMode = EnnemyVision.SightMode.LastKnownDirection;
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
        else if (!energized)
        {
            ExclamationPoint.gameObject.SetActive(false);
            QuestionMark.gameObject.SetActive(false);
            currentBehaviour = BehaviourStates.Patrol;
        }

        UpdateStateTriggers();

        moveDirection.x = rawDirection.x;
        if (FlyingEnemy)
            moveDirection.y = rawDirection.y;

        //Neutralizing Y moves when grounded, or head hitting ceiling or dashing*
        if (!FlyingEnemy)
        {
            if ((collisions.above && !collisions.getThroughAbove) || collisions.below)
            {
                moveDirection.y = 0;
            }

            moveDirection.y += calculatedGravity * Time.deltaTime;
        }

        if (!player.dashAttachment == grabbedScript.gameObject)
            ApplyMoveAndCollisions(moveDirection * Time.deltaTime);
    }

    void UpdateStateTriggers()
    {
        //Energized state depending if Health depleted or not
        if (currentHealthPoints <= 0)
            energized = false;
        else
            energized = true;

        if (PlayerInSight || touchingPlayer)
        {
            currentBehaviour = BehaviourStates.Chase;
            targetLastKnownPosition = player.transform.position;
        }

        //Just lost sight of player
        if (currentBehaviour == BehaviourStates.Chase && !PlayerInSight && !touchingPlayer)
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

        rawDirection.x = targetMove.x * speed;

        if (FlyingEnemy)
            rawDirection.y = targetMove.y;
    }

    void Chase()
    {
        if (FlyingEnemy)
            FlyChase();
        else
        {
            Vector3 targetMove = Vector3.zero;
            float targetTreshold = slowDownTreshold - minSpaceBetweenTargetWhileChasing;

            if (Mathf.Abs(player.transform.position.x - transform.position.x) > minSpaceBetweenTargetWhileChasing)
            {
                targetMove = player.transform.position - transform.position;

                float targetDistanceX = Mathf.Abs(player.transform.position.x - transform.position.x);

                if (Mathf.Abs(player.transform.position.x - transform.position.x) < slowDownTreshold)
                {
                    targetDistanceX -= minSpaceBetweenTargetWhileChasing;
                    targetDistanceX /= targetTreshold;
                    targetMove.x = Mathf.Sign(targetMove.x) * targetDistanceX;
                }

                if (targetMove.x > 1)
                    targetMove.x = 1;
                else if (targetMove.x < -1)
                    targetMove.x = -1f;

                targetMove.x *= speed;
            }
            else
                targetMove.x = 0;

            if (Mathf.Abs(player.transform.position.y - transform.position.y) > minSpaceBetweenTargetWhileChasing)
            {
                float targetDistanceY = Mathf.Abs(player.transform.position.y - transform.position.y);

                if (Mathf.Abs(player.transform.position.y - transform.position.y) < slowDownTreshold)
                {
                    targetDistanceY -= minSpaceBetweenTargetWhileChasing;
                    targetDistanceY /= targetTreshold;
                    targetMove.y = Mathf.Sign(targetMove.y) * targetDistanceY;
                }

                if (targetMove.y > 1)
                    targetMove.y = 1;
                else if (targetMove.y < -1)
                    targetMove.y = -1f;

                targetMove.y *= speed;
            }
            else
            {
                targetMove.y = 0;
            }

            rawDirection = targetMove;
            Debug.DrawRay(transform.position, rawDirection, Color.cyan);
        }
    }

    void FlyChase ()
    {
        Vector3 targetOffset = new Vector3(2, 2.5f, 0);
        Vector3 targetMove = player.transform.position + targetOffset - transform.position;

        //pos += transform.up * Time.deltaTime * MoveSpeed;

        targetMove = targetMove + transform.up * Mathf.Sin(Time.time * 5 /* frequency */) * .2f /* magnitude */;
        rawDirection = targetMove * speed;
    }

    void GoToLastKnownPosition()
    {
        Debug.DrawLine(transform.position, targetLastKnownPosition, Color.cyan);
        Vector3 targetMove = targetLastKnownPosition - transform.position;

        float targetDistanceX = Mathf.Abs(targetLastKnownPosition.x - transform.position.x);

        float targetTreshold = slowDownTreshold;

        if (Mathf.Abs(targetLastKnownPosition.x - transform.position.x) < slowDownTreshold)
        {
            targetDistanceX /= targetTreshold;
            targetMove.x = Mathf.Sign(targetMove.x) * targetDistanceX;
        }

        if (targetMove.x > 1)
            targetMove.x = 1;
        else if (targetMove.x < -1)
            targetMove.x = -1f;

        targetMove.x *= speed;

        float targetDistanceY = Mathf.Abs(targetLastKnownPosition.y - transform.position.y);

        if (Mathf.Abs(targetLastKnownPosition.y - transform.position.y) < slowDownTreshold)
        {
            targetDistanceY /= targetTreshold;
            targetMove.y = Mathf.Sign(targetMove.y) * targetDistanceY;
        }

        if (targetMove.y > 1)
            targetMove.y = 1;
        else if (targetMove.y < -1)
            targetMove.y = -1f;

        targetMove.y *= speed;

        rawDirection = targetMove;

        Debug.Log(transform.parent.name + " go to last known position");
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

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            touchingPlayer = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            touchingPlayer = false;
        }
    }
}
