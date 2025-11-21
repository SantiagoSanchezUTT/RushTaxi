using UnityEngine;
using TMPro;
using System.Collections;

public class ZoneUIManager : MonoBehaviour
{
    public static ZoneUIManager Instance; // Para poder llamarlo desde cualquier lado

    [Header("UI References")]
    public TextMeshProUGUI zoneText; // Arrastra aquí tu texto del Canvas

    [Header("Configuración")]
    public float fadeInTime = 1.0f;
    public float displayTime = 3.0f;
    public float fadeOutTime = 1.0f;

    private Coroutine currentRoutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (zoneText != null)
            zoneText.alpha = 0; // Empezar invisible
    }

    // Esta función la llamarán los barrios
    public void ShowZoneName(string name)
    {
        if (zoneText == null) return;

        // Si ya hay una animación ocurriendo, la cortamos y empezamos la nueva
        if (currentRoutine != null) StopCoroutine(currentRoutine);

        zoneText.text = name; // Cambiar el texto
        currentRoutine = StartCoroutine(AnimateText());
    }

    IEnumerator AnimateText()
    {
        // 1. FADE IN (Aparecer)
        float timer = 0f;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            zoneText.alpha = Mathf.Lerp(0f, 1f, timer / fadeInTime);
            yield return null;
        }
        zoneText.alpha = 1f;

        // 2. ESPERAR (Mostrar nombre)
        yield return new WaitForSeconds(displayTime);

        // 3. FADE OUT (Desaparecer)
        timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            zoneText.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            yield return null;
        }
        zoneText.alpha = 0f;
    }
}