using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CicloDiaNoche : MonoBehaviour
{

    [Range(0.0f, 24f)] public float Hora = 12;
    public Transform sol;
    private float solX;

    [Header("UI Reloj TMP")]
    public TMP_Text relojUI;

    public float DuracionDelDiaEnMinutos = 1;

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
        if (Hora > 18 || Hora < 6)
        {
            sol.GetComponent<Light>().intensity = 0;
        }else{
            sol.GetComponent<Light>().intensity = 1;
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