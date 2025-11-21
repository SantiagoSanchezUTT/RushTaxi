using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Nombre que saldrá en pantalla")]
    public string zoneName = "Nombre del Barrio";

    [Header("Configuración")]
    public string playerTag = "Player"; // Asegúrate de que tu Taxi tenga el Tag "Player"

    void OnTriggerEnter(Collider other)
    {
        // Si lo que entró es el Jugador (y no un coche NPC)
        if (other.CompareTag(playerTag))
        {
            // Mandar aviso al UI Manager
            if (ZoneUIManager.Instance != null)
            {
                ZoneUIManager.Instance.ShowZoneName(zoneName);
                Debug.Log(" Entrando en: " + zoneName);
            }
        }
    }
}