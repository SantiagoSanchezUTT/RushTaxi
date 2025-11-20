using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    [Header("Vehículos")]
    public List<GameObject> npcCarPrefabs = new List<GameObject>();   // NUEVO: múltiples prefabs

    [Header("Rutas (Detectadas automáticamente)")]
    public List<WaypointPath> routes = new List<WaypointPath>();      // Igual que antes, pero llenada automáticamente

    [Header("Límites de tráfico")]
    public int maxVehicles = 100;
    public float spawnInterval = 3f;
    public float despawnDistance = 120f;
    public float npcLifetime = 60f;

    private List<CarAI_Advanced> activeCars = new List<CarAI_Advanced>();
    private float spawnTimer = 0f;

    void Start()
    {
        // ==========================================
        //     DETECTAR RUTAS AUTOMÁTICAMENTE
        // ==========================================
        routes.Clear();
        WaypointPath[] foundRoutes = FindObjectsOfType<WaypointPath>();

        foreach (var r in foundRoutes)
            routes.Add(r);

        if (routes.Count == 0)
            Debug.LogWarning("⚠ No se encontraron rutas (WaypointPath) en la escena.");
        else
            Debug.Log("✔ Rutas detectadas: " + routes.Count);
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;

        // RESPAWN
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            if (activeCars.Count < maxVehicles)
                SpawnRandomCar();
        }

        // DESPAWN
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            var car = activeCars[i];

            if (car == null)
            {
                activeCars.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(transform.position, car.transform.position);

            if (dist > despawnDistance || car.lifeTimer >= npcLifetime)
            {
                Destroy(car.gameObject);
                activeCars.RemoveAt(i);
            }
        }
    }

    // ============================================================
    // SPAWNEAR NPC EN RUTA ALEATORIA
    // ============================================================
    void SpawnRandomCar()
    {
        if (npcCarPrefabs.Count == 0 || routes.Count == 0) return;

        // Elegir prefab aleatorio
        GameObject npcCarPrefab = npcCarPrefabs[Random.Range(0, npcCarPrefabs.Count)];

        WaypointPath route = routes[Random.Range(0, routes.Count)];

        if (route.transform.childCount < 2) return;

        int wpIndex = Random.Range(0, route.transform.childCount - 1);

        Transform wp = route.transform.GetChild(wpIndex);
        Transform next = route.transform.GetChild(wpIndex + 1);

        Vector3 pos = wp.position + Vector3.up * 0.2f;
        Quaternion rot = Quaternion.LookRotation((next.position - wp.position).normalized);

        GameObject carObj = Instantiate(npcCarPrefab, pos, rot);

        CarAI_Advanced ai = carObj.GetComponent<CarAI_Advanced>();
        ai.route = route;
        ai.currentIndex = wpIndex;
        ai.lifeTimer = 0f;

        activeCars.Add(ai);
    }
}
