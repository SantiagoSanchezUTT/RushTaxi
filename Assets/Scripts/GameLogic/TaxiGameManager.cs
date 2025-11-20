using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaxiGameManager : MonoBehaviour
{
    public static TaxiGameManager Instance;

    [Header("Configuración")]
    public Transform playerCar;
    public Transform[] spawnPoints;
    public GameObject passengerPrefab;
    public GameObject destinationZonePrefab;

    [Header("Dificultad")]
    public float timePerUnitDistance = 0.5f;
    public float baseTimeBonus = 10.0f;

    [Header("Estado del Juego")]
    public bool isMissionActive = false;
    public bool hasPassenger = false;
    public float currentTimer = 0;

    // Variables privadas para guardar la referencia de los objetos creados
    private GameObject currentPassengerObj;
    private GameObject currentDestinationObj;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Debug.Log("🚖 SISTEMA LISTO: Presiona '2' para iniciar el trabajo de taxi.");
    }

    void Update()
    {
        // 1. DETECTAR EL INPUT (Tecla 2 del teclado alfanumérico)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleMissionMode();
        }

        // Si la misión NO está activa, no hacemos nada más
        if (!isMissionActive) return;

        // 2. Lógica del Temporizador (Solo corre si tienes pasajero)
        if (hasPassenger)
        {
            currentTimer -= Time.deltaTime;

            // Aquí podrías actualizar tu UI de texto en el futuro

            if (currentTimer <= 0)
            {
                GameOver();
            }
        }

        // 3. --- DEBUG VISUAL (LÍNEAS) ---
        // Esto dibuja líneas en la escena para que sepas dónde ir
        if (playerCar != null)
        {
            if (currentPassengerObj != null && !hasPassenger)
            {
                // Línea ROJA hacia el pasajero a recoger
                Debug.DrawLine(playerCar.position, currentPassengerObj.transform.position, Color.red);
            }
            else if (currentDestinationObj != null && hasPassenger)
            {
                // Línea VERDE hacia el destino final
                Debug.DrawLine(playerCar.position, currentDestinationObj.transform.position, Color.green);
            }
        }
    }

    // --- CONTROL DE MISIONES ---

    public void ToggleMissionMode()
    {
        if (isMissionActive)
        {
            StopMission(); // Apagar
        }
        else
        {
            StartMission(); // Encender
        }
    }

    void StartMission()
    {
        isMissionActive = true;
        hasPassenger = false;
        Debug.Log("--- 🟢 MISIÓN DE TAXI INICIADA ---");
        SpawnNewPassenger();
    }

    void StopMission()
    {
        isMissionActive = false;
        hasPassenger = false;
        currentTimer = 0;

        // LIMPIEZA: Borrar objetos sobrantes
        if (currentPassengerObj != null) Destroy(currentPassengerObj);
        if (currentDestinationObj != null) Destroy(currentDestinationObj);

        Debug.Log("--- 🔴 MISIÓN CANCELADA (Modo Libre) ---");
    }

    // --- LÓGICA DEL JUEGO ---

    public void SpawnNewPassenger()
    {
        if (!isMissionActive) return;

        // Elegir punto aleatorio
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedSpawn = spawnPoints[randomIndex];

        // Crear Pasajero
        currentPassengerObj = Instantiate(passengerPrefab, selectedSpawn.position, Quaternion.identity);

        // LOG DEPURACIÓN: Nos dice el nombre exacto del punto
        Debug.Log("📍 Nuevo Pasajero en: " + selectedSpawn.name);
    }

    public void PickupPassenger()
    {
        if (!isMissionActive) return;

        hasPassenger = true;

        // Borrar la esfera del pasajero (ya se subió)
        if (currentPassengerObj != null) Destroy(currentPassengerObj);

        Debug.Log("🚕 ¡Pasajero Recogido!");

        GenerateDestination();
    }

    void GenerateDestination()
    {
        // Elegir destino aleatorio
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedDest = spawnPoints[randomIndex];

        // Crear zona de destino
        currentDestinationObj = Instantiate(destinationZonePrefab, selectedDest.position, Quaternion.identity);

        // CALCULAR DIFICULTAD
        float distance = Vector3.Distance(playerCar.position, selectedDest.position);
        currentTimer = (distance * timePerUnitDistance) + baseTimeBonus;

        // LOG DEPURACIÓN
        Debug.Log("🏁 Destino generado en: " + selectedDest.name + " || Distancia: " + (int)distance + "m || Tiempo: " + (int)currentTimer + "s");
    }

    public void DropOffPassenger()
    {
        if (!isMissionActive) return;

        hasPassenger = false;

        // Borrar el destino
        if (currentDestinationObj != null) Destroy(currentDestinationObj);

        Debug.Log("💰 ¡Viaje completado! +$$$");

        // Generar el siguiente inmediatamente
        SpawnNewPassenger();
    }

    void GameOver()
    {
        Debug.Log("❌ ¡SE ACABÓ EL TIEMPO! Game Over.");
        StopMission();
    }
}