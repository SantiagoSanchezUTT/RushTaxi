using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GtaCamera : MonoBehaviour
{
    public Transform player;

    [Header("Offsets")]
    public float minDistance = 2f;     // distancia mínima (zoom in)
    public float maxDistance = 10f;    // distancia máxima (zoom out)
    public float scrollSpeed = 2f;     // sensibilidad del scroll
    public float height = 2f;          // altura fija sobre el jugador

    [Header("Rotation")]
    public float rotateSpeed = 5f;
    public float verticalAngleMin = -30f;
    public float verticalAngleMax = 60f;

    private float yaw = 0f;
    private float pitch = 10f;
    private float currentDistance;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentDistance = (minDistance + maxDistance) / 2f; // valor inicial
    }

    void LateUpdate()
    {
        if (player == null) return;

        // --- Rotación con mouse ---
        yaw += Input.GetAxis("Mouse X") * rotateSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
        pitch = Mathf.Clamp(pitch, verticalAngleMin, verticalAngleMax);

        // --- Zoom con scroll ---
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // positivo/negativo
        currentDistance -= scroll * scrollSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // --- Posición y rotación de la cámara ---
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 targetPos = player.position + Vector3.up * height;
        Vector3 cameraOffset = rotation * new Vector3(0, 0, -currentDistance);

        Camera.main.transform.position = targetPos + cameraOffset;
        Camera.main.transform.LookAt(targetPos);
    }
}
