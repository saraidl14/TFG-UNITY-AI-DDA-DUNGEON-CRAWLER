using UnityEngine;

/// <summary>
/// Gestiona el sistema de puntuacion del juego basado en el DDA.
/// Calcula puntos por sala aplicando multiplicadores de performance,
/// bonificaciones fijas por logros y asigna un rango final al jugador.
///
/// Formulas DDA:
///   Puntos sala = (puntos_base[nivel] + bonus_kills + bonus_boss)
///                 * multiplicador_performance
///   Puntos finales = suma_salas + bonus_fijos (flawless, speedrun, milestones, etc.)
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static ScoreManager Instance { get; private set; }

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
    // TABLAS DDA (documentos TFG)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Puntos base por nivel de dificultad (indices 0-9 = niveles 1-10).
    /// </summary>
    private readonly int[] basePoints = { 100, 150, 225, 350, 550, 850, 1300, 2000, 3200, 5000 };

    // Multiplicadores de performance
    private const float MULT_PERFECT_CLEAR   = 1.5f;  // Sin dano recibido
    private const float MULT_SPEED_CLEAR     = 1.3f;  // Sala completada en menos de 30s
    private const float MULT_OVERKILL        = 1.2f;  // HP restante > 80%
    private const float MULT_NO_ITEMS        = 1.15f; // Sin items usados
    private const float MULT_AFTER_DEATH     = 0.5f;  // Penalizacion: se murio en la sala

    // Bonificaciones fijas
    private const int   BONUS_PER_KILL       = 10;    // Por kill: 10 * nivel_dificultad
    private const int   BONUS_BOSS_BASE      = 10000; // Boss derrotado: 10000 * nivel_dificultad
    private const int   BONUS_FLAWLESS_RUN   = 25000; // Run sin dano en ninguna sala
    private const int   BONUS_SPEEDRUN       = 15000; // Run completa en menos de 10 minutos
    private const int   BONUS_MILESTONE_BASE = 5000;  // Por cada 10 salas: 5000 * (salas/10)
    private const int   BONUS_DIFF_CHAMPION  = 50000; // Llegar al nivel de dificultad 10

    // ─────────────────────────────────────────────
    // TRACKING GLOBAL DE LA PARTIDA
    // ─────────────────────────────────────────────

    /// <summary>Muertes totales del jugador en la partida.</summary>
    public int TotalDeaths { get; private set; } = 0;

    /// <summary>Tiempo total transcurrido en segundos.</summary>
    public float TotalTime { get; private set; } = 0f;

    /// <summary>Numero de salas completadas.</summary>
    public int TotalRooms { get; private set; } = 0;

    /// <summary>Racha de salas perfectas consecutivas (sin dano).</summary>
    public int PerfectStreak { get; private set; } = 0;

    /// <summary>Enemigos totales eliminados en la partida.</summary>
    public int TotalEnemiesKilled { get; private set; } = 0;

    /// <summary>True si el boss ha sido derrotado.</summary>
    public bool DefeatedBoss { get; private set; } = false;

    /// <summary>True si el jugador llego al nivel de dificultad 10.</summary>
    public bool ReachedLevel10 { get; private set; } = false;

    /// <summary>Puntuacion acumulada de todas las salas.</summary>
    public int AccumulatedScore { get; private set; } = 0;

    /// <summary>Puntuacion final tras aplicar todos los bonus.</summary>
    public int FinalScore { get; private set; } = 0;

    // ─────────────────────────────────────────────
    // CALCULO POR SALA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Calcula la puntuacion de una sala segun los parametros de performance.
    /// Acumula el resultado en AccumulatedScore y actualiza el tracking global.
    /// </summary>
    /// <param name="difficultyLevel">Nivel de dificultad actual (1-10).</param>
    /// <param name="damageTaken">Dano recibido durante la sala.</param>
    /// <param name="timeTaken">Segundos tardados en completar la sala.</param>
    /// <param name="hpRemaining">HP del jugador al terminar la sala.</param>
    /// <param name="maxHP">HP maximo del jugador.</param>
    /// <param name="died">True si el jugador murio en esta sala.</param>
    /// <param name="usedItems">True si el jugador uso algun item.</param>
    /// <param name="enemiesKilled">Enemigos eliminados en la sala.</param>
    /// <returns>Puntuacion calculada para la sala.</returns>
    public int CalculateRoomScore(
        int   difficultyLevel,
        float damageTaken,
        float timeTaken,
        float hpRemaining,
        float maxHP,
        bool  died,
        bool  usedItems,
        int   enemiesKilled)
    {
        // Clampar el nivel al rango valido del array
        int levelIndex = Mathf.Clamp(difficultyLevel - 1, 0, basePoints.Length - 1);

        // ── Puntos base del nivel ──────────────────
        int score = basePoints[levelIndex];

        // ── Bonus por kills ────────────────────────
        score += enemiesKilled * BONUS_PER_KILL * difficultyLevel;

        // ── Multiplicadores de performance ─────────
        float multiplier = 1f;

        bool perfectClear = (damageTaken <= 0f);
        bool speedClear   = (timeTaken < 30f);
        bool overkill     = (maxHP > 0f && hpRemaining / maxHP > 0.8f);

        if (perfectClear) multiplier *= MULT_PERFECT_CLEAR;
        if (speedClear)   multiplier *= MULT_SPEED_CLEAR;
        if (overkill)     multiplier *= MULT_OVERKILL;
        if (!usedItems)   multiplier *= MULT_NO_ITEMS;
        if (died)         multiplier *= MULT_AFTER_DEATH;

        score = Mathf.RoundToInt(score * multiplier);

        // ── Actualizar tracking global ─────────────
        TotalRooms++;
        TotalEnemiesKilled += enemiesKilled;
        TotalTime          += timeTaken;

        if (died)
        {
            TotalDeaths++;
            PerfectStreak = 0;
        }
        else if (perfectClear)
        {
            PerfectStreak++;
        }
        else
        {
            PerfectStreak = 0;
        }

        AccumulatedScore += score;

        Debug.Log($"[ScoreManager] Sala {TotalRooms} | Nivel {difficultyLevel} | " +
                  $"Base: {basePoints[levelIndex]} | x{multiplier:F2} | " +
                  $"Puntos sala: {score} | Acumulado: {AccumulatedScore}");

        return score;
    }

    // ─────────────────────────────────────────────
    // REGISTRO DE EVENTOS ESPECIALES
    // ─────────────────────────────────────────────

    /// <summary>Registra que el boss ha sido derrotado.</summary>
    public void RegisterBossDefeated(int difficultyLevel)
    {
        DefeatedBoss = true;
        int bossBonus = BONUS_BOSS_BASE * difficultyLevel;
        AccumulatedScore += bossBonus;
        Debug.Log($"[ScoreManager] Boss derrotado. Bonus: {bossBonus} | Acumulado: {AccumulatedScore}");
    }

    /// <summary>Registra que el jugador alcanzo el nivel de dificultad 10.</summary>
    public void RegisterReachedLevel10()
    {
        if (ReachedLevel10) return; // Evitar duplicados
        ReachedLevel10 = true;
        Debug.Log("[ScoreManager] Nivel de dificultad 10 alcanzado.");
    }

    // ─────────────────────────────────────────────
    // CALCULO FINAL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Calcula la puntuacion final de la partida aplicando
    /// todos los bonus fijos sobre el acumulado de salas.
    /// Debe llamarse al terminar la partida (Game Over o victoria).
    /// </summary>
    /// <returns>Puntuacion final total.</returns>
    public int CalculateFinalScore()
    {
        FinalScore = AccumulatedScore;

        // ── Flawless Run: ninguna sala con dano ───
        // Se cumple si la racha perfecta es igual al total de salas
        bool flawlessRun = (TotalRooms > 0 && PerfectStreak == TotalRooms);
        if (flawlessRun)
        {
            FinalScore += BONUS_FLAWLESS_RUN;
            Debug.Log($"[ScoreManager] Bonus Flawless Run: +{BONUS_FLAWLESS_RUN}");
        }

        // ── Speedrun: partida completa en menos de 10 minutos ───
        if (TotalTime < 600f) // 600s = 10 minutos
        {
            FinalScore += BONUS_SPEEDRUN;
            Debug.Log($"[ScoreManager] Bonus Speedrun (<10 min): +{BONUS_SPEEDRUN}");
        }

        // ── Milestones cada 10 salas ─────────────
        int milestones     = TotalRooms / 10;
        int milestoneBonus = milestones * BONUS_MILESTONE_BASE * milestones; // 5000 * (salas/10)
        if (milestoneBonus > 0)
        {
            FinalScore += milestoneBonus;
            Debug.Log($"[ScoreManager] Bonus Milestone ({milestones} x 10 salas): +{milestoneBonus}");
        }

        // ── Difficulty Champion ───────────────────
        if (ReachedLevel10)
        {
            FinalScore += BONUS_DIFF_CHAMPION;
            Debug.Log($"[ScoreManager] Bonus Difficulty Champion (nivel 10): +{BONUS_DIFF_CHAMPION}");
        }

        Debug.Log($"[ScoreManager] Puntuacion final: {FinalScore} | Rango: {GetRank()}");
        return FinalScore;
    }

    // ─────────────────────────────────────────────
    // SISTEMA DE RANGOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Devuelve el rango del jugador segun la puntuacion final.
    /// Rangos definidos en los documentos DDA del TFG.
    /// </summary>
    public string GetRank()
    {
        if (FinalScore >= 500000) return "Legendario";
        if (FinalScore >= 350000) return "Maestro";
        if (FinalScore >= 200000) return "Diamante";
        if (FinalScore >= 100000) return "Platino";
        if (FinalScore >= 50000)  return "Oro";
        if (FinalScore >= 20000)  return "Plata";
        return "Bronce";
    }

    /// <summary>
    /// Devuelve el rango calculado sobre una puntuacion arbitraria
    /// (util para preview en UI antes de llamar a CalculateFinalScore).
    /// </summary>
    public string GetRankForScore(int score)
    {
        if (score >= 500000) return "Legendario";
        if (score >= 350000) return "Maestro";
        if (score >= 200000) return "Diamante";
        if (score >= 100000) return "Platino";
        if (score >= 50000)  return "Oro";
        if (score >= 20000)  return "Plata";
        return "Bronce";
    }

    // ─────────────────────────────────────────────
    // RESET (nueva partida)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Reinicia todos los contadores. Llamar al volver al menu principal.
    /// </summary>
    public void ResetAll()
    {
        TotalDeaths        = 0;
        TotalTime          = 0f;
        TotalRooms         = 0;
        PerfectStreak      = 0;
        TotalEnemiesKilled = 0;
        DefeatedBoss       = false;
        ReachedLevel10     = false;
        AccumulatedScore   = 0;
        FinalScore         = 0;
        Debug.Log("[ScoreManager] Contadores reseteados.");
    }

    /// <summary>
    /// Resetea el intento actual pero acumula las muertes.
    /// Llamar al pulsar Reintentar para que las muertes se sumen entre intentos.
    /// </summary>
    public void ResetForRetry()
    {
        int savedDeaths    = TotalDeaths; // Guardar muertes acumuladas
        ResetAll();
        TotalDeaths        = savedDeaths; // Restaurar
        Debug.Log($"[ScoreManager] Retry: muertes acumuladas conservadas ({TotalDeaths}).");
    }
}
