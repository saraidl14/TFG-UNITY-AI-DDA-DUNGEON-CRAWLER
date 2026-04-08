using UnityEngine;

/// <summary>
/// Gestor central del sistema DDA (Dynamic Difficulty Adjustment).
/// Implementa las 6 reglas DDA v2.0 con umbrales multiples y modificadores de contexto.
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
    public int currentLevel = 1;

    public const int MIN_LEVEL = 1;
    public const int MAX_LEVEL = 10;

    private int _roomsSinceLastAdjustment = 0;
    private int _lastAdjustment           = 0;

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

        Debug.Log($"[DifficultyManager] Score DDA: {totalScore:F2}");

        // Guardar en MetricsTracker para los nodos BT del arbol DDA
        mt.SetDDAScore(totalScore);

        int adjustment = CalculateAdjustment(totalScore);
        adjustment     = ApplyContextModifiers(adjustment, mt);

        ApplyAdjustment(adjustment);

        _roomsSinceLastAdjustment = 0;
        _lastAdjustment           = adjustment;
    }

    // ─────────────────────────────────────────────
    // 6 REGLAS DDA
    // ─────────────────────────────────────────────

    /// <summary>Regla 1 — PlayerDamageRule (25%)</summary>
    private float EvaluatePlayerDamageRule(float damageTaken)
    {
        float maxHP = 100f;
        float pts;

        if      (damageTaken <= 0f)              pts = 10f;
        else if (damageTaken < maxHP * 0.2f)     pts =  5f;
        else if (damageTaken > maxHP * 0.5f)     pts = -8f;
        else                                     pts =  0f;

        return pts * W_DAMAGE;
    }

    /// <summary>Regla 2 — EnemyKillSpeedRule (20%)</summary>
    private float EvaluateKillSpeedRule(float timeTaken)
    {
        float pts;

        if      (timeTaken < 30f)   pts =  8f;
        else if (timeTaken <= 90f)  pts =  0f;
        else if (timeTaken > 120f)  pts = -5f;
        else                        pts =  0f;

        return pts * W_KILL_SPEED;
    }

    /// <summary>Regla 3 — HealthRemainingRule (20%)</summary>
    private float EvaluateHealthRemainingRule(float hpRemaining)
    {
        float hpPct = hpRemaining / 100f;
        float pts;

        if      (hpPct >= 1.0f)  pts = 12f;
        else if (hpPct > 0.70f)  pts =  6f;
        else if (hpPct >= 0.30f) pts =  0f;
        else                     pts = -10f;

        return pts * W_HEALTH;
    }

    /// <summary>Regla 4 — DeathCountRule (25%) — MAS IMPORTANTE</summary>
    private float EvaluateDeathCountRule(int deaths)
    {
        float pts;

        if      (deaths == 0) pts =   0f;
        else if (deaths == 1) pts = -12f;
        else                  pts = -20f;

        return pts * W_DEATHS;
    }

    /// <summary>Regla 5 — PerfectStreakRule (15%)</summary>
    private float EvaluatePerfectStreakRule(int streak)
    {
        float pts;

        if      (streak <= 0) pts =  0f;
        else if (streak <= 2) pts =  3f;
        else if (streak <= 4) pts =  8f;
        else                  pts = 15f;

        return pts * W_STREAK;
    }

    /// <summary>Regla 6 — ItemUsageRule (10%)</summary>
    private float EvaluateItemUsageRule(int itemsUsed)
    {
        float pts;

        if      (itemsUsed == 0) pts =  4f;
        else if (itemsUsed <= 2) pts =  0f;
        else                     pts = -6f;

        return pts * W_ITEMS;
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
        if      (score >= 40f)  return  3;  // DOMINANDO
        else if (score >= 25f)  return  2;  // EXCELENTE
        else if (score >= 15f)  return  1;  // MUY BIEN
        else if (score >= -14f) return  0;  // FLOW PERFECTO / EQUILIBRADO
        else if (score >= -24f) return -1;  // LUCHANDO
        else if (score >= -39f) return -2;  // MUY DIFICIL
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

        // 4. Cooldown (ajuste grande hace <2 salas) → reducir a ±1
        if (Mathf.Abs(_lastAdjustment) >= 2 && _roomsSinceLastAdjustment < 2 && Mathf.Abs(adj) > 1)
        {
            adj = (adj > 0) ? 1 : -1;
            Debug.Log("[DDA] Mod: Cooldown activo → ajuste reducido a ±1");
        }

        _roomsSinceLastAdjustment++;
        return adj;
    }

    // ─────────────────────────────────────────────
    // APLICAR AJUSTE
    // ─────────────────────────────────────────────

    private void ApplyAdjustment(int adjustment)
    {
        int prev    = currentLevel;
        currentLevel = Mathf.Clamp(currentLevel + adjustment, MIN_LEVEL, MAX_LEVEL);

        if (currentLevel != prev)
        {
            Debug.Log($"[DifficultyManager] Nivel: {prev} → {currentLevel} ({adjustment:+0;-0})");

            if (GameManager.Instance != null)
                GameManager.Instance.CurrentDifficultyLevel = currentLevel;

            if (currentLevel == MAX_LEVEL && ScoreManager.Instance != null)
                ScoreManager.Instance.RegisterReachedLevel10();

            if (EnemiesControllers.Instance != null)
                EnemiesControllers.Instance.ApplyScalingToActiveEnemies();
        }
        else
        {
            Debug.Log($"[DifficultyManager] Nivel mantenido en {currentLevel}.");
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
