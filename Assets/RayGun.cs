using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RayGun : MonoBehaviour {

    [SerializeField]
    float normalRange = .5f;
    [SerializeField]
    float rangeComboMultiplier = 2;
    [SerializeField]
    float ComboStepDuration = 1;
    [SerializeField]
    int normalDamage = 1;
    [SerializeField]
    int damageComboAmount = 2;
    [SerializeField]
    LayerMask rayLayers;
    [SerializeField]
    float rayDuration = .3f;
    [SerializeField]
    float coolDownDuration = .5f;

    float rayTimer = 0;
    float comboTimer = 0;
    float coolDownTimer;
    bool rayActive = false;
    float currentRange;
    LineRenderer lineRenderer;
    int currentCombo = 0;

    Vector3 worldMousePos;

    List<GameObject> alreadyTouchedInThisShot = new List<GameObject>();

	// Use this for initialization
	void Start ()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        currentRange = normalRange;
        lineRenderer.enabled = false;
        coolDownTimer = coolDownDuration;
	}

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(worldMousePos, 1f);
    }

    void ComboTiming ()
    {
        if (currentCombo > 0)
        {
            if (comboTimer <= ComboStepDuration)
            {
                comboTimer += Time.deltaTime;
            }
            else
            {
                comboTimer = 0;
                currentCombo--;
            }
        }
        else
        {
            comboTimer = 0;
        }
    }

    void RangeCombo ()
    {
        currentRange = normalRange * rangeComboMultiplier;
    }

    // Update is called once per frame
    void Update ()
    {
        //Get Mouse Pos for Keyboard/Mouse control scheme

        worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3 (Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs (Camera.main.transform.position.z - transform.position.z)));
        worldMousePos.z = transform.position.z;

        //Debug.Log("Mouse screen pos = " + Input.mousePosition + ", world Pos = " + worldMousePos);

        Vector3 rayDirection = worldMousePos - transform.position;
        rayDirection = Vector3.Normalize(rayDirection);

        Debug.DrawLine(transform.position, worldMousePos, Color.black);

        if (Input.GetButtonDown("RayGun") && !rayActive && coolDownTimer >= coolDownDuration)
        {
            Debug.Log("PROJEEEEEET");
            rayActive = true;
            lineRenderer.enabled = true;
            coolDownTimer = 0;
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
                if (!alreadyTouchedInThisShot.Contains(hit.transform.gameObject))
                {
                    if (currentCombo < 3)
                    {
                        currentCombo++;
                        comboTimer = 0;
                    }

                    Debug.Log("RAYGUN HIT : " + hit.transform.name);

                    alreadyTouchedInThisShot.Add(hit.transform.gameObject);
                }
            }
            else
            {
                currentCombo = 0;
            }

            rayTimer += Time.deltaTime;
        }
        else if (rayTimer > rayDuration)
        {
            rayTimer = 0;
            rayActive = false;
            lineRenderer.enabled = false;
            alreadyTouchedInThisShot.Clear();
        }

        ComboTiming();

        if (currentCombo >= 1)
            RangeCombo();
        else
            currentRange = normalRange;

        if (coolDownTimer < coolDownDuration)
            coolDownTimer += Time.deltaTime;
        else
            coolDownTimer = coolDownDuration;
    }
}
