using UnityEngine;
using System.Collections.Generic;

public class AutoTrafficNetwork : MonoBehaviour
{
    [Header("Ajustes de Precisión")]
    [Tooltip("Distancia máxima. BÁJALO a 6 o 8 para evitar conectar calles lejanas.")]
    public float connectionRadius = 8.0f;

    [Tooltip("Ángulo máximo. 30-45 es ideal. Evita que se conecte con calles que cruzan o van en contra.")]
    public float maxAngleDiff = 45.0f;

    void Start()
    {
        // 1. PRIMERO LIMPIAMOS EL DESASTRE ANTERIOR
        LimpiarConexiones();

        // 2. LUEGO CONECTAMOS CON PRECISIÓN QUIRÚRGICA
        ConnectRoutesStrictly();
    }

    void LimpiarConexiones()
    {
        WaypointPath[] allPaths = FindObjectsOfType<WaypointPath>();
        foreach (var path in allPaths)
        {
            if (path.nextConnectedPaths != null)
                path.nextConnectedPaths.Clear();
        }
        Debug.Log("🧹 Conexiones antiguas borradas.");
    }

    void ConnectRoutesStrictly()
    {
        WaypointPath[] allPaths = FindObjectsOfType<WaypointPath>();
        int connectionsMade = 0;

        Debug.Log("🔄 Iniciando conexión ESTRICTA...");

        foreach (WaypointPath pathA in allPaths)
        {
            try
            {
                if (pathA == null || pathA.transform.childCount < 2) continue;

                // Inicializar lista
                if (pathA.nextConnectedPaths == null) pathA.nextConnectedPaths = new List<WaypointPath>();

                // Vectores de referencia Ruta A (Final)
                Transform endNodeA = pathA.transform.GetChild(pathA.transform.childCount - 1);
                Transform preEndNodeA = pathA.transform.GetChild(pathA.transform.childCount - 2);
                Vector3 directionA = (endNodeA.position - preEndNodeA.position).normalized;

                foreach (WaypointPath pathB in allPaths)
                {
                    if (pathB == null || pathA == pathB || pathB.transform.childCount < 2) continue;

                    // Vectores de referencia Ruta B (Inicio)
                    Transform startNodeB = pathB.transform.GetChild(0);
                    Transform postStartNodeB = pathB.transform.GetChild(1);
                    Vector3 directionB = (postStartNodeB.position - startNodeB.position).normalized;

                    // --- FILTRO 1: DISTANCIA (Radio corto) ---
                    float dist = Vector3.Distance(endNodeA.position, startNodeB.position);
                    if (dist > connectionRadius) continue;

                    // --- FILTRO 2: ALINEACIÓN (Que vayan hacia el mismo lado) ---
                    // Si el ángulo entre las direcciones es mayor a 45 grados, es un cruce o sentido contrario.
                    float angle = Vector3.Angle(directionA, directionB);
                    if (angle > maxAngleDiff) continue;

                    // --- FILTRO 3: POSICIÓN RELATIVA (Que esté DELANTE, no al lado) ---
                    // Trazamos una línea desde A hacia B. ¿Esa línea coincide con hacia donde miraba A?
                    Vector3 directionToB = (startNodeB.position - endNodeA.position).normalized;
                    float angleToTarget = Vector3.Angle(directionA, directionToB);

                    // Si B está "al lado" (ej. carril paralelo), el ángulo será grande (ej. 90°). Lo rechazamos.
                    if (angleToTarget > 60) continue;

                    // ¡SI PASA TODO, CONECTAMOS!
                    if (!pathA.nextConnectedPaths.Contains(pathB))
                    {
                        pathA.nextConnectedPaths.Add(pathB);
                        connectionsMade++;
                    }
                }
            }
            catch (System.Exception) { continue; }
        }

        Debug.Log($"✅ ¡Mapa limpio! Conexiones precisas creadas: {connectionsMade}");
    }
}