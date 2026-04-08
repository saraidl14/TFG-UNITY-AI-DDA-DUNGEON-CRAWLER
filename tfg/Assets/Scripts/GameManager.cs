using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor central del juego. Singleton persistente entre escenas.
///
/// Responsabilidades:
/// - Suscribirse a PlayerHealth.OnPlayerDeath y BossController.OnBossDead.
/// - Al morir el jugador: guardar monedas y pociones con PlayerPrefs,
///   calcular puntuacion y cargar la escena de Game Over.
/// - Al derrotar al boss: calcular score de sala y gestionar la transicion.
/// - Expone monedas acumuladas y pociones no usadas que sobreviven a la muerte.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Buscar el panel y su Canvas entre los hijos (incluye inactivos)
        _dungeonClearedPanel = GetComponentInChildren<DungeonClearedPanel>(true);
        _dungeonCanvas       = GetComponentInChildren<Canvas>(true);

        if (_dungeonClearedPanel != null)
            Debug.Log($"[GameManager] DungeonClearedPanel encontrado en: {_dungeonClearedPanel.gameObject.name}");
        else
            Debug.LogError("[GameManager] DungeonClearedPanel NO encontrado. Asegurate de que el script esta en un hijo del GameController.");

        if (_dungeonCanvas != null)
            Debug.Log($"[GameManager] Canvas del panel encontrado: {_dungeonCanvas.gameObject.name}");
        else
            Debug.LogError("[GameManager] Canvas del DungeonPanel NO encontrado.");

        // Cargar datos persistentes de partidas anteriores
        LoadPersistentData();
    }

    // ─────────────────────────────────────────────
    // CONFIGURACION DE ESCENAS
    // ─────────────────────────────────────────────
    [Header("Nombres de Escenas")]
    [Tooltip("Nombre exacto de la escena de juego principal en Build Settings.")]
    public string gameSceneName = "GameScene";

    [Tooltip("Nombre exacto de la escena de Game Over en Build Settings.")]
    public string gameOverSceneName = "GameOverScene";

    // ─────────────────────────────────────────────
    // NIVEL DE DIFICULTAD ACTUAL
    // ─────────────────────────────────────────────

    /// <summary>Nivel de dificultad actual, gestionado por DifficultyManager.</summary>
    public int CurrentDifficultyLevel { get; set; } = 1;

    // ─────────────────────────────────────────────
    // PANEL DE MAZMORRA COMPLETADA
    // ─────────────────────────────────────────────

    private DungeonClearedPanel _dungeonClearedPanel;
    private Canvas              _dungeonCanvas;

    // ─────────────────────────────────────────────
    // TRACKING POR MAZMORRA
    // ─────────────────────────────────────────────

    /// <summary>Numero de mazmorra actual (empieza en 1).</summary>
    public int DungeonNumber { get; private set; } = 1;

    /// <summary>Tiempo transcurrido en la ultima mazmorra completada.</summary>
    public float LastDungeonTime   { get; private set; } = 0f;

    /// <summary>Enemigos eliminados en la ultima mazmorra completada.</summary>
    public int LastDungeonKills    { get; private set; } = 0;

    /// <summary>Monedas ganadas en la ultima mazmorra completada.</summary>
    public int LastDungeonCoins    { get; private set; } = 0;

    /// <summary>Puntuacion obtenida en la ultima mazmorra completada.</summary>
    public int LastDungeonScore    { get; private set; } = 0;

    // Snapshots al inicio de cada mazmorra para calcular el delta
    private float _dungeonStartTime  = 0f;
    private int   _dungeonStartKills = 0;
    private int   _dungeonStartCoins = 0;
    private int   _dungeonStartScore = 0;

    // ─────────────────────────────────────────────
    // BOOST DE VELOCIDAD (persiste entre mazmorras, se pierde al morir)
    // ─────────────────────────────────────────────

    [Header("Boost de Velocidad")]
    [Tooltip("Velocidad extra acumulada por cada boss derrotado.")]
    public float speedBoostPerBoss = 0.5f;

    [Tooltip("Maximo boost de velocidad acumulable en un run.")]
    public float maxSpeedBoost = 3f;

    /// <summary>Boost acumulado en el run actual. Se aplica al spawnear un nuevo jugador.</summary>
    public float AccumulatedSpeedBoost { get; private set; } = 0f;

    /// <summary>Suma un incremento al boost. Llamado al derrotar al boss.</summary>
    public void AddSpeedBoost()
    {
        AccumulatedSpeedBoost = Mathf.Min(AccumulatedSpeedBoost + speedBoostPerBoss, maxSpeedBoost);
        Debug.Log($"[GameManager] Speed boost acumulado: {AccumulatedSpeedBoost} / {maxSpeedBoost}");
    }

    /// <summary>Reinicia el boost a 0. Llamado al morir el jugador.</summary>
    public void ResetSpeedBoost()
    {
        AccumulatedSpeedBoost = 0f;
        Debug.Log("[GameManager] Speed boost reiniciado por muerte.");
    }

    // ─────────────────────────────────────────────
    // PERSISTENCIA: MONEDAS Y POCIONES
    // ─────────────────────────────────────────────

    // Claves de PlayerPrefs
    private const string KEY_COINS   = "PersistentCoins";
    private const string KEY_POTIONS = "PersistentPotions";

    /// <summary>
    /// Monedas acumuladas que sobreviven a la muerte del jugador.
    /// Un porcentaje se guarda al morir (ver SaveOnDeath).
    /// </summary>
    public int PersistentCoins { get; private set; } = 0;

    /// <summary>
    /// Pociones no usadas que sobreviven a la muerte.
    /// </summary>
    public int PersistentPotions { get; private set; } = 0;

    /// <summary>Monedas de la sesion actual (se suman al persistente al morir).</summary>
    public int SessionCoins { get; set; } = 0;

    /// <summary>Pociones no usadas en la sesion actual.</summary>
    public int SessionPotions { get; set; } = 0;

    /// <summary>
    /// Añade monedas a la sesion actual.
    /// Llamado por EnemyBase al morir un enemigo y por EnemiesControllers con el bonus de limpieza.
    /// </summary>
    public void AddCoins(int amount)
    {
        SessionCoins += amount;
        Debug.Log($"[GameManager] +{amount} monedas | Total sesion: {SessionCoins}");
    }

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        // Suscribirse a los eventos de muerte
        PlayerHealth.OnPlayerDeath  += HandlePlayerDeath;
        BossController.OnBossDead   += HandleBossDefeated;
    }

    private void OnDisable()
    {
        // Desuscribirse para evitar memory leaks
        PlayerHealth.OnPlayerDeath  -= HandlePlayerDeath;
        BossController.OnBossDead   -= HandleBossDefeated;
    }

    // ─────────────────────────────────────────────
    // MANEJADORES DE EVENTOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado cuando el jugador muere.
    /// Guarda recursos con PlayerPrefs, actualiza metricas y carga Game Over.
    /// </summary>
    private void HandlePlayerDeath()
    {
        Debug.Log("[GameManager] Jugador muerto. Procesando...");

        // Guardar recursos que sobreviven a la muerte
        SaveOnDeath();

        // Calcular puntuacion final con penalizacion de muerte
        if (ScoreManager.Instance != null)
        {
            // La ultima sala se calcula con el flag 'died = true'
            // (MetricsTracker tiene los datos de la sala actual)
            if (MetricsTracker.Instance != null)
            {
                MetricsTracker mt = MetricsTracker.Instance;
                ScoreManager.Instance.CalculateRoomScore(
                    difficultyLevel: CurrentDifficultyLevel,
                    damageTaken:     mt.DamageTaken,
                    timeTaken:       mt.TimeTaken,
                    hpRemaining:     0f,
                    maxHP:           100f,
                    died:            true,
                    usedItems:       mt.ItemsUsed > 0,
                    enemiesKilled:   mt.EnemiesKilled
                );
            }

            ScoreManager.Instance.CalculateFinalScore();
        }

        LoadGameOverScene();
    }

    /// <summary>
    /// Llamado cuando el boss es derrotado.
    /// Registra el logro, calcula score de la sala y gestiona la transicion.
    /// </summary>
    private void HandleBossDefeated()
    {
        Debug.Log("[GameManager] Boss derrotado.");
        OnBossDefeated();
    }

    /// <summary>
    /// Logica de transicion tras derrotar al boss.
    /// Calcula score de la sala con los datos del MetricsTracker.
    /// </summary>
    public void OnBossDefeated()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterBossDefeated(CurrentDifficultyLevel);

            if (MetricsTracker.Instance != null)
            {
                MetricsTracker mt = MetricsTracker.Instance;

                // Obtener HP actual del jugador para el calculo
                float hpRemaining = 0f;
                float maxHP       = 100f;
                PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
                if (playerHealth != null)
                {
                    hpRemaining = playerHealth.GetCurrentHealth();
                    maxHP       = playerHealth.GetMaxHealth();
                }

                ScoreManager.Instance.CalculateRoomScore(
                    difficultyLevel: CurrentDifficultyLevel,
                    damageTaken:     mt.DamageTaken,
                    timeTaken:       mt.TimeTaken,
                    hpRemaining:     hpRemaining,
                    maxHP:           maxHP,
                    died:            mt.DeathCount > 0,
                    usedItems:       mt.ItemsUsed > 0,
                    enemiesKilled:   mt.EnemiesKilled
                );
            }
        }

        // Verificar si se alcanzo el nivel maximo
        if (CurrentDifficultyLevel >= 10 && ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterReachedLevel10();

        // ── Capturar stats de esta mazmorra para el panel ──
        if (ScoreManager.Instance != null)
        {
            LastDungeonTime  = ScoreManager.Instance.TotalTime          - _dungeonStartTime;
            LastDungeonKills = ScoreManager.Instance.TotalEnemiesKilled - _dungeonStartKills;
            LastDungeonScore = ScoreManager.Instance.AccumulatedScore   - _dungeonStartScore;
        }
        LastDungeonCoins = SessionCoins - _dungeonStartCoins;

        // ── Acumular boost de velocidad ──
        AddSpeedBoost();
        Debug.Log($"[GameManager] Mazmorra {DungeonNumber} completada. Boost acumulado: {AccumulatedSpeedBoost}");

        // ── Mostrar panel de mazmorra completada ──
        if (_dungeonClearedPanel != null)
        {
            // Activar el Canvas padre desde GameManager por si estuviera inactivo
            if (_dungeonCanvas != null)
                _dungeonCanvas.gameObject.SetActive(true);

            _dungeonClearedPanel.Show();
        }
        else
        {
            Debug.LogWarning("[GameManager] DungeonClearedPanel no encontrado. Cargando siguiente mazmorra directamente.");
            DungeonNumber++;
            LoadGameScene();
        }
    }

    // ─────────────────────────────────────────────
    // PERSISTENCIA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Guarda monedas y pociones al morir.
    /// Se conserva el 100% de las pociones no usadas
    /// y el 50% de las monedas de la sesion como penalizacion.
    /// </summary>
    private void SaveOnDeath()
    {
        // Pociones: se guardan integras
        PersistentPotions += SessionPotions;

        // Monedas: se pierde el 50% al morir
        int coinsToSave = Mathf.FloorToInt(SessionCoins * 0.5f);
        PersistentCoins += coinsToSave;

        // Persistir en disco
        PlayerPrefs.SetInt(KEY_COINS,   PersistentCoins);
        PlayerPrefs.SetInt(KEY_POTIONS, PersistentPotions);
        PlayerPrefs.Save();

        Debug.Log($"[GameManager] Guardado al morir | " +
                  $"Pociones guardadas: {SessionPotions} | " +
                  $"Monedas guardadas: {coinsToSave} (50% de {SessionCoins}) | " +
                  $"Total persistente -> Monedas: {PersistentCoins} | Pociones: {PersistentPotions}");

        // Resetear sesion
        SessionCoins   = 0;
        SessionPotions = 0;
    }

    /// <summary>
    /// Carga los datos persistentes guardados con PlayerPrefs.
    /// </summary>
    private void LoadPersistentData()
    {
        PersistentCoins   = PlayerPrefs.GetInt(KEY_COINS,   0);
        PersistentPotions = PlayerPrefs.GetInt(KEY_POTIONS, 0);

        Debug.Log($"[GameManager] Datos cargados | " +
                  $"Monedas persistentes: {PersistentCoins} | " +
                  $"Pociones persistentes: {PersistentPotions}");
    }

    /// <summary>
    /// Resetea los datos persistentes (nueva partida desde cero).
    /// </summary>
    public void ResetPersistentData()
    {
        PersistentCoins   = 0;
        PersistentPotions = 0;
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.DeleteKey(KEY_POTIONS);
        PlayerPrefs.Save();
        Debug.Log("[GameManager] Datos persistentes eliminados.");
    }

    // ─────────────────────────────────────────────
    // NAVEGACION DE ESCENAS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Carga la escena principal del juego.
    /// Resetea las metricas de sala si MetricsTracker esta disponible.
    /// </summary>
    public void LoadGameScene()
    {
        // Incrementar contador de mazmorra ANTES de cargar la nueva
        DungeonNumber++;

        Debug.Log($"[GameManager] Cargando mazmorra {DungeonNumber}: {gameSceneName}");

        // Guardar snapshot para calcular el delta de la proxima mazmorra
        _dungeonStartTime  = ScoreManager.Instance != null ? ScoreManager.Instance.TotalTime          : 0f;
        _dungeonStartKills = ScoreManager.Instance != null ? ScoreManager.Instance.TotalEnemiesKilled : 0;
        _dungeonStartCoins = SessionCoins;
        _dungeonStartScore = ScoreManager.Instance != null ? ScoreManager.Instance.AccumulatedScore   : 0;

        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.ResetRoomMetrics();

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Carga la escena de Game Over.
    /// ScoreManager retiene la puntuacion para que la UI la lea.
    /// </summary>
    public void LoadGameOverScene()
    {
        Debug.Log($"[GameManager] Cargando escena Game Over: {gameOverSceneName}");
        SceneManager.LoadScene(gameOverSceneName);
    }
}
