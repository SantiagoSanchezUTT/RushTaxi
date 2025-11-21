using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Necesario para Image

public class RotateUIElement : MonoBehaviour
{
    [Tooltip("Velocidad de rotación en grados por segundo")]
    public float rotationSpeed = 90f; // 90 grados por segundo

    [Tooltip("¿El disco está girando actualmente?")]
    public bool isRotating = false;

    // Puedes llamar a esta función desde tu script TaxiRadio
    public void SetRotation(bool rotate)
    {
        isRotating = rotate;
    }

    void Update()
    {
        if (isRotating)
        {
            // Rota el GameObject alrededor de su propio eje Z (para UI)
            transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
            // Usamos -rotationSpeed para que gire en sentido horario
        }
    }
}