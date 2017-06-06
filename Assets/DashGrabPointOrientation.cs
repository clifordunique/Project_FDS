using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DashGrabPointOrientation : MonoBehaviour {

    [SerializeField]
    float grabCoolDown = 1f;

    [HideInInspector]
    public float coolDownTimer = 0f;

    SpriteRenderer spriteRenderer;
    Vector3 startPosition;
    GameObject player;
    Player playerClass;
    DashGrabPointOrientation grabbedScript;

    bool readyToGrab = true;

	// Use this for initialization
	void Start ()
    {
        spriteRenderer = gameObject.GetComponentInParent<SpriteRenderer>();
        startPosition = transform.localPosition;
        player = GameObject.FindGameObjectWithTag("Player");
        playerClass = player.GetComponent<Player>();
        Physics.IgnoreCollision(this.GetComponent<Collider>(), transform.parent.Find("HitBox").GetComponent<Collider>());
        coolDownTimer = grabCoolDown;
	}
	
	// Update is called once per frame
	void Update ()
    {
        //Debug.Log(transform.name + " grab cool down timer = " + coolDownTimer);

        if (spriteRenderer.flipX == true)
            transform.localPosition = new Vector3(-startPosition.x, startPosition.y, startPosition.z);
        else
            transform.localPosition = startPosition;

        if (playerClass.dashAttachment == null)
        {
            coolDownTimer += Time.deltaTime;

            if (coolDownTimer > grabCoolDown)
                coolDownTimer = grabCoolDown;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger hit" + other.transform.name);
        if (other.CompareTag("Player") && coolDownTimer >= grabCoolDown)
        {
            Debug.Log("Yes it's player yes hello");
            if (playerClass.dashing && playerClass.dashAttachment == null && !playerClass.canDashFromAttachment)
            {
                Debug.Log("BLBLBLBLB");
                playerClass.dashAttachment = gameObject;
                playerClass.canDashFromAttachment = true;
                playerClass.attachmentColliders = gameObject.GetComponentsInChildren<Collider>().ToList();

                playerClass.StopAndResetDashNGrab(true);

                foreach (Collider collider in playerClass.attachmentColliders)
                {
                    Physics.IgnoreCollision(playerClass.thisCollider, collider, true);
                }
            }
        }
    }
}
