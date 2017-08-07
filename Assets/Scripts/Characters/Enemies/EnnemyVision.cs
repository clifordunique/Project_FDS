using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnnemyVision : MonoBehaviour {

    Enemy thisEnemy;
    SpriteRenderer sprite;
    Player player;

    #region Sight Setup
    [SerializeField]
    float sightRange = 10f;
    [SerializeField]
    float defaultSightAngle = 45f;
    [SerializeField]
    LayerMask ignoredLayers;
    [SerializeField]
    bool displayVisionInGame = false;
    [SerializeField]
    float sweepSpeed = 20f;

    public enum SightMode { Standard, Dynamic, SweepRotation, LastKnownDirection };
    public SightMode currentSightMode = SightMode.Standard;
    #endregion

    #region Sight Current State
    Vector3 sightDirection;
    float centerToTargetAngle = 0f;
    float currentSightAngle;
    bool ignoreAngleCheck = false;
    #endregion

    // Use this for initialization
    void Start ()
    {
        thisEnemy = transform.GetComponentInParent<Enemy>();
        sprite = transform.GetComponentInParent<SpriteRenderer>();
        player = GameObject.FindObjectOfType<Player>();

        currentSightAngle = defaultSightAngle;
	}
	
	// Update is called once per frame
	void Update ()
    {
        //Enemy is awake
        if (thisEnemy.energized)
        {
            centerToTargetAngle = 0f;

            SightModeManager();

            float playerToEnemyDistance = Vector3.Magnitude(transform.position - player.transform.position);
            thisEnemy.PlayerInSight = false;
            drawDetectSphere = false;

            //Player is in range
            if (playerToEnemyDistance <= sightRange)
            {
                Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, currentSightAngle / 2) * sightDirection * sightRange, Color.red);
                Debug.DrawRay(transform.position, sightDirection * sightRange, Color.red);
                Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -(currentSightAngle / 2)) * sightDirection * sightRange, Color.red);
                //Debug.Log(transform.name + " Angle from center = " + centerToTargetAngle + " current sight angle / 2 = " + currentSightAngle / 2);
                
                //Player is in fov
                if (ignoreAngleCheck || centerToTargetAngle <= currentSightAngle / 2)
                {
                    RaycastHit hit;
                    List<Vector3> raytargets = new List<Vector3>();
                    Vector3 headCheck = new Vector3(player.thisCollider.bounds.center.x, player.thisCollider.bounds.center.y + (player.thisCollider.bounds.extents.y * .9f), player.thisCollider.bounds.center.z);
                    Vector3 feetCheck = new Vector3(player.thisCollider.bounds.center.x, player.thisCollider.bounds.center.y - (player.thisCollider.bounds.extents.y * .9f), player.thisCollider.bounds.center.z);

                    raytargets.Add(headCheck);
                    raytargets.Add(player.thisCollider.bounds.center);
                    raytargets.Add(feetCheck);

                    foreach (Vector3 target in raytargets)
                    {
                        if (!thisEnemy.PlayerInSight && !thisEnemy.touchingPlayer)
                        {
                            if (Physics.Linecast(transform.position, target, out hit, ignoredLayers))
                            {
                                //Testing view obstruction
                                ConfirmPlayerIsInSight(hit);
                            }
                        }
                        else
                            break;
                    }
                }
            }

            if (displayVisionInGame)
                DisplayVision();

        }
	}

    void DisplayVision ()
    {
        MeshFilter  meshfilter = gameObject.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        meshfilter.mesh = mesh;

        Vector3[] vertices = new Vector3[4];

        Vector3 sideDir = Vector3.right;

        /*if (thisEnemy.PlayerInSight || currentSightMode == SightMode.SweepRotation || currentSightMode == SightMode.LastKnownDirection)
        {*/
            sideDir = sightDirection;
        /*}
        else
        {
            if (!thisEnemy.thisSprite.flipX)
                sideDir = transform.right;
            else
                sideDir = -transform.right;
        }*/

        vertices[0] = Vector3.zero;
        vertices[1] = (Quaternion.Euler(0,0, defaultSightAngle / 2) * sideDir * sightRange);
        vertices[2] = (sideDir * sightRange);
        vertices[3] = (Quaternion.Euler(0,0, -(defaultSightAngle / 2)) * sideDir * sightRange);

        mesh.vertices = vertices;

        int[] tri = new int[6];

        //  Lower left triangle.
        tri[0] = 0;
        tri[1] = 1;
        tri[2] = 2;

        tri[3] = 0;
        tri[4] = 2;
        tri[5] = 3;

        mesh.triangles = tri;

        Vector3[] normals  = new Vector3[4];

        normals[0] = Vector3.forward;
        normals[1] = Vector3.forward;
        normals[2] = Vector3.forward;
        normals[3] = Vector3.forward;

        mesh.normals = normals;
    }

    void SightModeManager ()
    {
        //Debug.Log("Sight Mode Manager engaged");

        ignoreAngleCheck = false; //By default

        switch (currentSightMode)
        {
            case SightMode.Standard:
                PatrolSight();
            break;

            case SightMode.Dynamic:
                ChaseSight();
            break;

            case SightMode.SweepRotation:
                SweepSight();
            break;

            case SightMode.LastKnownDirection:
                WatchLastKnownDirection();
            break;
        }
    }

    void WatchLastKnownDirection ()
    {
        sightDirection = thisEnemy.targetLastKnownPosition - transform.position;
        sightDirection = sightDirection.normalized;

        centerToTargetAngle = Vector3.Angle(-sightDirection, (transform.position - player.transform.position));
    }

    void SweepSight ()
    {

        sightDirection = (Quaternion.Euler(0, 0, sweepSpeed * Time.deltaTime) * sightDirection);
        sightDirection = sightDirection.normalized;

        centerToTargetAngle = Vector3.Angle(-sightDirection, (transform.position - player.transform.position));
    }

    void PatrolSight ()
    {
        //Debug.Log("Standard patrol");

        if (sprite.flipX)
            sightDirection = transform.right;
        else
            sightDirection = -transform.right;

        centerToTargetAngle = Vector3.Angle(sightDirection, (transform.position - player.transform.position));
    }

    void ChaseSight()
    {
        //Debug.Log("Standard chase");

        ignoreAngleCheck = true;
        sightDirection = (player.transform.position - transform.position).normalized;
    }

    bool drawDetectSphere = false;

    void ConfirmPlayerIsInSight (RaycastHit hit)
    {
        if (hit.transform.CompareTag("Player"))
        {
            drawDetectSphere = true;
            thisEnemy.PlayerInSight = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawDetectSphere)
            Gizmos.DrawSphere(transform.position, .5f);
    }
}
