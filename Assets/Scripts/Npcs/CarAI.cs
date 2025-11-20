using UnityEngine;
using System.Collections.Generic;

public class CarAI_Advanced : MonoBehaviour
{
    [Header("Ruta del vehículo")]
    public WaypointPath route;
    public int currentIndex = 0;

    [Header("Movimiento")]
    public float maxSpeed = 18f;
    public float minSpeed = 2f;
    public float acceleration = 6f;
    public float brakingForce = 8f;
    public float turnSpeed = 4.5f;
    private float currentSpeed;

    [Header("Detección")]
    public float safeDistance = 6f;
    public float detectionLength = 10f;
    public LayerMask obstacleMask;
    public float sideDetectionAngle = 25f;
    public float laneChangeCooldown = 3f;

    private float lastLaneChange = 0f;
    private bool isAvoiding = false;
    private Vector3 avoidanceDir;

    // ============================
    //   LIFE TIMER AÑADIDO AQUÍ
    // ============================
    [HideInInspector]
    public float lifeTimer = 0f;

    void Start()
    {
        currentSpeed = Random.Range(minSpeed, maxSpeed);

        if (route == null || route.transform.childCount == 0)
        {
            Debug.LogError("Ruta no asignada en " + gameObject.name);
            enabled = false;
        }
    }

    void Update()
    {
        // AUMENTA EL TIEMPO VIVO DEL NPC
        lifeTimer += Time.deltaTime;

        if (route == null || route.transform.childCount == 0) return;

        // Obtener punto objetivo (curva suavizada)
        Vector3 target = route.GetWaypointPosition(currentIndex);

        Vector3 dir = (target - transform.position).normalized;

        // RAYCASTS
        bool frontBlocked = Physics.Raycast(
            transform.position + transform.forward * 1.5f,
            transform.forward,
            out RaycastHit frontHit,
            detectionLength,
            obstacleMask);

        bool leftBlocked = Physics.Raycast(
            transform.position + transform.forward * 1.5f,
            Quaternion.Euler(0, -sideDetectionAngle, 0) * transform.forward,
            out RaycastHit leftHit,
            detectionLength / 1.5f,
            obstacleMask);

        bool rightBlocked = Physics.Raycast(
            transform.position + transform.forward * 1.5f,
            Quaternion.Euler(0, sideDetectionAngle, 0) * transform.forward,
            out RaycastHit rightHit,
            detectionLength / 1.5f,
            obstacleMask);

        // COMPORTAMIENTO
        if (frontBlocked && frontHit.collider.CompareTag("NPC"))
        {
            currentSpeed = Mathf.Lerp(currentSpeed, minSpeed, brakingForce * Time.deltaTime);

            if (Time.time > lastLaneChange + laneChangeCooldown)
            {
                if (!leftBlocked) StartCoroutine(ChangeLane(-1));
                else if (!rightBlocked) StartCoroutine(ChangeLane(1));
            }
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        }

        // MOVIMIENTO
        Vector3 moveDir = isAvoiding ? avoidanceDir : dir;
        Quaternion lookRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // CAMBIO DE WAYPOINT
        if (Vector3.Distance(transform.position, target) < 2f)
        {
            currentIndex = (currentIndex + 1) % route.transform.childCount;
        }
    }

    private System.Collections.IEnumerator ChangeLane(int direction)
    {
        isAvoiding = true;
        lastLaneChange = Time.time;
        float avoidTime = Random.Range(1.0f, 1.5f);
        float timer = 0f;

        avoidanceDir = (Quaternion.Euler(0, direction * 20f, 0) * transform.forward).normalized;

        while (timer < avoidTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isAvoiding = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * detectionLength);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Quaternion.Euler(0, -sideDetectionAngle, 0) * transform.forward) * detectionLength / 1.5f);
        Gizmos.DrawLine(transform.position, transform.position + (Quaternion.Euler(0, sideDetectionAngle, 0) * transform.forward) * detectionLength / 1.5f);
    }
}
