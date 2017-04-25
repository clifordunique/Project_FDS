﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Enemy : Characters {

    bool jump = false;
    CollisionTests collisionTests;

    [SerializeField]
    GameObject LinkedPath;
    List<Transform> wayPoints = new List<Transform>();
    Transform currentWayPoint = null;

    // Use this for initialization
    void Start ()
    {
        collisionTests = gameObject.GetComponent<CollisionTests>();
        UpdateWayPointList();
        GetNextWayPoint();
    }
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        collisionTests.GetRealContactPointsCount();
        //jump = Input.GetButtonDown("Jump");
        Patrol();
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

        Debug.Log("Current WayPoint for " + transform.name + " is " + currentWayPoint.name);
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

        Debug.Log("Patrol direction = " + moveDirection);

        Move(moveDirection.x, jump);
    }

}
