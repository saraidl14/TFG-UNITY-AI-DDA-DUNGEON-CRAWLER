using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Boss. Usa Behavior Tree con dos fases:
///
///   FASE 1 (HP > 50%): ataque normal + persecucion normal.
///   FASE 2 (HP <= 50%): ENFURECIDO — mas dano, mas velocidad, ataca mas rapido.
///
/// Arbol BT:
///   Selector (raiz)
///   ├─ Sequence (FASE 2 - enfurecido)
///   │   ├─ CheckBossEnraged         (HP <= 50%)
///   │   └─ Selector
///   │       ├─ Sequence (atacar)
///   │       │   ├─ CheckPlayerInAttackRange
///   │       │   └─ TaskBossAttack   (dano x1.5, cooldown x0.7)
///   │       └─ TaskChasePlayer      (velocidad x1.3)
///   └─ Selector (FASE 1 - normal)
///       ├─ Sequence (atacar)
///       │   ├─ CheckPlayerDetected
///       │   ├─ CheckPlayerInAttackRange
///       │   └─ TaskBossAttack       (dano base)
///       └─ Sequence (perseguir)
///           ├─ CheckPlayerDetected
///           └─ TaskChasePlayer      (velocidad base)
///
/// Tabla de stats por nivel DDA (1 = muy facil, 10 = casi imposible):
///   Nivel |   HP  | DMG | FURY | SPD | Cooldown
///     1   |    40 |   5 |    7 | 3.0 |   3.0s  ← Muere en 4 golpes basicos
///     2   |    65 |   8 |   11 | 3.2 |   2.7s
///     3   |   100 |  12 |   17 | 3.4 |   2.4s
///     4   |   155 |  17 |   24 | 3.6 |   2.1s
///     5   |   230 |  23 |   32 | 3.8 |   1.9s
///     6   |   330 |  31 |   43 | 4.1 |   1.7s
///     7   |   470 |  40 |   56 | 4.4 |   1.5s
///     8   |   650 |  50 |   70 | 4.7 |   1.2s
///     9   |   840 |  58 |   81 | 5.1 |   1.0s
///    10   |  1050 |  65 |   91 | 5.5 |   0.8s  ← CASI IMPOSIBLE (fury = one-shot)
///
/// En FASE 2 (enfurecido): DMG x1.4 | SPD x1.3 | Cooldown x0.7
/// </summary>
public class BossController : EnemyBase
{
    // ─────────────────────────────────────────────
    // EVENTO
    // ─────────────────────────────────────────────

    /// <summary>Disparado cuando el boss muere. GameManager lo escucha.</summary>
    public static event System.Action OnBossDead;

    // ─────────────────────────────────────────────
    // TABLA DE STATS POR NIVEL (indices 0-9 = niveles 1-10)
    // ─────────────────────────────────────────────

    private static readonly float[] BossHP   = {   40f,   65f,  100f,  155f,  230f,  330f,  470f,  650f,  840f, 1050f };
    private static readonly float[] BossDMG  = {    5f,    8f,   12f,   17f,   23f,   31f,   40f,   50f,   58f,   65f };
    private static readonly float[] BossSPD  = {  3.0f,  3.2f,  3.4f,  3.6f,  3.8f,  4.1f,  4.4f,  4.7f,  5.1f,  5.5f };
    private static readonly float[] BossCOOL = {  3.0f,  2.7f,  2.4f,  2.1f,  1.9f,  1.7f,  1.5f,  1.2f,  1.0f,  0.8f };

    // Multiplicadores de fase enfurecida
    // DMG x1.4 → nivel 1: 5 x 1.4 = 7 fury  |  nivel 10: 65 x 1.4 = 91 (one-shot)
    private const float EnragedDmgMult  = 1.4f;
    private const float EnragedSpdMult  = 1.3f;
    private const float EnragedCoolMult = 0.7f;  // Cooldown mas corto = ataca mas rapido

    // ─────────────────────────────────────────────
    // STATS CALCULADOS EN FASE 2
    // ─────────────────────────────────────────────

    private float _enragedDamage;
    private float _enragedSpeed;
    private float _enragedCooldown;

    // ─────────────────────────────────────────────
    // BT
    // ─────────────────────────────────────────────

