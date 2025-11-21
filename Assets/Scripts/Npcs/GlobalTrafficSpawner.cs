using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    [Header("Vehículos")]
    public List<GameObject> npcCarPrefabs = new List<GameObject>();

    [Header("Rutas (Detectadas automáticamente)")]
    public List<WaypointPath> routes = new List<WaypointPath>();

    [Header("Límites de tráfico")]
    public int maxVehicles = 100;
    public float spawnInterval = 2f; // Bajado a 2 para que llene más rápido
    public float despawnDistance = 150f; // Subido un poco para que no desaparezcan en la cara
    public float npcLifetime = 60f;

    private List<CarAI_Advanced> activeCars = new List<CarAI_Advanced>();
    private float spawnTimer = 0f;

    void Start()
    {
        // ==========================================
        //      DETECTAR RUTAS AUTOMÁTICAMENTE
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

        // DESPAWN (Limpieza)
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            var car = activeCars[i];

            // Si el coche se destruyó solo (por el script anti-atasco), lo sacamos de la lista
            if (car == null)
            {
                activeCars.RemoveAt(i);
                continue;
            }

            // Calcular distancia (Usamos la posición de ESTE objeto TrafficManager)
            // RECOMENDACIÓN: Pon este objeto TrafficManager hijo de tu Coche Jugador 
            // o cerca del centro de la acción.
            float dist = Vector3.Distance(transform.position, car.transform.position);

            if (dist > despawnDistance/* || car.lifeTimer >= npcLifetime*/)
            {
                Destroy(car.gameObject);
                activeCars.RemoveAt(i);
            }
        }
    }

    // ============================================================
    // SPAWNEAR NPC CON SEGURIDAD
    // ============================================================
    void SpawnRandomCar()
    {
        if (npcCarPrefabs.Count == 0 || routes.Count == 0) return;

        // 1. Elegir ruta y punto
        WaypointPath route = routes[Random.Range(0, routes.Count)];
        if (route == null || route.transform.childCount < 2) return;

        // Elegimos un punto al azar (menos el último para poder orientarlo)
        int wpIndex = Random.Range(0, route.transform.childCount - 1);

        Transform wp = route.transform.GetChild(wpIndex);
        Transform next = route.transform.GetChild(wpIndex + 1);

        Vector3 pos = wp.position;

        // --- NUEVO: SEGURIDAD ANTI-CHOQUE ---
        // Verifica si hay algo en un radio de 3 metros. Si hay, cancela el spawn.
        if (Physics.CheckSphere(pos, 3f))
        {
            // Si está ocupado, abortamos esta vez para no crear un accidente
            return;
        }

        // Calcular rotación mirando al siguiente punto
        Quaternion rot = Quaternion.LookRotation((next.position - wp.position).normalized);

        // 2. Crear coche
        GameObject npcCarPrefab = npcCarPrefabs[Random.Range(0, npcCarPrefabs.Count)];
        GameObject carObj = Instantiate(npcCarPrefab, pos, rot);

        // 3. Configurar IA
        CarAI_Advanced ai = carObj.GetComponent<CarAI_Advanced>();
        ai.route = route;
        ai.currentIndex = wpIndex;
       // ai.lifeTimer = 0f;

        activeCars.Add(ai);
    }
}