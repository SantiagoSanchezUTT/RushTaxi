using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class TaxiRadio : MonoBehaviour
{
    [Header("Configuración de Audio")]
    [Tooltip("Arrastra aquí el componente AudioSource de este objeto")]
    public AudioSource audioSource;
    [Tooltip("Arrastra aquí tus canciones SIN COPYRIGHT para la radio por defecto")]
    public List<AudioClip> defaultStationTracks;
    [Tooltip("SFX que sonará COMPLETO como transición al cargar una canción")]
    public AudioClip stationChangeSfx;

    [Header("Referencias de UI")]
    [Tooltip("Arrastra aquí el GameObject con el script ScrollingTextUI")]
    public ScrollingTextUI radioTextScroller;
    [Tooltip("Arrastra aquí el GameObject con el script RotateUIElement")]
    public RotateUIElement radioDiskRotator;
    [Tooltip("Arrastra aquí el Sprite por defecto del disco")]
    public Sprite defaultDiskSprite;
    [Tooltip("Arrastra aquí el componente Image del cover (DiscoGrande)")]
    public Image diskCoverImage;

    [Header("Configuración Avanzada")]
    [Tooltip("Máximo número de clips en caché (ajusta según RAM)")]
    public int maxCacheItems = 30;

    // --- CACHÉ Y CONTROL ---
    private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    // --- ESTRUCTURA DE ESTACIONES ---
    private Dictionary<string, StationData> userStationsData = new Dictionary<string, StationData>();
    private List<string> masterStationList = new List<string>();
    private int currentStationIndex = 0;
    private bool isLoadingSong = false;

    // ID para evitar conflictos de cargas asíncronas
    private int _currentStationRequestID = 0;

    // Variable para recordar la última canción y no repetirla
    private string _lastPlayedSongPath = "";

    [System.Serializable]
    public class StationData
    {
        public List<string> Songs = new List<string>();
        public string CoverPath;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;

        ScanRadioFolders();
        BuildMasterStationList();

        // Iniciamos en la estación 0
        SwitchStationByIndex(0);
    }

    void Update()
    {
        // He quitado el Input.GetKeyDown. 
        // Ahora el cambio de estación depende totalmente de que llames a CycleNextStation() desde fuera.

        // Lógica automática: Si la canción terminó, reproducir la siguiente
        if (currentStationIndex != 0 && !audioSource.isPlaying && !isLoadingSong)
        {
            // Verificamos que no esté simplemente pausado
            if (audioSource.time == 0 || !audioSource.isPlaying)
            {
                StartCoroutine(PlayNextTrackCoroutine(false, _currentStationRequestID));
            }
        }
    }

    // Llama a esta función desde tu otro script o botón UI
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
        userStationsData.Clear();
        try
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string radiosBasePath = Path.Combine(documentsPath, "Rush City User Files", "Radios");

            if (!Directory.Exists(radiosBasePath))
            {
                Directory.CreateDirectory(radiosBasePath);
                return;
            }

            string[] stationFolders = Directory.GetDirectories(radiosBasePath);

            foreach (string stationPath in stationFolders)
            {
                string stationName = Path.GetFileName(stationPath);
                List<string> songs = Directory.GetFiles(stationPath, "*.ogg").ToList();

                string coverPath = Directory.GetFiles(stationPath, "cover.png").FirstOrDefault();
                if (string.IsNullOrEmpty(coverPath))
                {
                    coverPath = Directory.GetFiles(stationPath, "cover.jpg").FirstOrDefault();
                }

                if (songs.Count > 0)
                {
                    userStationsData.Add(stationName, new StationData { Songs = songs, CoverPath = coverPath });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al escanear radios: " + e.Message);
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
        foreach (string stationName in userStationsData.Keys)
        {
            masterStationList.Add(stationName);
        }
    }
    #endregion

    private void SwitchStationByIndex(int index)
    {
        currentStationIndex = index;
        string newStationName = masterStationList[currentStationIndex];

        _currentStationRequestID++;
        int myRequestID = _currentStationRequestID;

        Debug.Log($"Cambiando a: {newStationName}");

        audioSource.Stop();
        isLoadingSong = false;

        // UI Reset
        if (radioDiskRotator != null) { radioDiskRotator.SetRotation(false); }
        if (diskCoverImage != null) { diskCoverImage.sprite = defaultDiskSprite; }
        if (radioTextScroller != null) { radioTextScroller.UpdateText(newStationName); }

        if (newStationName != "Radio Apagada")
        {
            if (radioDiskRotator != null) { radioDiskRotator.SetRotation(true); }

            string coverPathToLoad = null;
            if (newStationName == "Radio Predeterminada")
            {
                // Default sprite ya seteado arriba
            }
            else if (userStationsData.TryGetValue(newStationName, out StationData data))
            {
                coverPathToLoad = data.CoverPath;
            }

            StartCoroutine(LoadAndAssignCover(coverPathToLoad, myRequestID));
            StartCoroutine(PlayNextTrackCoroutine(true, myRequestID));
        }
    }

    private IEnumerator PlayNextTrackCoroutine(bool isStationChange = false, int requestID = 0)
    {
        isLoadingSong = true;

        if (requestID != _currentStationRequestID) { CancelLoading(); yield break; }

        string currentStationName = masterStationList[currentStationIndex];

        // === CASO 1: RADIO PREDETERMINADA ===
        if (currentStationName == "Radio Predeterminada")
        {
            if (defaultStationTracks.Count == 0) { CancelLoading(); yield break; }

            if (isStationChange && stationChangeSfx != null)
            {
                PlaySFX(stationChangeSfx);
                yield return new WaitForSeconds(stationChangeSfx.length);
            }

            if (requestID != _currentStationRequestID) { CancelLoading(); yield break; }

            AudioClip clipToPlay = defaultStationTracks[Random.Range(0, defaultStationTracks.Count)];
            audioSource.clip = clipToPlay;
            audioSource.loop = false;
            audioSource.Play();
            isLoadingSong = false;
            yield break;
        }
        // === CASO 2: RADIO DE USUARIO (Streaming + Anti-Repetición) ===
        else if (currentStationName != "Radio Apagada")
        {
            if (!userStationsData.ContainsKey(currentStationName) || userStationsData[currentStationName].Songs.Count == 0)
            {
                CancelLoading();
                yield break;
            }

            List<string> songs = userStationsData[currentStationName].Songs;
            string songPath = "";

            // --- ANTI-REPETICIÓN ---
            if (songs.Count == 1)
            {
                songPath = songs[0];
            }
            else
            {
                int attempts = 0;
                do
                {
                    songPath = songs[Random.Range(0, songs.Count)];
                    attempts++;
                }
                while (songPath == _lastPlayedSongPath && attempts < 10);
            }
            _lastPlayedSongPath = songPath;
            // -----------------------

            if (isStationChange && stationChangeSfx != null)
            {
                PlaySFX(stationChangeSfx);
                yield return new WaitForSeconds(stationChangeSfx.length);
            }

            if (requestID != _currentStationRequestID) { CancelLoading(); yield break; }

            // STREAMING SIN LAG
            yield return StartCoroutine(StreamAndPlaySong(songPath, requestID));
        }

        isLoadingSong = false;
    }

    private IEnumerator StreamAndPlaySong(string songPath, int requestID)
    {
        string url = "file://" + songPath;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
        {
            ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                if (requestID != _currentStationRequestID) yield break;
                yield return null;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (requestID != _currentStationRequestID) yield break;

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = clip;
                audioSource.loop = false;
                audioSource.Play();
            }
            else
            {
                Debug.LogError($"Error Streaming: {songPath} | {request.error}");
            }
        }
    }

    private IEnumerator LoadAndAssignCover(string imagePath, int requestID)
    {
        if (string.IsNullOrEmpty(imagePath) || requestID != _currentStationRequestID)
        {
            if (diskCoverImage != null) { diskCoverImage.sprite = defaultDiskSprite; }
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + imagePath))
        {
            yield return request.SendWebRequest();

            if (requestID != _currentStationRequestID) yield break;

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    if (diskCoverImage != null)
                    {
                        diskCoverImage.sprite = sprite;
                    }
                }
            }
            else
            {
                if (diskCoverImage != null) { diskCoverImage.sprite = defaultDiskSprite; }
            }
        }
    }

    private void CancelLoading()
    {
        isLoadingSong = false;
        if (radioDiskRotator != null) { radioDiskRotator.SetRotation(false); }
    }

    private void PlaySFX(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.Play();
    }
}