using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pathfinding;
using Pathfinding.RVO;
using Pathfinding.Util;

public class Enemy : Characters {

    Vector3 rawDirection = Vector3.zero;
    Vector3 previousRawDir = Vector3.zero;
    Vector3 moveDirection = Vector3.zero;

    public bool preventStopChase = false;

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
    Seeker AstarSeeker;
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

        //A* pathfinding if flyingEnemy
        if (FlyingEnemy)
        {
                //gameObject.AddComponent<Seeker>();
                AstarSeeker = GetComponent<Seeker>();
            // Make sure we receive callbacks when paths are calculated
            AstarSeeker.pathCallback += OnPathComplete;

            StartCoroutine(RepeatTrySearchPath());
        }
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

                    if (!FlyingEnemy)
                        Chase();
                    else
                        MovementUpdate(Time.deltaTime);

                    ExclamationPoint.gameObject.SetActive(true);
                    QuestionMark.gameObject.SetActive(false);
                    break;

                case BehaviourStates.LostTrack:
                    vision.currentSightMode = EnnemyVision.SightMode.LastKnownDirection;

                    //TODO: Adapt this method to flying enemies
                    if (!FlyingEnemy)
                        GoToLastKnownPosition();
                    else
                        MovementUpdate(Time.deltaTime);

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

        moveDirection.x = rawDirection.x * speed;
        if (FlyingEnemy)
            moveDirection.y = rawDirection.y * flightYSpeed;

        //Neutralizing Y moves when grounded, or head hitting ceiling or dashing*
        if (!FlyingEnemy)
        {
            if ((collisions.above && !collisions.getThroughAbove) || collisions.below)
            {
                moveDirection.y = 0;
            }

            moveDirection.y += calculatedGravity * Time.deltaTime;
        }

        //Debug.Log(transform.parent.name + "Move direction = " + moveDirection + " & Raw Direction was = " + rawDirection);

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
        if (currentBehaviour == BehaviourStates.Chase && !PlayerInSight && !touchingPlayer && !preventStopChase)
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

        rawDirection.x = targetMove.x;

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
        rawDirection = targetMove;
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

    #region flying enemy pathfinding

    
    /** Time when the last path request was sent */
    protected float lastRepath = -9999;

    /** Determines how often it will search for new paths.
 * If you have fast moving targets or AIs, you might want to set it to a lower value.
 * The value is in seconds between path requests.
 */
    public float repathRate = 0.5F;

    protected IMovementPlane movementPlane = GraphTransform.identityTransform;

    /** Only when the previous path has been returned should be search for a new path */
    protected bool canSearchAgain = true;

    Transform AStarTarget;

    /** Current path which is followed */
    protected Path path;

    bool TargetReached = false;

    protected PathInterpolator interpolator = new PathInterpolator();

    /** Determines within what range it will switch to target the next waypoint in the path */
    public float pickNextWaypointDist = 2;

    /** Tries to search for a path.
	 * Will search for a new path if there was a sufficient time since the last repath and both
	 * #canSearchAgain and #canSearch are true and there is a target.
	 *
	 * \returns The time to wait until calling this function again (based on #repathRate)
	 */
    public float TrySearchPath()
    {
        if (currentBehaviour == BehaviourStates.Chase)
            AStarTarget = player.transform;

        if (Time.time - lastRepath >= repathRate && canSearchAgain && AStarTarget != null)
        {
            SearchPath();
            return repathRate;
        }
        else {
            float v = repathRate - (Time.time - lastRepath);
            return v < 0 ? 0 : v;
        }
    }

    /** Distance to the end point to consider the end of path to be reached.
 * When the end is within this distance then #OnTargetReached will be called and #TargetReached will return true.
 */
    public float endReachedDistance = 0.2F;

    Vector3 targetPoint;

