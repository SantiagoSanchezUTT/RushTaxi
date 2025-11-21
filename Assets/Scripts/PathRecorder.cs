using UnityEngine;
using System.Collections.Generic;

public class PathRecorder : MonoBehaviour
{
    [Header("Controles")]
    public KeyCode recordKey = KeyCode.Q;

    [Header("Configuración")]
    [Tooltip("Cada cuántos metros poner un punto. 10 es ideal para rectas.")]
    public float distanceStep = 10f;
    public string pathNameBase = "Ruta_Grabada_";

    private GameObject currentPathObj;
    private Vector3 lastPosition;
    private bool isRecording = false;
    private int pathCount = 0;

    void Update()
    {
        // TECLA R: Inicia o Corta la grabación actual
        if (Input.GetKeyDown(recordKey))
        {
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
                // Opcional: Reiniciar inmediatamente para seguir grabando el siguiente tramo
                // StartRecording(); 
            }
        }

        // LÓGICA DE GRABACIÓN (Mientras conduces)
        if (isRecording)
        {
            float dist = Vector3.Distance(transform.position, lastPosition);

            // Si avanzaste X metros, pone un punto
            if (dist >= distanceStep)
            {
                AddWaypoint();
            }
        }
    }

    void StartRecording()
    {
        isRecording = true;
        pathCount++;

        // Crear el objeto contenedor de la ruta
        currentPathObj = new GameObject(pathNameBase + pathCount);

        // Le añade el script WaypointPath automáticamente para que veas la línea amarilla
        currentPathObj.AddComponent<WaypointPath>();

        // Poner el primer punto YA
        AddWaypoint();

        Debug.Log($"🔴 GRABANDO {currentPathObj.name}... (Conduce!)");
    }

    void StopRecording()
    {
        isRecording = false;
        Debug.Log($"⏹ RUTA FINALIZADA. ¡No olvides COPIAR los objetos antes de quitar Play!");
        currentPathObj = null;
    }

    void AddWaypoint()
    {
        if (currentPathObj == null) return;

        // Crear el nodo en la posición actual del coche
        GameObject p = new GameObject("Node");
        p.transform.position = transform.position;
        p.transform.rotation = transform.rotation; // Guarda hacia donde miras

        // Hacerlo hijo de la ruta
        p.transform.SetParent(currentPathObj.transform);

        // Actualizar referencia
        lastPosition = transform.position;
    }
}