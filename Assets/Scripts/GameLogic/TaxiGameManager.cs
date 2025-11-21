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

    [Header("Balance de Juego")]
    public float timePerUnitDistance = 0.5f;
    public float baseTimeBonus = 10.0f;

    [Header("Configuración de Dificultad")]
    [Tooltip("Distancia máxima para encontrar un pasajero cerca de ti")]
    public float maxPickupSearchRadius = 150f;

    [Tooltip("Distancia máxima para un viaje FÁCIL")]
    public float easyDistanceCap = 200f;

    [Tooltip("Distancia máxima para un viaje MEDIO")]
    public float mediumDistanceCap = 500f;
    // Cualquier viaje mayor a mediumDistanceCap se considera DIFÍCIL

    [Header("Progresión")]
    [Tooltip("A partir de qué viaje empieza el modo difícil (Fase 3). Recomendado: 10")]
    public int hardModeThreshold = 10;

    [Header("Estado del Juego")]
    public bool isMissionActive = false;
    public bool hasPassenger = false;
    public float currentTimer = 0;
    public int completedTrips = 0;

    private GameObject currentPassengerObj;
    private GameObject currentDestinationObj;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Debug.Log("🚖 SISTEMA LISTO");
        if (infoText != null) infoText.text = "Presiona '2' para TAXI";
        if (timerText != null) timerText.text = "--";
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
        completedTrips = 0; // Reiniciamos contador

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

        // 1. BUSCAR CERCA: Filtramos puntos cercanos al jugador
        List<Transform> nearbyPoints = pickupPoints
            .Where(p => Vector3.Distance(playerCar.position, p.position) <= maxPickupSearchRadius)
            .ToList();

        Transform selectedSpawn;

        // Si hay puntos cerca, elige uno de ellos. Si no, global.
        if (nearbyPoints.Count > 0)
        {
            selectedSpawn = nearbyPoints[Random.Range(0, nearbyPoints.Count)];
        }
        else
        {
            selectedSpawn = pickupPoints[Random.Range(0, pickupPoints.Length)];
        }

        currentPassengerObj = Instantiate(passengerPrefab, selectedSpawn.position, Quaternion.identity);

        if (infoText != null) infoText.text = "Recoger en: " + selectedSpawn.name;
        if (timerText != null) timerText.text = "Espera";

        Debug.Log($"📍 Pasajero (Viaje #{completedTrips + 1}) esperando en: {selectedSpawn.name}");
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
        float rand = Random.value; // 0.0 a 1.0

        // --- LÓGICA DE PROBABILIDADES ACTUALIZADA ---

        // FASE 1: INICIO (Viajes 0, 1, 2 -> Primeros 3)
        if (completedTrips < 3)
        {
            level = Difficulty.Easy;
        }
        // FASE 2: INTERMEDIO (Hasta el viaje 9)
        else if (completedTrips < hardModeThreshold)
        {
            // 25% Fácil | 50% Medio | 25% Difícil
            if (rand < 0.25f) level = Difficulty.Easy;
            else if (rand < 0.75f) level = Difficulty.Medium;
            else level = Difficulty.Hard;
        }
        // FASE 3: HARDCORE (Viaje 10 en adelante)
        else
        {
            // 5% Fácil | 40% Medio | 55% Difícil
            if (rand < 0.05f) level = Difficulty.Easy;
            else if (rand < 0.45f) level = Difficulty.Medium;
            else level = Difficulty.Hard;
        }

        // --- FILTRADO DE DESTINOS ---
        List<Transform> validDestinations = new List<Transform>();

        foreach (Transform point in dropOffPoints)
        {
            float dist = Vector3.Distance(playerCar.position, point.position);

            switch (level)
            {
                case Difficulty.Easy:
                    if (dist <= easyDistanceCap) validDestinations.Add(point);
                    break;
                case Difficulty.Medium:
                    if (dist > easyDistanceCap && dist <= mediumDistanceCap) validDestinations.Add(point);
                    break;
                case Difficulty.Hard:
                    if (dist > mediumDistanceCap) validDestinations.Add(point);
                    break;
            }
        }

        // FALLBACK: Si no encuentra destinos para esa dificultad, busca cualquiera
        Transform selectedDest;
        if (validDestinations.Count > 0)
        {
            selectedDest = validDestinations[Random.Range(0, validDestinations.Count)];
        }
        else
        {
            // Si el filtro fue muy estricto y falló (ej. no hay puntos Hard), agarra cualquiera que esté lejos (si era Hard) o random total
            Debug.LogWarning($"⚠️ No se encontraron destinos para {level}. Usando aleatorio global.");
            selectedDest = dropOffPoints[Random.Range(0, dropOffPoints.Length)];
        }

        currentDestinationObj = Instantiate(destinationZonePrefab, selectedDest.position, Quaternion.identity);

        float distance = Vector3.Distance(playerCar.position, selectedDest.position);
        currentTimer = (distance * timePerUnitDistance) + baseTimeBonus;

        if (infoText != null) infoText.text = $"Llevar a: {selectedDest.name} ({level})";

        Debug.Log($"🏁 Destino: {selectedDest.name} | Distancia: {distance:F1} | Dificultad: {level}");
    }

    public void DropOffPassenger()
    {
        if (!isMissionActive) return;

        hasPassenger = false;
        completedTrips++; // IMPORTANTE: Aumenta contador

        if (currentDestinationObj != null) Destroy(currentDestinationObj);

        if (infoText != null) infoText.text = $"¡Entregado! (+$$$) Total: {completedTrips}";
        if (timerText != null) timerText.text = ":)";

        Debug.Log("💰 ¡Viaje completado!");
        SpawnNewPassenger();
    }

    void GameOver()
    {
        Debug.Log("❌ ¡SE ACABÓ EL TIEMPO!");
        if (infoText != null) infoText.text = "¡TIEMPO FUERA!";
        StopMission();
    }

    enum Difficulty { Easy, Medium, Hard }
}