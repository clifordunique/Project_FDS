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
    int currentDamage = 1;
    bool rayActive = false;
    float currentRange;
    LineRenderer lineRenderer;
    int currentCombo = 0;
    bool secondaryFire = false;

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

    Vector3 Aiming ()
    {
        //Get Mouse Pos for Keyboard/Mouse control scheme

        worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(Camera.main.transform.position.z - transform.position.z)));
        worldMousePos.z = transform.position.z;

        //Debug.Log("Mouse screen pos = " + Input.mousePosition + ", world Pos = " + worldMousePos);

        Vector3 rayDirection = worldMousePos - transform.position;
        rayDirection = Vector3.Normalize(rayDirection);

        //Debug.DrawLine(transform.position, worldMousePos, Color.black);

        return rayDirection;
    }

    void FiringRay (Vector3 rayDirection)
    {
        Ray ray = new Ray(transform.position, rayDirection * currentRange);


        Debug.DrawRay(transform.position, rayDirection * currentRange, Color.black);

        Vector3[] positions = new Vector3[2];
        positions[0] = transform.position;
        positions[1] = positions[0] + rayDirection * currentRange;
        lineRenderer.SetPositions(positions);

        RaycastHit[] hit = Physics.RaycastAll(ray, currentRange, rayLayers);

        if (hit.Length > 0)
        {
            //Touched enemy or energized device
            foreach (RaycastHit singleHit in hit)
            {
                if (!alreadyTouchedInThisShot.Contains(singleHit.transform.gameObject))
                {
                    if (currentCombo < 2)
                    {
                        currentCombo++;
                        comboTimer = 0;
                    }

                    Debug.Log("RAYGUN HIT : " + singleHit.transform.name);

                    Enemy hitEnemy = singleHit.transform.GetComponent<Enemy>();

                    if (hitEnemy != null)
                    {
                        if (!secondaryFire)
                            hitEnemy.GetDamage(currentDamage);
                        else if (secondaryFire && !hitEnemy.energized)
                            hitEnemy.ReEnergize();
                    }

                    alreadyTouchedInThisShot.Add(singleHit.transform.gameObject);
                }
            }
        }
        else
        {
            currentCombo = 0;
        }

        rayTimer += Time.deltaTime;
    }

    void ComboParameters ()
    {
        if (currentCombo >= 1)
            RangeCombo();
        else
            currentRange = normalRange;

        if (currentCombo >= 2)
            currentDamage = damageComboAmount;
        else
            currentDamage = normalDamage;

        if (coolDownTimer < coolDownDuration)
            coolDownTimer += Time.deltaTime;
        else
            coolDownTimer = coolDownDuration;
    }

    // Update is called once per frame
    void FixedUpdate ()
    {
        Vector3 rayDirection = Aiming();

        if (Input.GetButtonDown("RayGun") || Input.GetButton("RayGunSecondary"))
        {
            if (!rayActive && coolDownTimer >= coolDownDuration)
            {
                rayActive = true;
                lineRenderer.enabled = true;

                coolDownTimer = 0;

                if (Input.GetButtonDown("RayGun"))
                {
                    lineRenderer.startColor = Color.red;
                    lineRenderer.endColor = Color.red;
                    secondaryFire = false;
                }
                else if (Input.GetButtonDown("RayGunSecondary"))
                {
                    //lineRenderer.colorGradient = Gradient.;
                    lineRenderer.startColor = Color.green;
                    lineRenderer.endColor = Color.green;
                    secondaryFire = true;
                }
            }
        }

        if (rayActive && rayTimer <= rayDuration)
        {
            FiringRay(rayDirection);
        }
        else if (rayTimer > rayDuration)
        {
            rayTimer = 0;
            rayActive = false;
            lineRenderer.enabled = false;
            alreadyTouchedInThisShot.Clear();
        }

        ComboTiming();
        ComboParameters();
    }
}
