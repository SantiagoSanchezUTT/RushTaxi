using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MinimapFromImage:
/// - Usa una textura (RawImage) como mapa estático.
/// - Centra el playerIcon en pantalla y desplaza la textura para simular movimiento.
/// - Permite rotar el mapa (rotateMap = true) o rotar el icono.
/// - Opcional: crea blips para objetos en escena desde trackedObjects/iconPrefab.
/// </summary>
public class MinimapFromImage : MonoBehaviour
{
    [Header("UI (assign in inspector)")]
    public RectTransform mapContainer;     // RectTransform del contenedor (MinimapContainer). Pivot (0.5,0.5)
    public RawImage mapImage;              // RawImage que contiene la textura del mapa (boquejo)
    public RectTransform playerIcon;       // Icono del jugador (UI Image) hijo del mapContainer

    [Header("World mapping")]
    public Transform player;               // Transform del jugador en la escena (mundo)
    [Tooltip("Tamaño total del mundo que representa la imagen: X = ancho (world X), Y = profundidad (world Z)")]
    public Vector2 worldSize = new Vector2(100f, 100f); // en unidades world (X,Z)
    [Tooltip("Posición world (X,Z) que corresponde al centro de la imagen (puede ser (0,0) si tu imagen mapea el centro del mundo)")]
    public Vector2 worldOrigin = Vector2.zero; // worldCoordinates (x,z) que corresponden al centro de la imagen

    [Header("Opciones")]
    public bool rotateMap = false;         // true -> rota el mapa con la rotación del jugador (icon queda estable)
    [Range(0.1f, 4f)] public float zoom = 1f; // escala del mapa (1 = normal)
    public bool clampToEdges = true;       // si true evita que el background salga demasiado del contenedor

    [Header("Tracked icons (opcional)")]
    public GameObject iconPrefab;          // prefab UI (RectTransform + Image) para blips
    public List<Transform> trackedObjects = new List<Transform>();

    // internos
    private List<RectTransform> trackedIcons = new List<RectTransform>();
    private Vector2 mapPixelSize;          // tamaño actual del mapa en unidades UI (rect)
    private Vector2 playerAnchored;        // última posición anclada del jugador en coords del mapa (UI)

    void Start()
    {
        if (mapContainer == null || mapImage == null || playerIcon == null || player == null)
        {
            Debug.LogError("MinimapFromImage: asigna mapContainer, mapImage, playerIcon y player en inspector.");
            enabled = false;
            return;
        }

        // Asegúrate que pivots/anchors son centrados para una fórmula simple:
        // mapContainer pivot = (0.5, 0.5); playerIcon pivot = (0.5, 0.5)
        mapPixelSize = mapImage.rectTransform.rect.size;

        // crear blips iniciales (si hay trackedObjects)
        foreach (var t in trackedObjects) CreateIconForTracked(t);
    }

    void Update()
    {
        // recalcula tamaño si el UI cambió (por si el Canvas escala)
        mapPixelSize = mapImage.rectTransform.rect.size;

        // pos relativa del jugador respecto al worldOrigin (x, z)
        Vector3 p = player.position;
        Vector2 relPlayer = new Vector2(p.x - worldOrigin.x, p.z - worldOrigin.y);

        // anchored del jugador (en coordenadas UI centradas)
        playerAnchored = WorldToMapAnchored(relPlayer);

        // desplaza la textura (mueve la RawImage) para centrar al jugador
        // al mover la RawImage en sentido contrario simulamos que el jugador está fijo en centro
        mapImage.rectTransform.anchoredPosition = -playerAnchored * zoom;

        // opcional: evitar que el fondo se mueva más allá de sus bordes visibles
        if (clampToEdges) ClampMapPosition();

        // mantener el icono del jugador en centro
        playerIcon.anchoredPosition = Vector2.zero;

        // rotación: mapa o icono
        if (rotateMap)
        {
            // rota el contenedor en Z en sentido opuesto a la rotación Y del jugador
            mapContainer.localEulerAngles = new Vector3(0f, 0f, -player.eulerAngles.y);
            playerIcon.localEulerAngles = Vector3.zero;
        }
        else
        {
            mapContainer.localEulerAngles = Vector3.zero;
            // rota el icono para que apunte en la dirección del jugador
            playerIcon.localEulerAngles = new Vector3(0f, 0f, -player.eulerAngles.y);
        }

        // actualizar blips
        UpdateTrackedIcons();
    }

    // convierte una posición relativa en world (relWorld.x = x - origin.x, relWorld.y = z - origin.y)
    // a coordenadas anchored (UI) centradas en el minimap
    Vector2 WorldToMapAnchored(Vector2 relWorld)
    {
        // normalizamos en rango -1..1 (suponiendo origin=centro de la imagen)
        float nx = relWorld.x / (worldSize.x * 0.5f); // -1..1
        float ny = relWorld.y / (worldSize.y * 0.5f);

        nx = Mathf.Clamp(nx, -1f, 1f);
        ny = Mathf.Clamp(ny, -1f, 1f);

        Vector2 half = mapPixelSize * 0.5f;
        return new Vector2(nx * half.x, ny * half.y);
    }

    void CreateIconForTracked(Transform t)
    {
        if (iconPrefab == null) return;
        GameObject go = Instantiate(iconPrefab, mapContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        trackedIcons.Add(rt);
    }

    void UpdateTrackedIcons()
    {
        for (int i = 0; i < trackedObjects.Count; i++)
        {
            Transform t = trackedObjects[i];
            RectTransform icon = (i < trackedIcons.Count) ? trackedIcons[i] : null;

            if (t == null)
            {
                if (icon != null) Destroy(icon.gameObject);
                continue;
            }

            if (icon == null && iconPrefab != null)
            {
                CreateIconForTracked(t);
                icon = trackedIcons[trackedIcons.Count - 1];
            }

            Vector2 rel = new Vector2(t.position.x - worldOrigin.x, t.position.z - worldOrigin.y);
            Vector2 anchored = WorldToMapAnchored(rel);

            // icon position relative to center: (anchoredObject - anchoredPlayer) * zoom
            Vector2 iconPos = (anchored - playerAnchored) * zoom;
            icon.anchoredPosition = iconPos;

            // si no rotamos el mapa, mantenemos los iconos orientados 'norte arriba'
            if (!rotateMap)
                icon.localEulerAngles = new Vector3(0f, 0f, -player.eulerAngles.y);
            else
                icon.localEulerAngles = Vector3.zero;
        }
    }

    // evita que la RawImage se mueva tanto que muestre fuera del sprite (simple clamp)
    void ClampMapPosition()
    {
        Vector2 current = mapImage.rectTransform.anchoredPosition;
        Vector2 halfMap = mapPixelSize * 0.5f * zoom;
        Vector2 halfContainer = mapContainer.rect.size * 0.5f;

        float maxX = halfMap.x - halfContainer.x;
        float maxY = halfMap.y - halfContainer.y;

        current.x = Mathf.Clamp(current.x, -maxX, maxX);
        current.y = Mathf.Clamp(current.y, -maxY, maxY);

        mapImage.rectTransform.anchoredPosition = current;
    }

    // utilidad: convertir world bounds a worldOrigin/worldSize (puedes llamar desde editor o script)
    public void SetWorldBounds(float xMin, float xMax, float zMin, float zMax)
    {
        worldOrigin = new Vector2((xMin + xMax) * 0.5f, (zMin + zMax) * 0.5f);
        worldSize = new Vector2(Mathf.Abs(xMax - xMin), Mathf.Abs(zMax - zMin));
    }
}
