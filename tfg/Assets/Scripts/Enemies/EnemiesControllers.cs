using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BehaviorTree;

/// <summary>
/// Manager de enemigos por sala. Singleton.
///
/// Responsabilidades:
/// - Registrar todos los enemigos activos en la escena.
/// - Aplicar el scaling de DifficultyManager a los enemigos activos.
/// - Ejecutar el arbol BT-DDA al terminar cada sala.
/// - Notificar a DifficultyManager cuando la sala esta limpia.
/// </summary>
public class EnemiesControllers : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static EnemiesControllers Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>Al cargar una nueva escena (nueva mazmorra) se limpia el estado de la sala anterior.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _activeEnemies.Clear();
        _roomNormalEnemyCount = 0;
        Debug.Log("[EnemiesControllers] Lista de enemigos limpiada para nueva escena.");
    }

    // ─────────────────────────────────────────────
    // LISTA DE ENEMIGOS ACTIVOS
    // ─────────────────────────────────────────────

    private readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>();

    // Contador de enemigos normales de la sala (excluye boss)
    // Se usa para calcular el bonus de limpieza al derrotarlos a todos
    private int _roomNormalEnemyCount = 0;

    /// <summary>Registra un enemigo al spawnear. Llamado desde EnemyBase.Start().</summary>
    public void RegisterEnemy(EnemyBase enemy)
    {
        if (!_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Add(enemy);

            // Solo contar enemigos normales para el bonus de limpieza (no el boss)
            if (!(enemy is BossController))
                _roomNormalEnemyCount++;
        }
    }

    /// <summary>Elimina un enemigo de la lista al morir. Llamado desde EnemyBase.Die().</summary>
    public void UnregisterEnemy(EnemyBase enemy)
    {
        _activeEnemies.Remove(enemy);

        // Si no quedan enemigos, la sala esta limpia
        if (_activeEnemies.Count == 0)
            OnRoomCleared();
    }

    // ─────────────────────────────────────────────
    // SCALING DDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Aplica el scaling del nivel actual a todos los enemigos vivos.
    /// Llamado por DifficultyManager.ApplyAdjustment cuando cambia el nivel.
    /// </summary>
    public void ApplyScalingToActiveEnemies()
    {
        if (DifficultyManager.Instance == null) return;

        float hp    = DifficultyManager.Instance.GetHPScaling();
        float dmg   = DifficultyManager.Instance.GetDamageScaling();
        float spd   = DifficultyManager.Instance.GetSpeedScaling();

        foreach (EnemyBase enemy in _activeEnemies)
        {
            if (enemy == null) continue;

            // Escalar usando el metodo del enemigo si lo implementa
            SlimeController slime = enemy as SlimeController;
            if (slime != null) { slime.ApplyDifficultyScaling(hp, dmg, spd); continue; }

            BossController boss = enemy as BossController;
            if (boss != null) { boss.ApplyDifficultyScaling(hp, dmg, spd); continue; }

            // Fallback generico: escalar directamente los campos de EnemyBase
            enemy.maxHealth  = enemy.maxHealth  * hp;
            enemy.damage     = enemy.damage     * dmg;
            enemy.moveSpeed  = enemy.moveSpeed  * spd;
        }

        Debug.Log($"[EnemiesControllers] Scaling aplicado a {_activeEnemies.Count} enemigos " +
                  $"| HP x{hp} | DMG x{dmg} | SPD x{spd}");
    }

    // ─────────────────────────────────────────────
    // SALA COMPLETADA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado automaticamente cuando el ultimo enemigo muere.
    /// Cierra metricas de la sala, ejecuta el arbol BT-DDA y notifica al GameManager.
    /// </summary>
    private void OnRoomCleared()
    {
        Debug.Log("[EnemiesControllers] Sala limpia.");

        // ── Bonus de limpieza por matar a todos los enemigos normales ──
        // Formula: 20 * n_enemigos + 300 de bonus por limpieza total
        if (_roomNormalEnemyCount > 0 && GameManager.Instance != null)
        {
            int clearBonus = 20 * _roomNormalEnemyCount + 300;
            GameManager.Instance.AddCoins(clearBonus);
            Debug.Log($"[EnemiesControllers] Bonus limpieza: +{clearBonus} monedas " +
                      $"(20x{_roomNormalEnemyCount} enemigos + 300)");
        }
        _roomNormalEnemyCount = 0;

        // Cerrar metricas (guarda HP restante y detiene el timer)
        if (MetricsTracker.Instance != null)
        {
            PlayerHealth ph = FindObjectOfType<PlayerHealth>();
            float hpNow = ph != null ? ph.GetCurrentHealth() : 0f;
            MetricsTracker.Instance.CloseRoom(hpNow);
        }

        // Evaluar DDA y ajustar dificultad
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.EvaluateDifficulty();

        // Ejecutar arbol BT-DDA con los umbrales v2.0
        EvaluateDDATree();
    }

    // ─────────────────────────────────────────────
    // ARBOL BT-DDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Construye y evalua el arbol BT-DDA segun la tabla de umbrales v2.0.
    ///
    /// Selector (raiz)
    /// ├─ Sequence → score >= +40  → IncreaseDifficulty(+3)
    /// ├─ Sequence → score >= +25  → IncreaseDifficulty(+2)
    /// ├─ Sequence → score >= +15  → IncreaseDifficulty(+1)
    /// ├─ Sequence → score <= -15  → DecreaseDifficulty(-1)
    /// ├─ Sequence → score <= -25  → DecreaseDifficulty(-2)
    /// ├─ Sequence → score <= -40  → DecreaseDifficulty(-3)
    /// └─ TaskMaintainDifficulty   → mantener
    /// </summary>
    private void EvaluateDDATree()
    {
        float score = MetricsTracker.Instance != null
            ? MetricsTracker.Instance.LastDDAScore
            : 0f;

        // Construir arbol
        Node ddaTree = new Selector(new System.Collections.Generic.List<Node>
        {
            MakeAdjustBranch( 40f, true,   3),
            MakeAdjustBranch( 25f, true,   2),
            MakeAdjustBranch( 15f, true,   1),
            MakeAdjustBranch(-15f, false, -1),
            MakeAdjustBranch(-25f, false, -2),
            MakeAdjustBranch(-40f, false, -3),
            new TaskMaintainDifficulty()
        });

        // Pasar el score al blackboard
        ddaTree.SetData("ddaScore", score);

        // Evaluar
        ddaTree.Evaluate();

        // Resetear metricas para la siguiente sala
        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.ResetRoomMetrics();

        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.StartRoomTimer();
    }

    /// <summary>Crea una rama Sequence: CheckScoreThreshold + Task(Increase/Decrease).</summary>
    private Node MakeAdjustBranch(float threshold, bool above, int amount)
    {
        Node task = amount > 0
            ? (Node)new TaskIncreaseDifficulty(amount)
            : (Node)new TaskDecreaseDifficulty(Mathf.Abs(amount));

        return new Sequence(new System.Collections.Generic.List<Node>
        {
            new CheckScoreThreshold(threshold, above),
            task
        });
    }
}
