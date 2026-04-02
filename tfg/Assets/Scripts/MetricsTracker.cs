using UnityEngine;

/// <summary>
/// Registra las metricas de performance del jugador por sala.
/// Es el punto central de datos para el sistema DDA:
/// DifficultyManager las lee para ajustar la dificultad,
/// ScoreManager las usa para calcular la puntuacion.
///
/// Singleton persistente. Se resetea al entrar en una sala nueva.
/// </summary>
public class MetricsTracker : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static MetricsTracker Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────
    // METRICAS DE LA SALA ACTUAL
    // ─────────────────────────────────────────────

    /// <summary>Dano total recibido en la sala actual.</summary>
    public float DamageTaken { get; private set; } = 0f;

    /// <summary>Tiempo transcurrido en la sala actual (en segundos).</summary>
    public float TimeTaken { get; private set; } = 0f;

    /// <summary>Numero de muertes del jugador en la sala actual.</summary>
    public int DeathCount { get; private set; } = 0;

    /// <summary>Items consumidos en la sala actual.</summary>
    public int ItemsUsed { get; private set; } = 0;

    /// <summary>HP del jugador al finalizar la sala.</summary>
    public float HpRemaining { get; private set; } = 0f;

    /// <summary>Enemigos eliminados en la sala actual.</summary>
    public int EnemiesKilled { get; private set; } = 0;

    // ─────────────────────────────────────────────
    // METRICAS GLOBALES DE LA PARTIDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Salas perfectas consecutivas (sin recibir dano en ninguna).
    /// Se reinicia al recibir dano; ScoreManager lo lee para el bonus Flawless.
    /// </summary>
    public int PerfectStreak { get; private set; } = 0;

    /// <summary>Indica si la sala actual ha sido perfecta hasta el momento.</summary>
    private bool currentRoomWasPerfect = true;

    // ─────────────────────────────────────────────
    // TIMER DE SALA
    // ─────────────────────────────────────────────

    /// <summary>True mientras el timer de sala esta activo.</summary>
    private bool timerRunning = false;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Update()
    {
        if (timerRunning)
            TimeTaken += Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    // CONTROL DE SALA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Resetea todas las metricas de la sala actual.
    /// Debe llamarse al entrar en una sala nueva antes de iniciar el timer.
    /// PlayerCombat tambien resetea su contador de kills.
    /// </summary>
    public void ResetRoomMetrics()
    {
        DamageTaken           = 0f;
        TimeTaken             = 0f;
        DeathCount            = 0;
        ItemsUsed             = 0;
        HpRemaining           = 0f;
        EnemiesKilled         = 0;
        currentRoomWasPerfect = true;
        timerRunning          = false;

        Debug.Log("[MetricsTracker] Metricas de sala reseteadas.");
    }

    /// <summary>
    /// Inicia el temporizador de sala. Llamar cuando el jugador entra en la sala.
    /// </summary>
    public void StartRoomTimer()
    {
        timerRunning = true;
        Debug.Log("[MetricsTracker] Timer de sala iniciado.");
    }

    /// <summary>
    /// Detiene el temporizador de sala. Llamar al completar la sala.
    /// </summary>
    public void StopRoomTimer()
    {
        timerRunning = false;
        Debug.Log($"[MetricsTracker] Timer detenido. Tiempo de sala: {TimeTaken:F2}s");
    }

    /// <summary>
    /// Cierra la sala: detiene el timer, guarda el HP restante
    /// y actualiza la racha de salas perfectas.
    /// </summary>
    /// <param name="hpRemaining">HP del jugador al terminar la sala.</param>
    public void CloseRoom(float hpRemaining)
    {
        StopRoomTimer();
        HpRemaining = hpRemaining;

        if (currentRoomWasPerfect)
            PerfectStreak++;
        else
            PerfectStreak = 0;

        Debug.Log($"[MetricsTracker] Sala cerrada | " +
                  $"Dano: {DamageTaken} | Tiempo: {TimeTaken:F2}s | " +
                  $"Muertes: {DeathCount} | Items: {ItemsUsed} | " +
                  $"Kills: {EnemiesKilled} | HP restante: {HpRemaining} | " +
                  $"Racha perfecta: {PerfectStreak}");
    }

    // ─────────────────────────────────────────────
    // REGISTRO DE EVENTOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Registra dano recibido por el jugador.
    /// Llamado desde PlayerHealth.TakeDamage.
    /// </summary>
    public void RegisterDamage(float amount)
    {
        DamageTaken           += amount;
        currentRoomWasPerfect  = false; // La sala ya no es perfecta
        Debug.Log($"[MetricsTracker] Dano registrado: {amount} | Total sala: {DamageTaken}");
    }

    /// <summary>
    /// Registra la muerte del jugador en la sala actual.
    /// Llamado desde PlayerHealth.Die (via OnPlayerDeath o directamente).
    /// </summary>
    public void RegisterDeath()
    {
        DeathCount++;
        currentRoomWasPerfect = false;
        Debug.Log($"[MetricsTracker] Muerte registrada. Total en sala: {DeathCount}");
    }

    /// <summary>
    /// Registra el uso de un item (pocion, etc.).
    /// Llamado desde el sistema de inventario al consumir un item.
    /// </summary>
    public void RegisterItemUsed()
    {
        ItemsUsed++;
        Debug.Log($"[MetricsTracker] Item usado. Total en sala: {ItemsUsed}");
    }

    /// <summary>
    /// Registra un enemigo eliminado en la sala actual.
    /// Llamado desde PlayerCombat al detectar que un enemigo llego a 0 HP.
    /// </summary>
    public void RegisterKill()
    {
        EnemiesKilled++;
        Debug.Log($"[MetricsTracker] Kill registrado. Total en sala: {EnemiesKilled}");
    }

    // ─────────────────────────────────────────────
    // CONSULTAS PARA DDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// True si el jugador no ha recibido dano en la sala actual.
    /// Usado por DifficultyManager para subir la dificultad.
    /// </summary>
    public bool IsCurrentRoomPerfect() => currentRoomWasPerfect;

    /// <summary>
    /// True si el jugador completo la sala en menos de 30 segundos.
    /// </summary>
    public bool IsSpeedClear() => TimeTaken < 30f;

    /// <summary>
    /// True si el jugador no ha usado ningun item en la sala.
    /// </summary>
    public bool IsNoItemsRun() => ItemsUsed == 0;

    /// <summary>
    /// Devuelve un resumen de las metricas actuales para depuracion.
    /// </summary>
    public string GetSummary()
    {
        return $"Dano={DamageTaken} | Tiempo={TimeTaken:F1}s | " +
               $"Muertes={DeathCount} | Items={ItemsUsed} | " +
               $"Kills={EnemiesKilled} | HP={HpRemaining} | " +
               $"RachaPerfecta={PerfectStreak}";
    }
}
