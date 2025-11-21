using UnityEngine;
using UnityEditor;
using System.Linq; // Necesario para ordenar la lista

public class RenombradorMasivo : EditorWindow
{
    string nombreBase = "SP_WestMarket_"; // El nombre que quieres poner
    int numeroInicial = 1; // En qué número empieza a contar
    bool usarCeros = true; // Si quieres que sea '01' en vez de '1'

    // Esto añade la opción en el menú de arriba de Unity
    [MenuItem("Tools/Renombrador Masivo")]
    public static void ShowWindow()
    {
        GetWindow<RenombradorMasivo>("Renombrador");
    }

    void OnGUI()
    {
        GUILayout.Label("Configuración de Renombrado", EditorStyles.boldLabel);

        nombreBase = EditorGUILayout.TextField("Nombre Base:", nombreBase);
        numeroInicial = EditorGUILayout.IntField("Empezar en:", numeroInicial);
        usarCeros = EditorGUILayout.Toggle("Usar ceros (01, 02...)", usarCeros);

        GUILayout.Space(10);

        // Muestra cuántos objetos tienes seleccionados actualmente
        GUILayout.Label($"Objetos seleccionados: {Selection.gameObjects.Length}");

        if (GUILayout.Button("¡RENOMBRAR AHORA!"))
        {
            RenombrarObjetos();
        }
    }

    void RenombrarObjetos()
    {
        // Obtenemos los objetos seleccionados
        GameObject[] seleccionados = Selection.gameObjects;

        // IMPORTANTE: Los ordenamos según su posición en la Jerarquía (de arriba a abajo)
        // Si no hacemos esto, Unity los renombra en el orden en que los clickeaste, que suele ser un caos.
        var objetosOrdenados = seleccionados.OrderBy(x => x.transform.GetSiblingIndex()).ToArray();

        int contador = numeroInicial;

        foreach (GameObject obj in objetosOrdenados)
        {
            // Esto permite que puedas hacer Ctrl+Z si te equivocas
            Undo.RecordObject(obj, "Renombrado Masivo");

            // Formato del número (ej: "001" o "1")
            string numeroFormateado = contador.ToString();
            if (usarCeros)
            {
                // Si son menos de 100 objetos usa "01", si son más usa "001" automáticamente
                numeroFormateado = contador.ToString("00");
            }

            // Asignar el nombre final
            obj.name = $"{nombreBase}{numeroFormateado}";

            contador++;
        }
    }
}