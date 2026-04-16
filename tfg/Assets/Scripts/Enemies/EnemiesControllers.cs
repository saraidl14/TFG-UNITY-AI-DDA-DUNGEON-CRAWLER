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
        _ddaEvaluatedThisRoom = false;
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

        // Si no quedan enemigos Y el DDA no fue ya evaluado por muerte del boss → sala limpia
        if (_activeEnemies.Count == 0 && !_ddaEvaluatedThisRoom)
            OnRoomCleared();
    }

    // Flag para evitar doble evaluación DDA (cuando el boss muere y además quedan 0 enemigos)
    private bool _ddaEvaluatedThisRoom = false;

    /// <summary>
    /// Llamado directamente desde BossController.Die() para garantizar que el DDA
    /// se evalua al morir el boss, independientemente de si quedan enemigos normales vivos.
    /// </summary>
    public void TriggerBossDDA()
    {
        if (_ddaEvaluatedThisRoom) return;
        _ddaEvaluatedThisRoom = true;
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

            // ── Boss: usa su propia tabla de stats, ignora multiplicadores ──
            BossController boss = enemy as BossController;
            if (boss != null) { boss.ApplyDifficultyScaling(hp, dmg, spd); continue; }

            // ── Enemigos con controlador propio: delegar en su metodo ──
            // (cuando se implementen SkeletonController, GoblinController, etc.
            //  añadir un bloque identico al de SlimeController)

            SlimeController slime = enemy as SlimeController;
            if (slime != null) { slime.ApplyDifficultyScaling(hp, dmg, spd); continue; }

            // TODO: SkeletonController  → skeleton.ApplyDifficultyScaling(hp, dmg, spd)
            // TODO: GoblinController    → goblin.ApplyDifficultyScaling(hp, dmg, spd)
            // TODO: OrcController       → orc.ApplyDifficultyScaling(hp, dmg, spd)
            // TODO: SpiderController    → spider.ApplyDifficultyScaling(hp, dmg, spd)
            // TODO: MageController      → mage.ApplyDifficultyScaling(hp, dmg, spd)

            // ── Fallback generico para cualquier EnemyBase sin controlador propio ──
            enemy.maxHealth = enemy.maxHealth * hp;
            enemy.damage    = enemy.damage    * dmg;
            enemy.moveSpeed = enemy.moveSpeed * spd;
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
            // Si no se encuentra al jugador usamos 50 como valor neutro (no penaliza ni bonifica)
            // para evitar que un null reference hunda el score DDA injustamente
            float hpNow = ph != null ? ph.GetCurrentHealth() : 50f;
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
    /// ├─ Sequence → score >= +35  → IncreaseDifficulty(+3)  [Dominando]
    /// ├─ Sequence → score >= +15  → IncreaseDifficulty(+2)  [Excelente]
    /// ├─ Sequence → score >=  +1  → IncreaseDifficulty(+1)  [Bien]
    /// ├─ Sequence → score <=  -8  → DecreaseDifficulty(-1)  [Costó]
    /// ├─ Sequence → score <= -18  → DecreaseDifficulty(-2)  [Muy difícil]
    /// ├─ Sequence → score <= -30  → DecreaseDifficulty(-3)  [Imposible]
    /// └─ TaskMaintainDifficulty   → mantener                [Flow equilibrado]
    /// </summary>
    private void EvaluateDDATree()
    {
        float score = MetricsTracker.Instance != null
            ? MetricsTracker.Instance.LastDDAScore
            : 0f;

        // Construir arbol — umbrales alineados con CalculateAdjustment():
        //   Score maximo tipico: ~46 (sin daño + <20s + HP llena + streak>4)
        //   Score normal (sin muertes, velocidad ok, sin items): ~6-13
        //   +1 desde score>=1 para que cualquier partida positiva suba la dificultad
        Node ddaTree = new Selector(new System.Collections.Generic.List<Node>
        {
            MakeAdjustBranch( 35f, true,   3),  // Dominando (sin daño + muy rapido + HP llena)
            MakeAdjustBranch( 15f, true,   2),  // Excelente (rapido aunque con daño)
            MakeAdjustBranch(  1f, true,   1),  // Bien (gano sin morir, cualquier ritmo)
            MakeAdjustBranch( -8f, false, -1),  // Costo (HP critica o muy lento)
            MakeAdjustBranch(-18f, false, -2),  // Muy dificil (murio una vez)
            MakeAdjustBranch(-30f, false, -3),  // Imposible (murio varias veces)
            new TaskMaintainDifficulty()         // Zona de flow equilibrado [-8, +1)
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
