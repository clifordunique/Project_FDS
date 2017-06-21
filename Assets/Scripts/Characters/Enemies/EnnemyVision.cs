﻿using System.Collections;
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

                Debug.DrawRay(thisEnemy.thisCollider.bounds.center, Quaternion.Euler(0, 0, centerToTargetAngle) * Vector3.right * sigthRange, Color.red);
                Debug.DrawRay(thisEnemy.thisCollider.bounds.center, Quaternion.Euler(0, 0, defaultSightAngle /*+ centerToTargetAngle*/) * Vector3.right * sigthRange, Color.cyan);
                Debug.DrawRay(thisEnemy.thisCollider.bounds.center, Quaternion.Euler(0, 0, /*centerToTargetAngle*/ -defaultSightAngle) * Vector3.right * sigthRange, Color.cyan);

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
        }
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
        centerToTargetAngle = Vector3.Angle(sightDirection, (transform.position - player.transform.position)); // Basically, when in chase mode, the player is always considered inside the FOV
    }

    bool drawDetectSphere = false;

    void ConfirmPlayerIsInSight (RaycastHit hit)
    {
        if (hit.transform.CompareTag("Player"))
        {
            drawDetectSphere = true;
            thisEnemy.PlayerInSight = true;
            //Debug.DrawLine(transform.position, hit.point, Color.red);
            //Debug.Log("PLAYER IN SIGHT");
        }
        else
            Debug.Log("View obstructed = " + hit.transform.name);
    }

    private void OnDrawGizmos()
    {
        if (drawDetectSphere)
            Gizmos.DrawSphere(transform.position, .5f);
    }
}
