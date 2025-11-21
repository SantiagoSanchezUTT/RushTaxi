using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class TaxiGameManager : MonoBehaviour
{
    public static TaxiGameManager Instance;

    [Header("Configuración Básica")]
    public Transform playerCar;

    [Header("Ubicaciones")]
    public Transform[] pickupPoints;
    public Transform[] dropOffPoints;

    public GameObject passengerPrefab;
    public GameObject destinationZonePrefab;

    [Header("Interfaz (UI)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI scoreText; // <--- ¡NUEVO! Arrastra aquí tu texto de puntaje

    [Header("Balance de Juego")]
    public float timePerUnitDistance = 0.5f;
    public float baseTimeBonus = 10.0f;

    [Header("Configuración de Dificultad")]
    public float maxPickupSearchRadius = 150f;
    public float easyDistanceCap = 200f;
    public float mediumDistanceCap = 500f;

    [Header("Progresión")]
    public int hardModeThreshold = 10;

    [Header("Estado del Juego")]
    public bool isMissionActive = false;
    public bool hasPassenger = false;
    public float currentTimer = 0;
    public int completedTrips = 0;
    public int highScore = 0; // <--- Para guardar el récord

    private GameObject currentPassengerObj;
    private GameObject currentDestinationObj;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Debug.Log("🚖 SISTEMA LISTO");

        // Cargar el Récord guardado (si no existe, es 0)
        highScore = PlayerPrefs.GetInt("TaxiHighScore", 0);

        if (infoText != null) infoText.text = "Presiona '2' para TAXI";
        if (timerText != null) timerText.text = "--";

        UpdateScoreUI(); // Mostrar ceros al inicio
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleMissionMode();

        if (!isMissionActive) return;

        if (hasPassenger)
        {
            currentTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = currentTimer.ToString("F1");

            if (currentTimer <= 0) GameOver();
        }
    }

    public void ToggleMissionMode()
    {
        if (isMissionActive) StopMission();
        else StartMission();
    }

    void StartMission()
    {
        if (pickupPoints.Length == 0 || dropOffPoints.Length == 0)
        {
            Debug.LogError("⚠️ Faltan puntos en el Inspector.");
            return;
        }

        isMissionActive = true;
        hasPassenger = false;
        completedTrips = 0;
        UpdateScoreUI(); // Reiniciar contador visual a 0

        Debug.Log("--- 🟢 MISIÓN DE TAXI INICIADA ---");
        SpawnNewPassenger();
    }

    void StopMission()
    {
        isMissionActive = false;
        hasPassenger = false;
        currentTimer = 0;

        if (infoText != null) infoText.text = "LIBRE (Presiona 2)";
        if (timerText != null) timerText.text = "OFF";

        if (currentPassengerObj != null) Destroy(currentPassengerObj);
        if (currentDestinationObj != null) Destroy(currentDestinationObj);

        Debug.Log("--- 🔴 MISIÓN CANCELADA ---");
    }

    // --- LÓGICA DE JUEGO ---

    public void SpawnNewPassenger()
    {
        if (!isMissionActive) return;

        List<Transform> nearbyPoints = pickupPoints
            .Where(p => Vector3.Distance(playerCar.position, p.position) <= maxPickupSearchRadius)
            .ToList();

        Transform selectedSpawn;

        if (nearbyPoints.Count > 0)
            selectedSpawn = nearbyPoints[Random.Range(0, nearbyPoints.Count)];
        else
            selectedSpawn = pickupPoints[Random.Range(0, pickupPoints.Length)];

        currentPassengerObj = Instantiate(passengerPrefab, selectedSpawn.position, Quaternion.identity);

        string zoneName = GetZoneNameFromPosition(selectedSpawn.position);

        if (infoText != null) infoText.text = "Recoger en: " + zoneName;
        if (timerText != null) timerText.text = "Espera";

        Debug.Log($"📍 Pasajero generado en: {selectedSpawn.name} (Barrio: {zoneName})");
    }

    public void PickupPassenger()
    {
        if (!isMissionActive) return;

        hasPassenger = true;
        if (currentPassengerObj != null) Destroy(currentPassengerObj);

        Debug.Log("🚕 ¡Pasajero Recogido!");
        GenerateSmartDestination();
    }

    void GenerateSmartDestination()
    {
        Difficulty level = Difficulty.Easy;
        float rand = Random.value;

        if (completedTrips < 3) level = Difficulty.Easy;
        else if (completedTrips < hardModeThreshold)
        {
            if (rand < 0.25f) level = Difficulty.Easy;
            else if (rand < 0.75f) level = Difficulty.Medium;
            else level = Difficulty.Hard;
        }
        else
        {
            if (rand < 0.05f) level = Difficulty.Easy;
            else if (rand < 0.45f) level = Difficulty.Medium;
            else level = Difficulty.Hard;
        }

        List<Transform> validDestinations = new List<Transform>();

        foreach (Transform point in dropOffPoints)
        {
            float dist = Vector3.Distance(playerCar.position, point.position);
            switch (level)
            {
                case Difficulty.Easy:
                    if (dist <= easyDistanceCap) validDestinations.Add(point); break;
                case Difficulty.Medium:
                    if (dist > easyDistanceCap && dist <= mediumDistanceCap) validDestinations.Add(point); break;
                case Difficulty.Hard:
                    if (dist > mediumDistanceCap) validDestinations.Add(point); break;
            }
        }

        Transform selectedDest;
        if (validDestinations.Count > 0)
            selectedDest = validDestinations[Random.Range(0, validDestinations.Count)];
        else
        {
            Debug.LogWarning($"⚠️ Fallback activado para dificultad {level}.");
            selectedDest = dropOffPoints[Random.Range(0, dropOffPoints.Length)];
        }

        currentDestinationObj = Instantiate(destinationZonePrefab, selectedDest.position, Quaternion.identity);

        float distance = Vector3.Distance(playerCar.position, selectedDest.position);
        currentTimer = (distance * timePerUnitDistance) + baseTimeBonus;

        if (infoText != null) infoText.text = $"Llevar a: {selectedDest.name}";

        Debug.Log($"🏁 Destino: {selectedDest.name} | Distancia: {distance:F1}");
    }

    public void DropOffPassenger()
    {
        if (!isMissionActive) return;

        hasPassenger = false;
        completedTrips++; // Sumar viaje

        // --- GUARDADO DE RÉCORD ---
        if (completedTrips > highScore)
        {
            highScore = completedTrips;
            PlayerPrefs.SetInt("TaxiHighScore", highScore); // Guardar en disco
            PlayerPrefs.Save();
        }

        UpdateScoreUI(); // Actualizar texto

        if (currentDestinationObj != null) Destroy(currentDestinationObj);

        if (infoText != null) infoText.text = "¡Entregado! +$$$";
        if (timerText != null) timerText.text = ":)";

        Debug.Log("💰 ¡Viaje completado!");
        SpawnNewPassenger();
    }

    // --- NUEVO: FUNCIÓN PARA ACTUALIZAR EL TEXTO DE PUNTAJE ---
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            // Muestra:  "VIAJES: 5  |  RECORD: 12"
            scoreText.text = $"VIAJES: {completedTrips}  |  RÉCORD: {highScore}";
        }
    }

    void GameOver()
    {
        Debug.Log("❌ ¡SE ACABÓ EL TIEMPO!");
        if (infoText != null) infoText.text = "¡TIEMPO FUERA!";
        StopMission();
    }

    string GetZoneNameFromPosition(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, 2.0f);
        foreach (var hit in hitColliders)
        {
            ZoneTrigger zone = hit.GetComponent<ZoneTrigger>();
            if (zone != null) return zone.zoneName;
        }
        return "Zona Desconocida";
    }

    enum Difficulty { Easy, Medium, Hard }
}