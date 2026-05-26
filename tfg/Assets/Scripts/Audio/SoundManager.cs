/*  Nombre:      SoundManager.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/05/2026
 *  Descripcion: Gestor centralizado de efectos de sonido (SFX).
 *               Singleton DontDestroyOnLoad.
 *
 *  SETUP EN UNITY:
 *    1. Crea un GameObject vacío "SoundManager" en la escena inicial (MainMenu o GameScene).
 *    2. Añade este script.
 *    3. Asigna los clips de audio en el Inspector.
 *    4. Los pasos necesitan al menos 1 clip en el array 'footsteps'.
 */

using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static SoundManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // CLIPS
    // ─────────────────────────────────────────────
    [Header("Combate — jugador")]
    [Tooltip("Sonido al golpear con espada o daga.")]
    public AudioClip swordHit;

    [Tooltip("Sonido al golpear con los puños.")]
    public AudioClip punchHit;

    [Tooltip("Sonidos cuando el jugador recibe daño (se elige uno al azar en cada golpe).")]
    public AudioClip[] playerHurtClips;

    [Header("Interacción")]
    [Tooltip("Sonido al abrir un cofre.")]
    public AudioClip chestOpen;

    [Header("Movimiento")]
    [Tooltip("Clips de pasos (se eligen al azar para dar variedad). Mínimo 1.")]
    public AudioClip[] footsteps;

    [Tooltip("Tiempo en segundos entre cada paso. Ajústalo al ritmo de la animación de caminar.")]
    public float footstepInterval = 0.4f;

    [Header("Jefe")]
    [Tooltip("Gruñido del boss al detectar al jugador por primera vez.")]
    public AudioClip bossGrowl;

    // ─────────────────────────────────────────────
    // VOLÚMENES
    // ─────────────────────────────────────────────
    [Header("Volúmenes")]
    [Range(0f, 1f)] public float sfxVolume      = 0.8f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    // ─────────────────────────────────────────────
    // AUDIO SOURCES (se crean automáticamente)
    // ─────────────────────────────────────────────
    private AudioSource _sfxSource;
    private AudioSource _footstepSource;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────
    private float _nextFootstepTime = 0f;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear dos AudioSources: uno para SFX generales y otro para pasos
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.playOnAwake = false;
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>Reproduce el sonido de golpe de espada o daga.</summary>
    public void PlaySwordHit()
    {
        if (swordHit != null)
            _sfxSource.PlayOneShot(swordHit, sfxVolume);
    }

    /// <summary>Reproduce el sonido de golpe de puño.</summary>
    public void PlayPunchHit()
    {
        if (punchHit != null)
            _sfxSource.PlayOneShot(punchHit, sfxVolume);
    }

    /// <summary>Reproduce un sonido de daño aleatorio del array playerHurtClips.</summary>
    public void PlayPlayerHurt()
    {
        if (playerHurtClips == null || playerHurtClips.Length == 0) return;
        AudioClip clip = playerHurtClips[Random.Range(0, playerHurtClips.Length)];
        if (clip != null)
            _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>Reproduce el sonido de apertura de cofre.</summary>
    public void PlayChestOpen()
    {
        if (chestOpen != null)
            _sfxSource.PlayOneShot(chestOpen, sfxVolume);
    }

    /// <summary>Reproduce el gruñido del boss.</summary>
    public void PlayBossGrowl()
    {
        if (bossGrowl != null)
            _sfxSource.PlayOneShot(bossGrowl, sfxVolume);
    }

    /// <summary>Inicia el loop de pasos si no estaba sonando.</summary>
    public void StartFootsteps()
    {
        if (footsteps == null || footsteps.Length == 0) return;
        if (_footstepSource.isPlaying) return;

        _footstepSource.clip   = footsteps[Random.Range(0, footsteps.Length)];
        _footstepSource.loop   = true;
        _footstepSource.volume = footstepVolume;
        _footstepSource.Play();
    }

    /// <summary>Para el loop de pasos.</summary>
    public void StopFootsteps()
    {
        if (_footstepSource.isPlaying)
            _footstepSource.Stop();
    }
}
