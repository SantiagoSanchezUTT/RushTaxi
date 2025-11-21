using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    public Transform player;
    public float fixedY = 90f; // Altura fija (eje Y)
    public float smoothing = 5f; // Factor de suavizado (ajusta esto en el Inspector: 1f=lento, 10f=rápido)
    private float fixedZ;

    void Start()
    {
        // Guardamos la posición Z inicial de la cámara
        fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        // 1. Definir la POSICIÓN OBJETIVO sin sacudidas
        Vector3 targetPosition = new Vector3(
            player.position.x,     // Tomamos la X del coche
            fixedY,                // Mantenemos la Y (Altura) fija
            fixedZ                 // Mantenemos la Z (Profundidad) fija
        );

        // 2. Aplicar Suavizado (Lerp)
        // La cámara se moverá gradualmente hacia targetPosition.
        transform.position = Vector3.Lerp(
            transform.position,     // Posición actual de la cámara
            targetPosition,         // Posición a la que queremos llegar
            Time.deltaTime * smoothing // Velocidad de suavizado
        );

        // 3. FIJAR LA ROTACIÓN (Para que no se voltee con el coche)
        // Mantiene la vista superior.
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}