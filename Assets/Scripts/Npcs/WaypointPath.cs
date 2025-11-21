using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WaypointPath : MonoBehaviour
{
    public enum PathMode
    {
        LineasRectas,
        CurvasSuaves
    }

    [Header("Modo de visualización")]
    public PathMode modo = PathMode.CurvasSuaves;

    [Header("Colores del camino")]
    public Color colorIda = Color.cyan;
    public Color colorVuelta = Color.magenta;

    [Header("Curvas (solo visual)")]
    [Range(2, 30)]
    public int smoothness = 10;

    [Tooltip("Mostrar también el camino de regreso")]
    public bool mostrarVuelta = false;

    // =========================================================
    // CORRECCIÓN: ESTA ES LA LISTA QUE NECESITA EL AUTO-CONECTOR
    // =========================================================
    [Header("Conexiones Automáticas")]
    public List<WaypointPath> nextConnectedPaths = new List<WaypointPath>();

    void OnDrawGizmos()
    {
        int count = transform.childCount;
        if (count < 2) return;

        // ================================
        // 1. DIBUJAR CAMINO INTERNO (IDA)
        // ================================
        Gizmos.color = colorIda;

        if (modo == PathMode.LineasRectas)
        {
            for (int i = 0; i < count - 1; i++)
            {
                Gizmos.DrawLine(
                    transform.GetChild(i).position,
                    transform.GetChild(i + 1).position
                );
            }
        }
        else
        {
            for (int i = 0; i < count - 1; i++)
            {
                Vector3 p0 = (i == 0) ? transform.GetChild(i).position : transform.GetChild(i - 1).position;
                Vector3 p1 = transform.GetChild(i).position;
                Vector3 p2 = transform.GetChild(i + 1).position;
                Vector3 p3 = (i + 2 < count) ? transform.GetChild(i + 2).position : p2;

                DibujarCurva(p0, p1, p2, p3);
            }
        }

        // ================================
        // 2. DIBUJAR VUELTA (OPCIONAL)
        // ================================
        if (mostrarVuelta)
        {
            Gizmos.color = colorVuelta;
            // ... (Lógica de vuelta simplificada para ahorrar espacio, usa la misma lógica inversa)
            if (modo == PathMode.LineasRectas)
            {
                for (int i = count - 1; i > 0; i--)
                    Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i - 1).position);
            }
            else
            {
                for (int i = count - 1; i > 0; i--)
                {
                    Vector3 p0 = (i + 1 < count) ? transform.GetChild(i + 1).position : transform.GetChild(i).position;
                    Vector3 p1 = transform.GetChild(i).position;
                    Vector3 p2 = transform.GetChild(i - 1).position;
                    Vector3 p3 = (i - 2 >= 0) ? transform.GetChild(i - 2).position : p2;
                    DibujarCurva(p0, p1, p2, p3);
                }
            }
        }

        // ================================
        // 3. DIBUJAR PUNTOS (ESFERAS)
        // ================================
        Gizmos.color = Color.yellow;
        for (int i = 0; i < count; i++)
        {
            Gizmos.DrawSphere(transform.GetChild(i).position, 0.3f);
        }

        // ================================
        // 4. DIBUJAR CONEXIONES (AZUL) - ¡ESTO ES NUEVO!
        // ================================
        if (nextConnectedPaths != null && nextConnectedPaths.Count > 0)
        {
            Gizmos.color = Color.blue;
            Vector3 myLastPoint = transform.GetChild(count - 1).position;

            foreach (var nextPath in nextConnectedPaths)
            {
                if (nextPath != null && nextPath.transform.childCount > 0)
                {
                    Vector3 nextFirstPoint = nextPath.transform.GetChild(0).position;
                    Gizmos.DrawLine(myLastPoint, nextFirstPoint);
                    Gizmos.DrawWireSphere(nextFirstPoint, 0.5f); // Marcador de conexión
                }
            }
        }
    }

    // ============================
    // MÉTODO PARA LA IA
    // ============================
    public Vector3 GetWaypointPosition(int index)
    {
        int count = transform.childCount;

        if (count == 0) return transform.position;

        index = Mathf.Clamp(index, 0, count - 1);
        Vector3 p1 = transform.GetChild(index).position;

        if (modo == PathMode.LineasRectas) return p1;

        if (index == count - 1 || index == 0) return p1;

        Vector3 p0 = transform.GetChild(index - 1).position;
        Vector3 p2 = transform.GetChild(index + 1).position;

        return GetCatmullRomPosition(0.5f, p0, p1, p2, p2);
    }

    // =============================
    // MATEMÁTICAS CURVAS
    // =============================
    void DibujarCurva(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 anterior = p1;
        for (int i = 1; i <= smoothness; i++)
        {
            float t = i / (float)smoothness;
            Vector3 siguiente = GetCatmullRomPosition(t, p0, p1, p2, p3);
            Gizmos.DrawLine(anterior, siguiente);
            anterior = siguiente;
        }
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}