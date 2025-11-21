using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaxiInteraction : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Si tocamos al pasajero y NO tenemos uno ya
        if (other.CompareTag("Passenger") && !TaxiGameManager.Instance.hasPassenger)
        {
            TaxiGameManager.Instance.PickupPassenger();
            Debug.Log("¡Pasajero recogido!");
        }

        // Si tocamos el destino y SÍ tenemos pasajero
        if (other.CompareTag("Destination") && TaxiGameManager.Instance.hasPassenger)
        {
            // Opcional: Verificar si el coche está casi detenido (velocidad < x)
            TaxiGameManager.Instance.DropOffPassenger();
            Debug.Log("¡Cliente entregado!");
        }
    }
}