    private Node _btRoot;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // Valores por defecto (nivel 1). Se sobreescriben en Start() con la tabla.
        maxHealth      = BossHP[0];
        damage         = BossDMG[0];
        moveSpeed      = BossSPD[0];
        attackRange    = 2.5f;
        attackCooldown = BossCOOL[0];
        detectionRange = 20f;
        currentHealth  = maxHealth;
        coinReward     = 0; // Recompensa del boss via ScoreManager, no monedas directas
    }

    protected override void Start()
    {
        // Inicializar posicion, jugador y registro (igual que EnemyBase.Start)
        spawnPosition = transform.position;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (EnemiesControllers.Instance != null)
            EnemiesControllers.Instance.RegisterEnemy(this);

        // Aplicar stats de la tabla segun nivel DDA actual
        ApplyBossStats();

        // Construir arbol BT
        SetupBT();
    }

    // ─────────────────────────────────────────────
    // STATS POR NIVEL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Aplica los stats exactos de la tabla segun el nivel DDA actual.
    /// Ignora el sistema de multiplicadores de EnemyBase para usar valores precisos.
    /// </summary>
    private void ApplyBossStats()
    {
        int level = 1;
        if (DifficultyManager.Instance != null)
            level = Mathf.Clamp(DifficultyManager.Instance.currentLevel, 1, 10);

        int idx = level - 1;

        maxHealth      = BossHP[idx];
        currentHealth  = maxHealth;
        damage         = BossDMG[idx];
        moveSpeed      = BossSPD[idx];
        attackCooldown = BossCOOL[idx];

        // Calcular stats de fase enfurecida
        _enragedDamage   = damage        * EnragedDmgMult;
        _enragedSpeed    = moveSpeed     * EnragedSpdMult;
        _enragedCooldown = attackCooldown * EnragedCoolMult;

        Debug.Log($"[BossController] Nivel {level} | " +
                  $"HP:{maxHealth} DMG:{damage} SPD:{moveSpeed} COOL:{attackCooldown:F1}s | " +
                  $"FURY → DMG:{_enragedDamage:F0} SPD:{_enragedSpeed:F1} COOL:{_enragedCooldown:F1}s");
    }

    // ─────────────────────────────────────────────
    // CONSTRUCCION DEL ARBOL BT
    // ─────────────────────────────────────────────

    private void SetupBT()
    {
        // ── FASE 2: Enfurecido (HP <= 50%) ───────────
        var enragedAttackSeq = new Sequence(new List<Node>
        {
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskBossAttack(_enragedDamage, _enragedCooldown, isEnraged: true)
        });

        var enragedChase = new TaskChasePlayer(
            transform, _enragedSpeed, maxRoamDistance, spawnPosition);

        var enragedSelector = new Selector(new List<Node>
        {
            enragedAttackSeq,
            enragedChase
        });

        var fase2 = new Sequence(new List<Node>
        {
            new CheckBossEnraged(hpThreshold: 0.5f),
            enragedSelector
        });

        // ── FASE 1: Normal ────────────────────────────
        var normalAttackSeq = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskBossAttack(damage, attackCooldown, isEnraged: false)
        });

        var normalChaseSeq = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        var fase1 = new Selector(new List<Node>
        {
            normalAttackSeq,
            normalChaseSeq
        });

        // ── RAIZ ──────────────────────────────────────
        _btRoot = new Selector(new List<Node> { fase2, fase1 });

        if (player != null)
            _btRoot.SetData("player", player);
    }

    // ─────────────────────────────────────────────
    // BEHAVIOR (llamado desde EnemyBase.Update)
    // ─────────────────────────────────────────────

    protected override void UpdateBehavior()
    {
        if (_btRoot == null) return;

        // Actualizar blackboard cada frame
        if (player != null)
            _btRoot.SetData("player", player);

        // Ratio de HP para CheckBossEnraged
        float hpRatio = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        _btRoot.SetData("bossHpRatio", hpRatio);

        _btRoot.Evaluate();
    }

    // ─────────────────────────────────────────────
    // ATAQUE (no se usa: lo gestiona TaskBossAttack en el BT)
    // ─────────────────────────────────────────────

    protected override void Attack() { }

    // ─────────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        Debug.Log($"[BossController] Boss derrotado en nivel {(DifficultyManager.Instance != null ? DifficultyManager.Instance.currentLevel : 1)}.");

        // Evaluar DDA directamente: no esperar a que _activeEnemies == 0.
        // El jugador puede matar solo al boss (speedrun) dejando enemigos normales vivos.
        if (EnemiesControllers.Instance != null)
            EnemiesControllers.Instance.TriggerBossDDA();

        base.Die();           // Desregistrar de _activeEnemies y destruir (flag evita doble DDA)
        OnBossDead?.Invoke(); // El panel ya lee LastAdjustment actualizado
    }

    // ─────────────────────────────────────────────
    // SCALING EN TIEMPO REAL (EnemiesControllers lo llama al cambiar nivel)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Recalcula stats desde la tabla y reconstruye el BT.
    /// Llamado por EnemiesControllers cuando el DDA cambia el nivel.
    /// </summary>
    public void ApplyDifficultyScaling(float hpMultiplier, float damageMultiplier, float speedMultiplier)
    {
        // Ignorar multiplicadores genericos: usar la tabla exacta del boss
        ApplyBossStats();
        SetupBT();
    }
}