    /** Called during either Update or FixedUpdate depending on if rigidbodies are used for movement or not */
    protected void MovementUpdate(float deltaTime)
    {
            if (!interpolator.valid)
            {
                rawDirection = Vector3.zero;
            }
            else {
                var currentPosition = transform.position;

                interpolator.MoveToLocallyClosestPoint(currentPosition, true, false);
                interpolator.MoveToCircleIntersection2D(currentPosition, pickNextWaypointDist, movementPlane);
                targetPoint = interpolator.position;
                var dir = movementPlane.ToPlane(targetPoint - currentPosition);

                var distanceToEnd = dir.magnitude + interpolator.remainingDistance;
                // How fast to move depending on the distance to the target.
                // Move slower as the character gets closer to the target.
                //float slowdown = slowdownDistance > 0 ? distanceToEnd / slowdownDistance : 1;

                // a = v/t, should probably expose as a variable
                float acceleration = speed / 0.4f;
                //moveDirection += MovementUtilities.CalculateAccelerationToReachPoint(dir, dir.normalized * speed, moveDirection, acceleration, speed) * deltaTime;
                //moveDirection = MovementUtilities.ClampVelocity(moveDirection, speed, slowdown, true, movementPlane.ToPlane(rotationIn2D ? tr.up : tr.forward));

                //ApplyGravity(deltaTime);

                //TODO : reapply this
                //Vector3 targetOffset = new Vector3(2, 2.5f, 0);
                //Vector3 targetMove = player.transform.position + targetOffset - transform.position;

                if (previousRawDir != Vector3.zero)
                    rawDirection = Vector3.Lerp(previousRawDir, targetPoint - transform.position, 1f);
                else
                    rawDirection = targetPoint - transform.position;

                //rawDirection = targetPoint - transform.position;
                rawDirection *= speed;
                /*rawDirection.x *= speed;
                rawDirection.y *= flightYSpeed;*/

                Debug.DrawLine(transform.position, targetPoint, Color.magenta);

                if (distanceToEnd <= endReachedDistance && !TargetReached)
                {
                    TargetReached = true;
                    Debug.Log(transform.name + " target reached");
                    //OnTargetReached();
                }

                Debug.Log(transform.name + " using flying pathfinder");

                //float rotationSpeed = 360f;

                // Rotate towards the direction we are moving in
                //var currentRotationSpeed = rotationSpeed;
                //RotateTowards(rawDirection, currentRotationSpeed * deltaTime);

                //var delta2D = CalculateDeltaToMoveThisFrame(movementPlane.ToPlane(currentPosition), distanceToEnd, deltaTime);
                //Move(currentPosition, movementPlane.ToWorld(delta2D, verticalVelocity * deltaTime));
                //rawDirection = movementPlane.ToWorld(rawDirection, 0);

                previousRawDir = rawDirection;
            }
    }

    /** Rotates in the specified direction.
 * Rotates around the Y-axis
 * \see turningSpeed
 */
    void RotateTowards(Vector2 direction, float maxDegrees)
    {
        if (direction != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementPlane.ToWorld(direction, 0), movementPlane.ToWorld(Vector2.zero, 1));
            targetRotation *= Quaternion.Euler(90, 0, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxDegrees);
        }
    }

    /** Requests a path to the target */
    public virtual void SearchPath()
    {
        if (AStarTarget == null) throw new System.InvalidOperationException("Target is null");

        lastRepath = Time.time;
        // This is where we should search to
        Vector3 targetPosition = AStarTarget.position;

        canSearchAgain = false;

        // Alternative way of requesting the path
        //ABPath p = ABPath.Construct(GetFeetPosition(), targetPosition, null);
        //seeker.StartPath(p);

        // We should search from the current position
        AstarSeeker.StartPath(transform.position, targetPosition);
    }

    /** Tries to search for a path every #repathRate seconds.
 * \see TrySearchPath
 */
    protected IEnumerator RepeatTrySearchPath()
    {
        while (true) yield return new WaitForSeconds(TrySearchPath());
    }

    /** Called when a requested path has finished calculation.
 * A path is first requested by #SearchPath, it is then calculated, probably in the same or the next frame.
 * Finally it is returned to the seeker which forwards it to this function.\n
 */
    public virtual void OnPathComplete(Path _p)
    {
            ABPath p = _p as ABPath;

            if (p == null) throw new System.Exception("This function only handles ABPaths, do not use special path types");

            canSearchAgain = true;

            // Claim the new path
            p.Claim(this);

            // Path couldn't be calculated of some reason.
            // More info in p.errorLog (debug string)
            if (p.error)
            {
                p.Release(this);
                return;
            }

            // Release the previous path
            if (path != null) path.Release(this);

            // Replace the old path
            path = p;

            // Make sure the path contains at least 2 points
            if (path.vectorPath.Count == 1) path.vectorPath.Add(path.vectorPath[0]);
            interpolator.SetPath(path.vectorPath);


            //TODO : WAS HERE
            var graph = AstarData.GetGraph(path.path[0]) as ITransformedGraph;
            movementPlane = graph != null ? graph.transform : GraphTransform.identityTransform;

            // Reset some variables
            TargetReached = false;

            // Simulate movement from the point where the path was requested
            // to where we are right now. This reduces the risk that the agent
            // gets confused because the first point in the path is far away
            // from the current position (possibly behind it which could cause
            // the agent to turn around, and that looks pretty bad).
            interpolator.MoveToLocallyClosestPoint((transform.position + p.originalStartPoint) * 0.5f);
            interpolator.MoveToLocallyClosestPoint(transform.position);
    }

    #endregion


}
