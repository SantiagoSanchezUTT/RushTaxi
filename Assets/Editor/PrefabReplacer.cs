using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PrefabReplacer : EditorWindow
{
    // Variables para la ventana
    GameObject prefabToSpawn;
    bool keepOriginal = true; // Por seguridad, activado por defecto

    // Añade un menú en la barra superior de Unity
    [MenuItem("Tools/Reemplazar o Crear Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacer>("Replacer Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("Herramienta de Creación Masiva", EditorStyles.boldLabel);

        // Campo para poner tu Prefab
        prefabToSpawn = (GameObject)EditorGUILayout.ObjectField("Nuevo Prefab", prefabToSpawn, typeof(GameObject), false);

        // Casilla para decidir si borras o mantienes los edificios
        keepOriginal = EditorGUILayout.Toggle("Mantener Originales", keepOriginal);

        GUILayout.Space(10);

        if (GUILayout.Button("¡Ejecutar en Objetos Seleccionados!"))
        {
            ReemplazarObjetos();
        }
    }

    void ReemplazarObjetos()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("¡Falta asignar el Prefab!");
            return;
        }

        // Recorremos todos los objetos que tengas seleccionados en azul en la jerarquía
        foreach (GameObject selectedObj in Selection.gameObjects)
        {
            // 1. Creamos el nuevo prefab (usando PrefabUtility para mantener el enlace azul)
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);

            // 2. Copiamos posición y rotación
            newObject.transform.position = selectedObj.transform.position;
            newObject.transform.rotation = selectedObj.transform.rotation;

            // 3. Organización: Lo ponemos como hijo o al lado
            if (keepOriginal)
            {
                // Si mantenemos el edificio, ponemos el spawn como HIJO para que sea fácil de encontrar
                newObject.transform.SetParent(selectedObj.transform);
                newObject.name = "SpawnPoint_" + selectedObj.name;
            }
            else
            {
                // Si borramos el original, copiamos su padre y nombre
                newObject.transform.SetParent(selectedObj.transform.parent);
                newObject.name = selectedObj.name;

                // Registrar para poder hacer Ctrl+Z
                Undo.DestroyObjectImmediate(selectedObj);
            }

            // Registrar la creación para Ctrl+Z
            Undo.RegisterCreatedObjectUndo(newObject, "Spawn Prefab");
        }

        Debug.Log("Proceso terminado en " + Selection.gameObjects.Length + " objetos.");
    }
}