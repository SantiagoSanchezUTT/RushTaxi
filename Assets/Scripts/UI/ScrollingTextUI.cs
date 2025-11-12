using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class ScrollingTextUI : MonoBehaviour
{
    // 1. Referencias (asigna en el Inspector)
    [Tooltip("El componente de texto que se va a mover")]
    public TextMeshProUGUI textComponent;

    [Tooltip("El RectTransform del panel que contiene este texto (el que tiene la Máscara)")]
    public RectTransform container;

    // 2. Configuración del Scroll
    [Tooltip("Velocidad de movimiento en píxeles por segundo")]
    public float scrollSpeed = 50f;
    [Tooltip("Segundos de espera antes de empezar a moverse")]
    public float startDelay = 1.5f;
    [Tooltip("Segundos de espera después de llegar al final, antes de reiniciar")]
    public float endDelay = 1.5f;
    [Tooltip("Espacio extra (padding) al inicio y final del scroll")]
    public float padding = 20f;

    // --- Internas ---
    private RectTransform textRectTransform;
    private Coroutine activeScrollingCoroutine;
    private string currentText = "";

    void Start()
    {
        // 1. Obtener el RectTransform de ESTE MISMO OBJETO (el texto)
        textRectTransform = GetComponent<RectTransform>();

        // 2. Obtener el componente TextMeshPro (si no se asignó manualmente)
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        // 3. Obtener el RectTransform del padre (el contenedor) si no se asignó manualmente
        //    ¡ESTA LÍNEA DE AQUÍ ABAJO ES LA QUE DEBEMOS REVISAR/ASEGURAR!
        if (container == null)
        {
            // Si el script está en el texto, el padre es el contenedor
            container = transform.parent.GetComponent<RectTransform>();
            if (container == null)
            {
                Debug.LogError("ScrollingTextUI: No se encontró un RectTransform 'container' (padre) ni se asignó manualmente.");
            }
        }

        // Iniciar con un texto de ejemplo o vacío
        UpdateText(textComponent.text);
    }

    // Tu script de "TaxiRadio" llamará a esta función para cambiar el nombre.
    public void UpdateText(string newText)
    {
        // Si el texto no ha cambiado, no hacer nada
        if (newText == currentText) return;

        currentText = newText;
        textComponent.text = newText;

        // Detener cualquier scroll anterior que estuviera activo
        if (activeScrollingCoroutine != null)
        {
            StopCoroutine(activeScrollingCoroutine);
        }

        // --- LÓGICA MODIFICADA ---

        // 1. SIEMPRE alinear el texto a la izquierda
        textComponent.alignment = TextAlignmentOptions.Left;

        // 2. SIEMPRE resetear la posición del texto al inicio (izquierda)
        textRectTransform.anchoredPosition = new Vector2(0, textRectTransform.anchoredPosition.y);

        // 3. Forzar el recálculo para saber el ancho real
        textComponent.ForceMeshUpdate(true);
        float containerWidth = container.rect.width;
        float textWidth = textComponent.preferredWidth;

        // 4. Iniciar el scroll SOLAMENTE SI el texto es más largo que el panel
        if (textWidth > containerWidth)
        {
            activeScrollingCoroutine = StartCoroutine(ScrollText(textWidth, containerWidth));
        }
        // ¡Y ya no hay "else"! Si el texto es corto, simplemente se queda quieto
        // a la izquierda, que es lo que querías.
    }


    IEnumerator ScrollText(float textWidth, float containerWidth)
    {
        // 1. Alinear el texto a la izquierda para que el scroll inicie bien
        textComponent.alignment = TextAlignmentOptions.Left;

        // 2. Calcular posiciones de inicio y fin
        // (El RectTransform del texto debe estar alineado (pivot) en el centro (0.5))
        float startX = 0;
        float endX = -(textWidth - containerWidth + padding); // Moverse hasta que el final del texto toque el borde derecho

        // 3. Esperar antes de empezar
        yield return new WaitForSeconds(startDelay);

        // Bucle infinito de scroll
        while (true)
        {
            // 4. Resetear a la posición inicial
            textRectTransform.anchoredPosition = new Vector2(startX, textRectTransform.anchoredPosition.y);
            float currentX = startX;

            // 5. Moverse hacia la izquierda
            while (currentX > endX)
            {
                currentX -= scrollSpeed * Time.deltaTime;
                textRectTransform.anchoredPosition = new Vector2(currentX, textRectTransform.anchoredPosition.y);
                yield return null; // Esperar al siguiente frame
            }

            // 6. Esperar al final
            yield return new WaitForSeconds(endDelay);
        }
    }
}