using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CicloDiaNoche : MonoBehaviour
{
    [Range(0.0f, 24f)] public float Hora = 12;
    public Transform sol;
    private float solX;

    // Variable privada para guardar la referencia de la luz y no buscarla en cada frame
    private Light luzDelSol;

    [Header("UI Reloj TMP")]
    public TMP_Text relojUI;

    public float DuracionDelDiaEnMinutos = 1;

    // --- AQUI ESTÁ LA SOLUCIÓN AL PROBLEMA DE ILUMINACIÓN ---
    void Start()
    {
        // 1. Obtenemos el componente Light una sola vez al inicio
        if (sol != null)
        {
            luzDelSol = sol.GetComponent<Light>();
        }

        // 2. Forzamos a Unity a reconocer este objeto como el SOL de la escena
        if (luzDelSol != null)
        {
            RenderSettings.sun = luzDelSol;
        }

        // 3. Actualizamos el entorno para quitar lo oscuro
        DynamicGI.UpdateEnvironment();
    }
    // -------------------------------------------------------

    void mostrarHoraEnUI()
    {
        int horas = Mathf.FloorToInt(Hora);
        int minutos = Mathf.FloorToInt((Hora - horas) * 60f);

        // Formato HH:MM
        string horaTexto = string.Format("{0:00}:{1:00}", horas, minutos);

        if (relojUI != null)
            relojUI.text = horaTexto;
    }

    void rotacionSol()
    {
        solX = 15 * Hora;
        sol.localEulerAngles = new Vector3(solX, 0, 0);

        // Usamos la variable 'luzDelSol' que guardamos en Start (es más rápido)
        if (luzDelSol != null)
        {
            if (Hora > 18 || Hora < 6)
            {
                luzDelSol.intensity = 0;
            }
            else
            {
                luzDelSol.intensity = 1;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Hora += Time.deltaTime * 24 / (60 * DuracionDelDiaEnMinutos);

        if (Hora >= 24)
        {
            Hora = 0;
        }

        rotacionSol();
        mostrarHoraEnUI();
    }
}