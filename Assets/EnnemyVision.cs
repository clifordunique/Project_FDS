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
    float sightAngle = 45f;
    [SerializeField]
    LayerMask playerLayer;
    #endregion

    // Use this for initialization
    void Start ()
    {
        thisEnemy = transform.GetComponentInParent<Enemy>();
        sprite = transform.GetComponentInParent<SpriteRenderer>();
        player = GameObject.FindObjectOfType<Player>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        float playerToEnemyDistance = Vector3.Magnitude (transform.position - player.transform.position);
        float currentSightAngle = 0f;

        thisEnemy.PlayerInSight = false;
        drawDetectSphere = false;
        if (playerToEnemyDistance <= sigthRange)
        {
            //Debug.Log("Player is in sight range");
            Vector3 sightDirection;

            if (sprite.flipX)
                sightDirection = transform.right;
            else
                sightDirection = -transform.right;

            currentSightAngle = Vector3.Angle(sightDirection, (transform.position - player.transform.position));
            //Debug.Log(currentSightAngle);

            if (currentSightAngle < sightAngle / 2)
            {
                RaycastHit hit;
                List<Vector3> raytargets = new List<Vector3>();
                Vector3 headCheck = new Vector3(player.thisCollider.bounds.center.x, player.thisCollider.bounds.center.y + (player.thisCollider.bounds.extents.y * .9f), player.thisCollider.bounds.center.z);
                Vector3 feetCheck = new Vector3(player.thisCollider.bounds.center.x, player.thisCollider.bounds.center.y - (player.thisCollider.bounds.extents.y * .9f), player.thisCollider.bounds.center.z);

                raytargets.Add (headCheck);
                raytargets.Add(player.thisCollider.bounds.center);
                raytargets.Add(feetCheck);

                foreach (Vector3 target in raytargets)
                {
                    if (!thisEnemy.PlayerInSight)
                    {
                        if (Physics.Linecast(transform.position, target, out hit))
                        {
                            ConfirmPlayerIsInSight(hit);
                        }
                    }
                    else
                        break;
                }
            }
        }
	}

    bool drawDetectSphere = false;

    void ConfirmPlayerIsInSight (RaycastHit hit)
    {
        if (hit.transform.CompareTag("Player"))
        {
            drawDetectSphere = true;
            thisEnemy.PlayerInSight = true;
            Debug.DrawLine(transform.position, hit.point, Color.red);
            //Debug.Log("PLAYER IN SIGHT");
        }
    }

    private void OnDrawGizmos()
    {
        if (drawDetectSphere)
            Gizmos.DrawSphere(transform.position, .5f);
    }

}
