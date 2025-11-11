using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System.Linq;

public class TaxiRadio : MonoBehaviour
{
    [Tooltip("Arrastra aquí el componente AudioSource de este objeto")]
    public AudioSource audioSource;
    [Tooltip("Arrastra aquí tus canciones SIN COPYRIGHT para la radio por defecto")]
    public List<AudioClip> defaultStationTracks;
    [Tooltip("SFX que sonará COMPLETO como transición al cargar una canción")]
    public AudioClip stationChangeSfx;
    [Tooltip("Arrastra aquí el objeto de texto (TMP) que mostrará el nombre")]
    public TextMeshProUGUI stationNameText;
    [Tooltip("Máximo número de clips en caché (ajusta según RAM)")]
    public int maxCacheItems = 30;

    // --- CACHÉ Y CONTROL ---
    private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private HashSet<string> preloading = new HashSet<string>();
    private Queue<string> cacheOrder = new Queue<string>();

    // --- ESTRUCTURA DE ESTACIONES ---
    private Dictionary<string, List<string>> userStations = new Dictionary<string, List<string>>();
    private List<string> masterStationList = new List<string>();
    private int currentStationIndex = 0;
    private bool isLoadingSong = false;

    // --- ¡SOLUCIÓN! ---
    // Este es el "número de ticket" de la petición de radio actual.
    private int _currentStationRequestID = 0;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        ScanRadioFolders();
        BuildMasterStationList();
        StartCoroutine(PreloadInitialStations());
        SwitchStationByIndex(0);
    }

    void Update()
    {
        if (currentStationIndex != 0 && !audioSource.isPlaying && !isLoadingSong)
        {
            // Pasa el ID de la petición actual a la corutina
            StartCoroutine(PlayNextTrackCoroutine(false, _currentStationRequestID)); // <-- CAMBIO
        }
    }

    public void CycleNextStation()
    {
        int nextIndex = currentStationIndex + 1;
        if (nextIndex >= masterStationList.Count)
        {
            nextIndex = 0;
        }
        SwitchStationByIndex(nextIndex);
    }

    #region Scan y Build
    private void ScanRadioFolders()
    {
        Debug.Log("Escaneando radios del usuario...");
        userStations.Clear();
        try
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string radiosBasePath = Path.Combine(documentsPath, "Rush City User Files", "Radios");

            if (!Directory.Exists(radiosBasePath))
            {
                Directory.CreateDirectory(radiosBasePath);
                Debug.Log("Carpeta de radios no encontrada. Creando una nueva en: " + radiosBasePath);
                return;
            }

            string[] stationFolders = Directory.GetDirectories(radiosBasePath);

            foreach (string stationPath in stationFolders)
            {
                string stationName = Path.GetFileName(stationPath);
                List<string> songs = Directory.GetFiles(stationPath, "*.ogg").ToList();

                if (songs.Count > 0)
                {
                    userStations.Add(stationName, songs);
                    Debug.Log($"Estación de usuario encontrada: '{stationName}' ({songs.Count} canciones)");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ocurrió un error al escanear las radios: " + e.Message);
        }
    }

    private void BuildMasterStationList()
    {
        masterStationList.Clear();
        masterStationList.Add("Radio Apagada");
        if (defaultStationTracks.Count > 0)
        {
            masterStationList.Add("Radio Predeterminada");
        }
        foreach (string stationName in userStations.Keys)
        {
            masterStationList.Add(stationName);
        }
    }
    #endregion

    private void SwitchStationByIndex(int index)
    {
        currentStationIndex = index;
        string newStationName = masterStationList[currentStationIndex];

        // --- ¡SOLUCIÓN! ---
        // Genera un nuevo "número de ticket", invalidando todas las peticiones anteriores.
        _currentStationRequestID++;
        int myRequestID = _currentStationRequestID; // Guarda el ID de *esta* petición

        Debug.Log($"Cambiando a: {newStationName} (Petición ID: {myRequestID})");

        audioSource.Stop();
        isLoadingSong = false;

        if (stationNameText != null)
        {
            stationNameText.text = newStationName;
        }

        if (newStationName != "Radio Apagada")
        {
            // Pasa el ID de esta petición específica
            StartCoroutine(PlayNextTrackCoroutine(true, myRequestID)); // <-- CAMBIO
        }
    }

    private IEnumerator PreloadInitialStations()
    {
        // ... (Esta función está bien, no necesita cambios) ...
        for (int i = 0; i < defaultStationTracks.Count; i++)
        {
            var clip = defaultStationTracks[i];
            if (clip != null) { try { clip.LoadAudioData(); } catch { } }
            yield return null;
        }
        foreach (var kv in userStations)
        {
            var songs = kv.Value;
            if (songs != null && songs.Count > 0)
            {
                string songPath = songs[Random.Range(0, songs.Count)];
                StartCoroutine(PreloadSong(songPath, false));
                yield return null;
            }
        }
    }

    // --- CORUTINA DE REPRODUCCIÓN (MODIFICADA) ---
    // Ahora acepta un "ID de petición"
    private IEnumerator PlayNextTrackCoroutine(bool isStationChange = false, int requestID = 0)
    {
        isLoadingSong = true;

        // --- ¡SOLUCIÓN! ---
        // Comprueba si esta corutina sigue siendo válida.
        // Si el ID de esta corutina NO es el ID actual, es obsoleta.
        if (requestID != _currentStationRequestID)
        {
            Debug.Log($"Petición {requestID} obsoleta, cancelando.");
            isLoadingSong = false; // Libera el bloqueo
            yield break; // Termina la corutina
        }

        string currentStationName = masterStationList[currentStationIndex];

        // --- RADIO PREDETERMINADA ---
        if (currentStationName == "Radio Predeterminada")
        {
            if (defaultStationTracks.Count == 0) { /* ...código de salida... */ }

            if (isStationChange && stationChangeSfx != null)
            {
                audioSource.clip = stationChangeSfx;
                audioSource.loop = false;
                audioSource.Play();
                yield return new WaitForSeconds(stationChangeSfx.length);
            }

            // --- ¡SOLUCIÓN! ---
            // Re-comprueba el ID después de la espera
            if (requestID != _currentStationRequestID) { isLoadingSong = false; yield break; }

            AudioClip clipToPlay = defaultStationTracks[Random.Range(0, defaultStationTracks.Count)];
            audioSource.clip = clipToPlay;
            audioSource.loop = false;
            audioSource.Play();

            StartCoroutine(PrefetchAnotherDefault());
            isLoadingSong = false;
            yield break;
        }
        // --- RADIO DE USUARIO ---
        else if (currentStationName != "Radio Apagada")
        {
            if (!userStations.ContainsKey(currentStationName) || userStations[currentStationName].Count == 0) { /* ...código de salida... */ }

            List<string> songs = userStations[currentStationName];
            string songPath = songs[Random.Range(0, songs.Count)];

            if (isStationChange && stationChangeSfx != null)
            {
                audioSource.clip = stationChangeSfx;
                audioSource.loop = false;
                audioSource.Play();
                yield return new WaitForSeconds(stationChangeSfx.length);
            }

            // --- ¡SOLUCIÓN! ---
            // Re-comprueba el ID después de la espera
            if (requestID != _currentStationRequestID) { isLoadingSong = false; yield break; }

            // 1. Revisa si la canción ya está en caché
            if (clipCache.TryGetValue(songPath, out AudioClip cached))
            {
                audioSource.clip = cached;
                audioSource.loop = false;
                audioSource.Play();
                StartCoroutine(PrefetchAnotherSong(songs, songPath));
            }
            // 2. Si no está en caché, hay que cargarla
            else
            {
                StartCoroutine(PreloadSong(songPath, true));

                float waitT = 0f;
                float timeout = 6f;
                while (!clipCache.ContainsKey(songPath) && waitT < timeout)
                {
                    // --- ¡SOLUCIÓN! ---
                    // Comprueba el ID en CADA frame del bucle de espera
                    if (requestID != _currentStationRequestID) { isLoadingSong = false; yield break; }

                    waitT += Time.deltaTime;
                    yield return null;
                }

                if (clipCache.TryGetValue(songPath, out AudioClip clipNow))
                {
                    // --- ¡SOLUCIÓN! ---
                    // Comprobación final antes de reproducir
                    if (requestID != _currentStationRequestID) { isLoadingSong = false; yield break; }

                    audioSource.clip = clipNow;
                    audioSource.loop = false;
                    audioSource.Play();
                }
                else
                {
                    yield return StartCoroutine(LoadAndPlaySongFallback(songPath, requestID)); // Pasar el ID
                }
                StartCoroutine(PrefetchAnotherSong(songs, songPath));
            }

            isLoadingSong = false;
            yield break;
        }

        isLoadingSong = false;
    }

    // --- (PreloadSong y Prefetch no necesitan cambios) ---
    #region Preload y Prefetch
    private IEnumerator PreloadSong(string songPath, bool assignWhenReady)
    {
        if (string.IsNullOrEmpty(songPath)) yield break;
        if (clipCache.ContainsKey(songPath)) yield break;
        if (preloading.Contains(songPath)) yield break;

        preloading.Add(songPath);

        string url = "file://" + songPath;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
        {
            (request.downloadHandler as DownloadHandlerAudioClip).streamAudio = false;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    clipCache[songPath] = clip;
                    cacheOrder.Enqueue(songPath);
                    while (cacheOrder.Count > maxCacheItems)
                    {
                        string evicted = cacheOrder.Dequeue();
                        if (clipCache.TryGetValue(evicted, out AudioClip evClip))
                        {
                            clipCache.Remove(evicted);
                            try { Destroy(evClip); } catch { }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Preload error: {songPath} | {request.error}");
            }
        }
        preloading.Remove(songPath);
    }

    private IEnumerator PrefetchAnotherSong(List<string> songs, string justPlayedPath)
    {
        if (songs == null || songs.Count == 0) yield break;
        string candidate = justPlayedPath;
        int tries = 0;
        while (candidate == justPlayedPath && songs.Count > 1 && tries < 10)
        {
            candidate = songs[Random.Range(0, songs.Count)];
            tries++;
        }
        if (!clipCache.ContainsKey(candidate))
        {
            StartCoroutine(PreloadSong(candidate, false));
        }
        yield break;
    }

    private IEnumerator PrefetchAnotherDefault()
    {
        if (defaultStationTracks == null || defaultStationTracks.Count <= 1) yield break;
        yield break;
    }
    #endregion

    // --- FALLBACK (MODIFICADO) ---
    // Acepta el ID de petición
    private IEnumerator LoadAndPlaySongFallback(string songPath, int requestID) // <-- CAMBIO
    {
        isLoadingSong = true;
        string url = "file://" + songPath;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
        {
            (request.downloadHandler as DownloadHandlerAudioClip).streamAudio = true;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // --- ¡SOLUCIÓN! ---
                // Comprobación final antes de reproducir
                if (requestID != _currentStationRequestID)
                {
                    isLoadingSong = false;
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = clip;
                audioSource.loop = false;
                audioSource.Play();
                if (!clipCache.ContainsKey(songPath))
                {
                    clipCache[songPath] = clip;
                    cacheOrder.Enqueue(songPath);
                }
            }
            else
            {
                Debug.LogError($"Error al cargar canción (fallback): {songPath} | Error: {request.error}");
            }
        }
        isLoadingSong = false;
    }
}