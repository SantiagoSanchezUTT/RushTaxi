using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Nombra el archivo "CameraGTA_Mejorada.cs"
public class CameraGTA_Mejorada : MonoBehaviour
{
    public Transform player;
    private Rigidbody playerRb; // Necesario para medir la velocidad
    private Camera cam;         // Necesario para el efecto de FOV

    [Header("Offsets")]
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float scrollSpeed = 2f;
    public float height = 2f;

    [Header("Rotation")]
    public float rotateSpeed = 5f;
    public float verticalAngleMin = -30f;
    public float verticalAngleMax = 60f;
    public float autoRotateSpeed = 3f;
    public float resetPitchValue = 10f;

    [Header("Auto Reset")]
    public float autoResetDelay = 2f;

    [Header("Collision Settings")]
    public LayerMask collisionLayers = ~0;
    public float cameraCollisionRadius = 0.3f;
    public float collisionSmooth = 10f;

    // --- ¡NUEVO! Efecto de Velocidad (FOV) ---
    [Header("Speed FX (Field of View)")]
    [Tooltip("El FOV base de la cámara (ej. 60)")]
    public float minFov = 60f;
    [Tooltip("El FOV máximo a alta velocidad (ej. 75)")]
    public float maxFov = 75f;
    [Tooltip("La velocidad (en m/s) a la que se alcanza el FOV máximo")]
    public float maxSpeedForFov = 30f;
    [Tooltip("Suavidad de la transición del FOV")]
    public float fovSmooth = 5f;

    // --- Variables Privadas ---
    private float yaw = 0f;
    private float pitch = 10f;
    private float currentDistance;
    private float timeSinceMouseMove = 0f;
    private Vector3 smoothCameraPosition;

    void Start()
    {
        // --- OBTENER COMPONENTES ---
        // Este script debe estar en el objeto de la Cámara
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Guarda el FOV inicial que pusiste en el editor
            minFov = cam.fieldOfView;
        }

        // El jugador debe tener un Rigidbody
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        currentDistance = (minDistance + maxDistance) / 2f;
        smoothCameraPosition = transform.position;
    }

    void LateUpdate()
    {
        // Si falta algo, no hacer nada
        if (player == null || playerRb == null || cam == null) return;

        // --- Zoom con scroll ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentDistance -= scroll * scrollSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // --- Movimiento del ratón ---
        float mouseInputX = Input.GetAxis("Mouse X");
        float mouseInputY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseInputX) > 0.01f || Mathf.Abs(mouseInputY) > 0.01f)
        {
            yaw += mouseInputX * rotateSpeed;
            pitch -= mouseInputY * rotateSpeed;
            timeSinceMouseMove = 0f;
        }
        else
        {
            timeSinceMouseMove += Time.deltaTime;

            if (timeSinceMouseMove >= autoResetDelay)
            {
                Quaternion targetRotation = Quaternion.LookRotation(player.forward);
                yaw = Mathf.LerpAngle(yaw, targetRotation.eulerAngles.y, Time.deltaTime * autoRotateSpeed);
                pitch = Mathf.Lerp(pitch, resetPitchValue, Time.deltaTime * autoRotateSpeed);
            }
        }

        pitch = Mathf.Clamp(pitch, verticalAngleMin, verticalAngleMax);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 targetPos = player.position + Vector3.up * height;
        Vector3 desiredCameraPos = targetPos + rotation * new Vector3(0, 0, -currentDistance);

        // --- 🔍 Detección de colisión MEJORADA ---
        RaycastHit hit;

        // --- ¡CORRECCIÓN DEL BUG! ---
        // Calcula si la cámara está detrás del jugador
        // (Compara el ángulo de la cámara 'yaw' con el ángulo del jugador 'player.eulerAngles.y')
        float yawDelta = Mathf.DeltaAngle(yaw, player.eulerAngles.y);
        bool isCameraBehindPlayer = Mathf.Abs(yawDelta) < 90f; // ¿Está la cámara en el hemisferio trasero?

        // Solo hacer la colisión si la cámara está DETRÁS
        if (isCameraBehindPlayer && Physics.SphereCast(targetPos, cameraCollisionRadius, desiredCameraPos - targetPos,
            out hit, currentDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            // Si choca, acerca la cámara
            float adjustedDistance = hit.distance - 0.1f;
            desiredCameraPos = targetPos + rotation * new Vector3(0, 0, -Mathf.Clamp(adjustedDistance, minDistance, maxDistance));
        }
        // Si la cámara está ADELANTE (mirando hacia atrás), no se hace colisión.
        // ¡Esto evita que choque con el suelo de enfrente!

        // --- Movimiento suave ---
        smoothCameraPosition = Vector3.Lerp(smoothCameraPosition, desiredCameraPos, Time.deltaTime * collisionSmooth);
        transform.position = smoothCameraPosition;
        transform.LookAt(targetPos);

        // --- 💨 ¡NUEVO! Efecto de Velocidad (FOV) ---
        // Obtiene la velocidad del Rigidbody (solo en el plano horizontal)
        Vector3 planarVelocity = new Vector3(playerRb.velocity.x, 0, playerRb.velocity.z);
        float currentSpeed = planarVelocity.magnitude;

        // Mapea la velocidad (de 0 a maxSpeedForFov) al rango de FOV (de minFov a maxFov)
        float targetFov = Mathf.Lerp(minFov, maxFov, Mathf.InverseLerp(0, maxSpeedForFov, currentSpeed));

        // Aplica el nuevo FOV suavemente
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovSmooth);
    }
}