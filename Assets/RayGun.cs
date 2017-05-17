using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayGun : MonoBehaviour {

    [SerializeField]
    float normalRange = .5f;
    [SerializeField]
    float rangeComboMultiplier;
    [SerializeField]
    int currentCombo = 0;
    [SerializeField]
    float ComboStepDuration = 0;
    [SerializeField]
    int normalDamage;
    [SerializeField]
    int damageComboAmount;
    [SerializeField]
    LayerMask rayLayers;
    [SerializeField]
    float rayDuration = .3f;

    float rayTimer = 0;
    bool rayActive = false;
    float currentRange;
    LineRenderer lineRenderer;

    Vector3 worldMousePos;

	// Use this for initialization
	void Start ()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        currentRange = normalRange;
        lineRenderer.enabled = false;
	}

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(worldMousePos, 1f);
    }

    // Update is called once per frame
    void Update ()
    {
        //Get Mouse Pos for Keyboard/Mouse control scheme

        worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3 (Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs (Camera.main.transform.position.z - transform.position.z)));
        worldMousePos.z = transform.position.z;

        Debug.Log("Mouse screen pos = " + Input.mousePosition + ", world Pos = " + worldMousePos);

        Vector3 rayDirection = worldMousePos - transform.position;
        rayDirection = Vector3.Normalize(rayDirection);

        Debug.DrawLine(transform.position, worldMousePos, Color.black);

        if (Input.GetButtonDown("RayGun") && !rayActive)
        {
            Debug.Log("PROJEEEEEET");
            rayActive = true;
            lineRenderer.enabled = true;
        }

        if (rayActive && rayTimer <= rayDuration)
        {
            Ray ray = new Ray(transform.position, transform.right);
            RaycastHit hit;

            Debug.DrawRay(transform.position, rayDirection * currentRange, Color.red);

            Vector3[] positions = new Vector3[2];
            positions[0] = transform.position;
            positions[1] = positions[0] + rayDirection * currentRange;
            lineRenderer.SetPositions(positions);

            if (Physics.Raycast(ray, out hit, currentRange, rayLayers))
            {
                Debug.Log("RAYGUN HIT : " + hit.transform.name);
            }

            rayTimer += Time.deltaTime;
        }
        else if (rayTimer > rayDuration)
        {
            rayTimer = 0;
            rayActive = false;
            lineRenderer.enabled = false;
        }
	}
}
