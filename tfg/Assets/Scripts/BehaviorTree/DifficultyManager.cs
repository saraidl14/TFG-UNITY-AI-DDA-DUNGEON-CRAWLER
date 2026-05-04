using UnityEngine;

/// <summary>
/// Gestor central del sistema DDA (Dynamic Difficulty Adjustment).
/// Implementa las 6 reglas DDA v2.0 con umbrales multiples y modificadores de contexto.
///
/// v3.0 - DDA bandado: el jugador elige dificultad inicial y el sistema solo puede
///         moverse +-2 niveles respecto a su eleccion.
///         Mecanica compensatoria: si el nivel llega al minimo de banda y el jugador
///         sigue teniendo dificultades durante 2+ salas, se activa ShouldBoostLoot
///         para que los cofres suelten loot premium.
///
/// Flujo de uso:
///   1. Al terminar cada sala llamar a EvaluateDifficulty()
///   2. Lee MetricsTracker, calcula el score DDA ponderado
///   3. Aplica la decision con modificadores de contexto
///   4. EnemiesControllers escala los enemigos activos
///
/// Reglas y pesos (doc DDA v2.0):
///   PlayerDamageRule    25%
///   EnemyKillSpeedRule  20%
///   HealthRemainingRule 20%
///   DeathCountRule      25%
///   PerfectStreakRule   15%
///   ItemUsageRule       10%
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static DifficultyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────
    // NIVEL DE DIFICULTAD
    // ─────────────────────────────────────────────

    [Header("Dificultad")]
    [Range(1, 10)]
    public int currentLevel = 5;

    public const int MIN_LEVEL = 1;
    public const int MAX_LEVEL = 10;

    private int _roomsSinceLastAdjustment = 0;
    private int _lastAdjustment           = 0;

    /// <summary>Ultimo ajuste aplicado. Positivo = subio, negativo = bajo, 0 = mantuvo.</summary>
    public int LastAdjustment => _lastAdjustment;

    // ─────────────────────────────────────────────
    // BANDA DE DIFICULTAD (v3.0)
    // ─────────────────────────────────────────────

    [Header("Banda de dificultad (se fija al elegir dificultad)")]
    [Tooltip("Nivel minimo que el DDA puede alcanzar (elegido - 2).")]
    public int minDifficulty = MIN_LEVEL;

    [Tooltip("Nivel maximo que el DDA puede alcanzar (elegido + 2).")]
    public int maxDifficulty = MAX_LEVEL;

    // ─────────────────────────────────────────────
    // MECANICA COMPENSATORIA (v3.0)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Salas consecutivas en que el nivel esta en el minimo de banda
    /// y el jugador sigue teniendo un score DDA negativo (< -8).
    /// Al llegar a 2, se activa ShouldBoostLoot.
    /// </summary>
    private int _struggleStreak = 0;

    /// <summary>Score DDA de la sala mas reciente (guardado en EvaluateDifficulty).</summary>
    private float _lastDDAScore = 0f;

    /// <summary>
    /// Cuando es true, LootTable.Roll() usara la tabla premium en lugar de la normal.
    /// Se activa si el jugador lleva 2+ salas en el nivel minimo de banda con score negativo.
    /// </summary>
    public bool ShouldBoostLoot { get; private set; } = false;

    // ─────────────────────────────────────────────
    // TABLAS DE SCALING (doc Game_Balance_10_Difficulty_Levels)
    // ─────────────────────────────────────────────

    private readonly float[] _hpScaling =
        { 0.8f, 1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f, 2.3f, 2.6f, 3.0f };

    private readonly float[] _damageScaling =
        { 0.7f, 0.9f, 1.0f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f, 2.3f, 2.5f };

    private readonly float[] _speedScaling =
        { 0.9f, 1.0f, 1.0f, 1.1f, 1.2f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f };

    private readonly int[]   _minEnemies =
        { 2, 3, 4, 5, 6, 7, 8, 10, 12, 15 };

    private readonly int[]   _maxEnemies =
        { 3, 4, 5, 6, 8, 9, 10, 12, 15, 20 };

    private readonly float[] _itemDropChance =
        { 0.80f, 0.70f, 0.60f, 0.50f, 0.45f, 0.40f, 0.35f, 0.30f, 0.25f, 0.20f };

    // ─────────────────────────────────────────────
    // PESOS DE LAS 6 REGLAS
    // ─────────────────────────────────────────────

    private const float W_DAMAGE     = 0.25f;
    private const float W_KILL_SPEED = 0.20f;
    private const float W_HEALTH     = 0.20f;
    private const float W_DEATHS     = 0.25f;
    private const float W_STREAK     = 0.15f;
    private const float W_ITEMS      = 0.10f;

    // ─────────────────────────────────────────────
    // CONFIGURAR DIFICULTAD INICIAL (v3.0)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado desde MainMenu al elegir dificultad.
    /// Fija el nivel inicial y calcula la banda +-2 (clampeada a [1, 10]).
    ///
    /// Tabla de bandas:
    ///   Facil      (2) → banda [1,  4]
    ///   Normal     (5) → banda [3,  7]
    ///   Dificil    (7) → banda [5, 10]
    ///   Muy Dificil(10)→ banda [8, 10]
    /// </summary>
    public void SetStartingDifficulty(int chosen)
    {
        currentLevel  = Mathf.Clamp(chosen, MIN_LEVEL, MAX_LEVEL);
        minDifficulty = Mathf.Max(MIN_LEVEL, currentLevel - 2);
        maxDifficulty = Mathf.Min(MAX_LEVEL, currentLevel + 2);

        // Reiniciar mecanica compensatoria
        _struggleStreak = 0;
        ShouldBoostLoot = false;
        _lastDDAScore   = 0f;

        Debug.Log($"[DifficultyManager] Dificultad elegida: {currentLevel} | Banda: [{minDifficulty}, {maxDifficulty}]");

        // Propagar al GameManager si ya existe
        if (GameManager.Instance != null)
            GameManager.Instance.CurrentDifficultyLevel = currentLevel;
    }

    // ─────────────────────────────────────────────
    // EVALUACION PRINCIPAL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Evalua las 6 reglas DDA al terminar una sala y ajusta el nivel.
    /// Llamar desde EnemiesControllers o GameManager al completar la sala.
    /// </summary>
    public void EvaluateDifficulty()
    {
        if (MetricsTracker.Instance == null)
        {
            Debug.LogWarning("[DifficultyManager] MetricsTracker no encontrado.");
            return;
        }

        MetricsTracker mt = MetricsTracker.Instance;

        float totalScore = 0f;
        totalScore += EvaluatePlayerDamageRule(mt.DamageTaken);
        totalScore += EvaluateKillSpeedRule(mt.TimeTaken);
        totalScore += EvaluateHealthRemainingRule(mt.HpRemaining);
        totalScore += EvaluateDeathCountRule(mt.DeathCount);
        totalScore += EvaluatePerfectStreakRule(mt.PerfectStreak);
        totalScore += EvaluateItemUsageRule(mt.ItemsUsed);

        // Guardar el score para la mecanica compensatoria
        _lastDDAScore = totalScore;

        Debug.Log($"[DifficultyManager] Score DDA: {totalScore:F2} " +
                  $"(dano={mt.DamageTaken} tiempo={mt.TimeTaken:F1}s " +
                  $"HP={mt.HpRemaining} muertes={mt.DeathCount} " +
                  $"streak={mt.PerfectStreak} items={mt.ItemsUsed})");

        // Solo guardar el score — el BT-DDA en EnemiesControllers.EvaluateDDATree()
        // es quien aplica el ajuste. Separar calculo de decision es el punto del TFG.
        mt.SetDDAScore(totalScore);
    }

    // ─────────────────────────────────────────────
    // 6 REGLAS DDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Regla 1 — PlayerDamageRule
    /// Solo bonifica si no recibiste dano. Sin castigo: el HP restante ya refleja el dano recibido.
    /// </summary>
    private float EvaluatePlayerDamageRule(float damageTaken)
    {
        float maxHP = 100f;
        if (damageTaken <= 0f)          return 15f;  // Sin dano: excelente
        if (damageTaken < maxHP * 0.2f) return  5f;  // Dano leve
        return 0f;                                    // Dano alto: neutro (el HP restante ya penaliza)
    }

    /// <summary>
    /// Regla 2 — EnemyKillSpeedRule
    /// Ganar rapido es el principal indicador de que el jugador domina el nivel.
    /// </summary>
    private float EvaluateKillSpeedRule(float timeTaken)
    {
        if (timeTaken < 20f)  return 18f;  // Muy rapido: domina
        if (timeTaken < 40f)  return 12f;  // Rapido: bien
        if (timeTaken < 60f)  return 10f;  // Menos de 1 min: garantiza +1 aunque HP sea baja
        if (timeTaken <= 90f) return  3f;  // Normal: ligero positivo
        if (timeTaken > 120f) return -5f;  // Muy lento: costo mucho
        return 0f;
    }

    /// <summary>
    /// Regla 3 — HealthRemainingRule
    /// Bonifica HP alta, penaliza ligeramente HP critica.
    /// </summary>
    private float EvaluateHealthRemainingRule(float hpRemaining)
    {
        if (hpRemaining >= 100f) return 10f;  // HP llena
        if (hpRemaining >= 70f)  return  5f;  // HP alta
        if (hpRemaining > 10f)   return  0f;  // HP media/baja: neutro
        return -8f;                            // HP critica (<=10): casi muerto
    }

    /// <summary>
    /// Regla 4 — DeathCountRule — senal mas importante de dificultad excesiva.
    /// </summary>
    private float EvaluateDeathCountRule(int deaths)
    {
        if (deaths == 0) return  0f;
        if (deaths == 1) return -15f;
        return -25f;
    }

    /// <summary>Regla 5 — PerfectStreakRule</summary>
    private float EvaluatePerfectStreakRule(int streak)
    {
        if (streak <= 0) return  0f;
        if (streak <= 2) return  4f;
        if (streak <= 4) return  8f;
        return 12f;
    }

    /// <summary>Regla 6 — ItemUsageRule</summary>
    private float EvaluateItemUsageRule(int itemsUsed)
    {
        if (itemsUsed == 0) return 3f;
        if (itemsUsed <= 2) return 0f;
        return -3f;
    }

    // ─────────────────────────────────────────────
    // TABLA DE DECISION v2.0
    // ─────────────────────────────────────────────

    /// <summary>
    /// Calcula el ajuste base segun la tabla de umbrales multiples v2.0.
    /// Zona de Flow Perfecto (-4 a +4): no cambiar.
    /// </summary>
    private int CalculateAdjustment(float score)
    {
        if      (score >= 35f)  return  3;  // DOMINANDO  (sin dano + muy rapido)
        else if (score >= 15f)  return  2;  // EXCELENTE  (rapido aunque haya recibido dano)
        else if (score >= 1f)   return  1;  // BIEN       (gano sin morir, cualquier ritmo)
        else if (score >= -8f)  return  0;  // EQUILIBRADO
        else if (score >= -18f) return -1;  // COSTO
        else if (score >= -30f) return -2;  // MUY DIFICIL
        else                    return -3;  // IMPOSIBLE
    }

    // ─────────────────────────────────────────────
    // MODIFICADORES DE CONTEXTO v2.0
    // ─────────────────────────────────────────────

    /// <summary>Aplica los 4 modificadores que modulan el ajuste base.</summary>
    private int ApplyContextModifiers(int baseAdj, MetricsTracker mt)
    {
        int adj = baseAdj;

        // 1. Perfect Streak >= 5 → reducir subidas en 1 (no romper momentum)
        if (mt.PerfectStreak >= 5 && adj > 0)
        {
            adj -= 1;
            Debug.Log("[DDA] Mod: PerfectStreak>=5 → ajuste -1");
        }

        // 2. Muerte reciente → limitar subidas a +1
        if (mt.DeathCount > 0 && adj > 1)
        {
            adj = 1;
            Debug.Log("[DDA] Mod: Muerte reciente → subida limitada a +1");
        }

        // 3. Nivel alto (>=8) → limitar subidas a +1
        if (currentLevel >= 8 && adj > 1)
        {
            adj = 1;
            Debug.Log("[DDA] Mod: Nivel>=8 → subida limitada a +1");
        }

        // 4. Cooldown (ajuste grande hace <2 salas) → reducir a +-1
        if (Mathf.Abs(_lastAdjustment) >= 2 && _roomsSinceLastAdjustment < 2 && Mathf.Abs(adj) > 1)
        {
            adj = (adj > 0) ? 1 : -1;
            Debug.Log("[DDA] Mod: Cooldown activo → ajuste reducido a +-1");
        }

        _roomsSinceLastAdjustment++;
        return adj;
    }

    // ─────────────────────────────────────────────
    // APLICAR AJUSTE
    // ─────────────────────────────────────────────

    private void ApplyAdjustment(int adjustment)
    {
        int prev     = currentLevel;
        // v3.0: clampear a la BANDA elegida, no al rango global
        currentLevel = Mathf.Clamp(currentLevel + adjustment, minDifficulty, maxDifficulty);

        if (currentLevel != prev)
        {
            Debug.Log($"[DifficultyManager] Nivel: {prev} → {currentLevel} ({adjustment:+0;-0}) | Banda: [{minDifficulty}, {maxDifficulty}]");

            if (GameManager.Instance != null)
                GameManager.Instance.CurrentDifficultyLevel = currentLevel;

            if (currentLevel == MAX_LEVEL && ScoreManager.Instance != null)
                ScoreManager.Instance.RegisterReachedLevel10();

            if (EnemiesControllers.Instance != null)
                EnemiesControllers.Instance.ApplyScalingToActiveEnemies();
        }
        else
        {
            Debug.Log($"[DifficultyManager] Nivel mantenido en {currentLevel} (banda [{minDifficulty}, {maxDifficulty}]).");
        }

        // ─── Mecanica compensatoria (v3.0) ───────────────────────────────────
        // Si ya estamos en el minimo de banda Y el score sigue siendo muy negativo:
        // acumular racha de dificultad → tras 2 salas activar loot premium.
        if (currentLevel <= minDifficulty && _lastDDAScore < -8f)
        {
            _struggleStreak++;
            ShouldBoostLoot = _struggleStreak >= 2;

            if (ShouldBoostLoot)
                Debug.Log($"[DDA] BoostLoot ACTIVADO (racha={_struggleStreak} salas, score={_lastDDAScore:F1})");
            else
                Debug.Log($"[DDA] Racha de dificultad: {_struggleStreak}/2 salas en minimo de banda.");
        }
        else
        {
            if (_struggleStreak > 0 || ShouldBoostLoot)
                Debug.Log("[DDA] Racha de dificultad reiniciada. BoostLoot desactivado.");

            _struggleStreak = 0;
            ShouldBoostLoot = false;
        }
    }

    // ─────────────────────────────────────────────
    // AJUSTE MANUAL (usado por nodos BT-DDA)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ajusta el nivel directamente en una cantidad (positiva o negativa).
    /// Llamado por TaskIncreaseDifficulty y TaskDecreaseDifficulty.
    /// </summary>
    public void AdjustLevel(int amount)
    {
        _lastAdjustment           = amount;
        _roomsSinceLastAdjustment = 0;
        ApplyAdjustment(amount);
    }

    // ─────────────────────────────────────────────
    // GETTERS DE SCALING
    // ─────────────────────────────────────────────

    private int LevelIndex => Mathf.Clamp(currentLevel - 1, 0, 9);

    public float GetHPScaling()      => _hpScaling    [LevelIndex];
    public float GetDamageScaling()  => _damageScaling [LevelIndex];
    public float GetSpeedScaling()   => _speedScaling  [LevelIndex];
    public float GetItemDropChance() => _itemDropChance[LevelIndex];
    public int   GetMinEnemies()     => _minEnemies    [LevelIndex];
    public int   GetMaxEnemies()     => _maxEnemies    [LevelIndex];
}
