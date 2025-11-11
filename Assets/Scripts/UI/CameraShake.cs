using UnityEngine;
using System.Collections; // Necesario para la corutina

public class CameraShake : MonoBehaviour
{
    [Tooltip("El script de tu cámara principal (arrástralo aquí)")]
    public CameraGTA_Mejorada cameraScript;

    [Tooltip("Velocidad mínima (en m/s) para empezar a vibrar")]
    public float minSpeedForShake = 25f; // (Equivale a 90 km/h)

    [Tooltip("Máxima intensidad de la vibración")]
    public float maxShakeMagnitude = 0.2f;

    [Tooltip("Máxima velocidad de la vibración (qué tan rápido tiembla)")]
    public float maxShakeFrequency = 20f;

    // --- Variables Privadas ---
    private Rigidbody playerRb;
    private Vector3 originalLocalPosition; // Posición original de la cámara
    private bool isShaking = false;

    void Start()
    {
        // Asegurarse de que el script de cámara esté asignado
        if (cameraScript == null)
        {
            Debug.LogError("¡Asigna el script 'CameraGTA_Mejorada' en el Inspector!");
            return;
        }

        // Obtener el Rigidbody desde el script de cámara
        playerRb = cameraScript.player.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        // Si todo está correcto, revisa la velocidad
        if (playerRb != null)
        {
            // Calcula la velocidad (solo horizontal)
            Vector3 planarVelocity = new Vector3(playerRb.velocity.x, 0, playerRb.velocity.z);
            float currentSpeed = planarVelocity.magnitude;

            // Comprueba si debemos empezar a vibrar
            if (currentSpeed > minSpeedForShake && !isShaking)
            {
                // ¡Empezar a vibrar!
                StartCoroutine(Shake(currentSpeed));
            }
            // Comprueba si debemos parar de vibrar
            else if (currentSpeed <= minSpeedForShake && isShaking)
            {
                // ¡Parar de vibrar!
                StopAllCoroutines();
                isShaking = false;
                // Resetea la posición de la cámara (opcional, pero limpio)
                transform.localPosition = originalLocalPosition;
            }
        }
    }

    IEnumerator Shake(float currentSpeed)
    {
        isShaking = true;
        // Guarda la posición local "limpia" que calculó el otro script
        originalLocalPosition = transform.localPosition;

        while (currentSpeed > minSpeedForShake)
        {
            // 1. Calcular la intensidad actual basada en la velocidad
            // (Usa InverseLerp para obtener un valor 0-1 de la velocidad)
            float speedPercent = Mathf.InverseLerp(minSpeedForShake, cameraScript.maxSpeedForFov, currentSpeed);
            float currentMagnitude = speedPercent * maxShakeMagnitude;
            float currentFrequency = speedPercent * maxShakeFrequency;

            // 2. Generar una posición aleatoria para la vibración
            // (Usamos PerlinNoise para un temblor más suave y orgánico que Random.insideUnitSphere)
            float x = (Mathf.PerlinNoise(Time.time * currentFrequency, 0f) * 2f - 1f) * currentMagnitude;
            float y = (Mathf.PerlinNoise(0f, Time.time * currentFrequency) * 2f - 1f) * currentMagnitude;

            // 3. Aplicar la vibración
            // (Sumamos la vibración a la posición que el script principal YA calculó)
            transform.localPosition = originalLocalPosition + new Vector3(x, y, 0);

            // 4. Actualizar la velocidad (para salir del bucle si frenamos)
            currentSpeed = new Vector3(playerRb.velocity.x, 0, playerRb.velocity.z).magnitude;

            // 5. Esperar al siguiente frame
            yield return null;
        }

        // El bucle terminó (frenamos), así que paramos de vibrar
        isShaking = false;
        transform.localPosition = originalLocalPosition; // Reset
    }
}