using UnityEngine;

public class CarAI_Advanced : MonoBehaviour
{
    [Header("Ruta")]
    public WaypointPath route;
    public int currentIndex = 0;

    [Header("Movimiento")]
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float brakingForce = 15f; // Frenado más agresivo
    public float turnSpeed = 6f;

    [Header("Sensores (Ojos)")]
    public float detectionLength = 6f;
    [Tooltip("Ancho de visión. Si es 0, solo mira recto. Ponle 30 o 45.")]
    public float sensorAngle = 30f;
    public LayerMask obstacleMask;

    [Header("Sistema Anti-Atasco")]
    [Tooltip("Tiempo máximo permitido parado antes de desaparecer")]
    public float maxTimeStuck = 4.0f;
    private float stuckTimer = 0f;

    private float currentSpeed;

    void Start()
    {
        currentSpeed = maxSpeed;
        // Validación inicial
        if (route == null || route.transform.childCount == 0) Destroy(gameObject);
    }

    void Update()
    {
        if (route == null) return;

        // --- 1. SISTEMA ANTI-ATASCO (LA GRÚA) ---
        // Si la velocidad es casi cero, contamos el tiempo
        if (currentSpeed < 1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > maxTimeStuck)
            {
                // Opción visual: Podrías hacer que se encoja antes de desaparecer
                Destroy(gameObject);
                return; // Adiós problema
            }
        }
        else
        {
            // Si nos movemos, reseteamos el contador
            stuckTimer = 0f;
        }

        // --- 2. DETECCIÓN DE OBSTÁCULOS (3 RAYOS) ---
        bool isBlocked = CheckObstacles();

        // --- 3. FÍSICA SIMPLIFICADA ---
        if (isBlocked)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, brakingForce * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        }

        // --- 4. SEGUIR RUTA ---
        // Validación extra por si el nodo se borró
        if (currentIndex >= route.transform.childCount)
        {
            ChangeRouteOrDestroy();
            return;
        }

        Vector3 target = route.transform.GetChild(currentIndex).position;
        Vector3 dirToEnd = target - transform.position;
        dirToEnd.y = 0;

        if (dirToEnd != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToEnd);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }

        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // --- 5. CAMBIAR NODO ---
        if (Vector3.Distance(transform.position, target) < 4.5f) // Radio amplio para fluidez
        {
            currentIndex++;
            if (currentIndex >= route.transform.childCount)
            {
                ChangeRouteOrDestroy();
            }
        }
    }

    // Función para mirar con 3 ojos (Centro, Izq, Der)
    bool CheckObstacles()
    {
        Vector3 start = transform.position + Vector3.up * 0.5f;

        // Rayo Central
        bool center = Physics.Raycast(start, transform.forward, detectionLength, obstacleMask);

        // Rayo Izquierdo
        Vector3 dirLeft = Quaternion.Euler(0, -sensorAngle, 0) * transform.forward;
        bool left = Physics.Raycast(start, dirLeft, detectionLength * 0.7f, obstacleMask); // Un poco más cortos los laterales

        // Rayo Derecho
        Vector3 dirRight = Quaternion.Euler(0, sensorAngle, 0) * transform.forward;
        bool right = Physics.Raycast(start, dirRight, detectionLength * 0.7f, obstacleMask);

        // Debug visual (Solo en editor)
        Debug.DrawRay(start, transform.forward * detectionLength, center ? Color.red : Color.green);
        Debug.DrawRay(start, dirLeft * detectionLength * 0.7f, left ? Color.red : Color.yellow);
        Debug.DrawRay(start, dirRight * detectionLength * 0.7f, right ? Color.red : Color.yellow);

        return center || left || right;
    }

    void ChangeRouteOrDestroy()
    {
        if (route.nextConnectedPaths != null && route.nextConnectedPaths.Count > 0)
        {
            WaypointPath nextPath = route.nextConnectedPaths[Random.Range(0, route.nextConnectedPaths.Count)];
            route = nextPath;
            currentIndex = 0;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}