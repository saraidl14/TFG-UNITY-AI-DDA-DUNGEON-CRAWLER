/*  Nombre:      DynamicMusicManager.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       12/05/2026
 *  Descripcion: Musica dinamica basada en la vida del jugador.
 *               Con mucha vida → tempo normal. Poca vida → tempo lento / pista tensa.
 *
 *  SETUP EN UNITY:
 *    1. Crea un GameObject vacío "DynamicMusic" en la escena de juego.
 *    2. Añade este script y dos AudioSource al mismo GameObject
 *       (o arrastra los AudioSource desde hijos).
 *    3. Asigna:
 *         musicNormal     → clip de musica normal (velocidad 1x)
 *         musicLowHealth  → clip de musica tensa (poca vida)
 *         audioNormal     → AudioSource para el clip normal
 *         audioLowHealth  → AudioSource para el clip de poca vida
 *    4. Ambos AudioSources deben tener Loop = true.
 *    5. Arrastra el PlayerHealth del jugador a 'playerHealth'
 *       (o el script lo busca solo si no se asigna).
 *
 *  LÓGICA:
 *    - Si HP >= umbralAlta  → solo musica normal al 100%
 *    - Si umbralBaja < HP < umbralAlta → crossfade gradual
 *    - Si HP <= umbralBaja  → solo musica tensa al 100%
 */

using UnityEngine;

/// <summary>
/// Gestor de musica dinamica: hace crossfade entre dos pistas segun
/// el porcentaje de vida del jugador.
/// </summary>
public class DynamicMusicManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static DynamicMusicManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // CLIPS Y SOURCES
    // ─────────────────────────────────────────────
    [Header("Clips de musica")]
    [Tooltip("Musica normal (jugador con mucha vida).")]
    public AudioClip musicNormal;

    [Tooltip("Musica tensa (jugador con poca vida).")]
    public AudioClip musicLowHealth;

    [Header("AudioSources (asignar o se crean automaticamente)")]
    [Tooltip("AudioSource para la musica normal.")]
    public AudioSource audioNormal;

    [Tooltip("AudioSource para la musica tensa.")]
    public AudioSource audioLowHealth;

    // ─────────────────────────────────────────────
    // PARAMETROS DE TRANSICION
    // ─────────────────────────────────────────────
    [Header("Umbrales de vida (0-1)")]
    [Tooltip("Por encima de este porcentaje de vida → musica normal al 100%.")]
    [Range(0f, 1f)] public float umbralAlta  = 0.6f;

    [Tooltip("Por debajo de este porcentaje de vida → musica tensa al 100%.")]
    [Range(0f, 1f)] public float umbralBaja  = 0.3f;

    [Header("Velocidad del crossfade")]
    [Tooltip("Cuanto de rapido transiciona el volumen (unidades por segundo).")]
    public float fadeSpeed = 1f;

    [Header("Volumen maximo de cada pista")]
    [Range(0f, 1f)] public float maxVolume = 0.8f;

    // ─────────────────────────────────────────────
    // REFERENCIA AL JUGADOR
    // ─────────────────────────────────────────────
    [Header("Referencia al jugador (opcional, se busca automaticamente)")]
    public PlayerHealth playerHealth;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
            Debug.LogWarning("[DynamicMusicManager] PlayerHealth no encontrado. La musica no cambiara con la vida.");

        StartMusic();
    }

    private void Update()
    {
        UpdateCrossfade();
    }

    // ─────────────────────────────────────────────
    // LOGICA
    // ─────────────────────────────────────────────

    private void StartMusic()
    {
        if (audioNormal    != null && musicNormal    != null) { audioNormal.clip    = musicNormal;    audioNormal.loop    = true; audioNormal.Play(); }
        if (audioLowHealth != null && musicLowHealth != null) { audioLowHealth.clip = musicLowHealth; audioLowHealth.loop = true; audioLowHealth.Play(); }

        // Empezar con musica normal a volumen maximo y tensa a 0
        if (audioNormal    != null) audioNormal.volume    = maxVolume;
        if (audioLowHealth != null) audioLowHealth.volume = 0f;
    }

    private void UpdateCrossfade()
    {
        if (playerHealth == null) return;

        float hpRatio = playerHealth.CurrentHealth / playerHealth.maxHealth;
        hpRatio = Mathf.Clamp01(hpRatio);

        // Calcular volumen objetivo de la pista tensa (0 = mucha vida, 1 = poca vida)
        float targetLowVolume;

        if (hpRatio >= umbralAlta)
            targetLowVolume = 0f;
        else if (hpRatio <= umbralBaja)
            targetLowVolume = 1f;
        else
            // Interpolacion lineal entre los dos umbrales
            targetLowVolume = 1f - Mathf.InverseLerp(umbralBaja, umbralAlta, hpRatio);

        float targetNormalVolume = 1f - targetLowVolume;

        // Crossfade suave
        if (audioNormal    != null)
            audioNormal.volume    = Mathf.MoveTowards(audioNormal.volume,    targetNormalVolume * maxVolume, fadeSpeed * Time.deltaTime);

        if (audioLowHealth != null)
            audioLowHealth.volume = Mathf.MoveTowards(audioLowHealth.volume, targetLowVolume    * maxVolume, fadeSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────

    private void EnsureAudioSources()
    {
        if (audioNormal == null)
        {
            audioNormal = gameObject.AddComponent<AudioSource>();
            audioNormal.playOnAwake = false;
        }
        if (audioLowHealth == null)
        {
            audioLowHealth = gameObject.AddComponent<AudioSource>();
            audioLowHealth.playOnAwake = false;
        }
    }

    /// <summary>
    /// Detiene ambas pistas (por ejemplo, al entrar en el menu principal).
    /// </summary>
    public void StopMusic()
    {
        audioNormal?.Stop();
        audioLowHealth?.Stop();
    }

    /// <summary>
    /// Reanuda la musica tras una pausa (SceneLoad, etc.).
    /// </summary>
    public void ResumeMusic()
    {
        if (audioNormal    != null && !audioNormal.isPlaying)    audioNormal.Play();
        if (audioLowHealth != null && !audioLowHealth.isPlaying) audioLowHealth.Play();
    }
}
