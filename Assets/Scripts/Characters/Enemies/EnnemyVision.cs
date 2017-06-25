using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnnemyVision : MonoBehaviour {

    Enemy thisEnemy;
    SpriteRenderer sprite;
    Player player;

    #region Sight Setup
    [SerializeField]
    float sigthRange = 10f;
    [SerializeField]
    float defaultSightAngle = 45f;
    [SerializeField]
    LayerMask ignoredLayers;
    [SerializeField]
    bool displayVisionInGame = false;

    public enum SightMode { Standard, Dynamic, SweepRotation };
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
        currentSightAngle = defaultSightAngle;

        //Enemy is awake
        if (thisEnemy.energized)
        {
            float playerToEnemyDistance = Vector3.Magnitude(transform.position - player.transform.position);
            thisEnemy.PlayerInSight = false;
            drawDetectSphere = false;

            //Player is in range
            if (playerToEnemyDistance <= sigthRange)
            {
                centerToTargetAngle = 0f;

                SightModeManager();

                Debug.DrawRay(transform.position, sightDirection, Color.red);

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

        Vector3[] vertices = new Vector3[3];

        Vector3 sideDir = Vector3.right;

        if (thisEnemy.PlayerInSight)
        {
            sideDir = sightDirection;
        }
        else
        {
            if (!thisEnemy.thisSprite.flipX)
                sideDir = transform.right;
            else
                sideDir = -transform.right;
        }

        vertices[0] = Vector3.zero;
        vertices[1] = (Quaternion.Euler(0,0, defaultSightAngle) * sideDir * sigthRange);
        vertices[2] = (Quaternion.Euler(0,0, -defaultSightAngle) * sideDir * sigthRange);

        mesh.vertices = vertices;

        int[] tri = new int[3];

        //  Lower left triangle.
        tri[0] = 2;
        tri[1] = 0;
        tri[2] = 1;

        mesh.triangles = tri;

        Vector3[] normals  = new Vector3[3];

        normals[0] = Vector3.forward;
        normals[1] = Vector3.forward;
        normals[2] = Vector3.forward;

        mesh.normals = normals;
    }

    void SightModeManager ()
    {
        ignoreAngleCheck = false; //By default

        switch (currentSightMode)
        {
            case SightMode.Standard:
                PatrolSight();
            break;

            case SightMode.Dynamic:
                ChaseSight();
            break;
        }
    }

    void PatrolSight ()
    {
        if (sprite.flipX)
            sightDirection = transform.right;
        else
            sightDirection = -transform.right;

        centerToTargetAngle = Vector3.Angle(sightDirection, (transform.position - player.transform.position));
    }

    void ChaseSight()
    {
        ignoreAngleCheck = true;
    }

    bool drawDetectSphere = false;

    void ConfirmPlayerIsInSight (RaycastHit hit)
    {
        if (hit.transform.CompareTag("Player"))
        {
            drawDetectSphere = true;
            thisEnemy.PlayerInSight = true;
            sightDirection = (player.transform.position - transform.position).normalized;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawDetectSphere)
            Gizmos.DrawSphere(transform.position, .5f);
    }
}
