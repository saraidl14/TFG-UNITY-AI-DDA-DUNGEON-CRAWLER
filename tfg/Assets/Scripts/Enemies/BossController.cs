using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Boss. Enemigo final de cada mazmorra.
/// Dos fases diferenciadas por HP:
///
///   FASE 1 (HP > 50%): ataque melee moderado, velocidad normal.
///   FASE 2 (HP <= 50%): velocidad aumentada, daño mayor, ataque más rápido.
///
/// BT:
///   Selector
///   ├─ Sequence (FASE 2 — enfurecido)
///   │   ├─ CheckBossEnraged (HP <= 50%)
///   │   ├─ CheckPlayerInAttackRange
///   │   └─ TaskBossAttack (enraged=true)
///   ├─ Sequence (FASE 1 — normal)
///   │   ├─ CheckPlayerInAttackRange
///   │   └─ TaskBossAttack (enraged=false)
///   └─ TaskChasePlayer (persigue siempre)
/// </summary>
public class BossController : EnemyBase
{
    /// <summary>Se dispara cuando el boss muere. GameManager lo escucha para activar la victoria.</summary>
    public static event System.Action OnBossDead;

    [Header("Fase 2 (se activa al 50% HP)")]
    public float enragedDamageMultiplier = 1.8f;
    public float enragedSpeedMultiplier  = 1.6f;
    public float enragedAttackCooldown   = 0.8f;

    private Node  _btRoot;
    private bool  _isEnraged = false;
    private float _baseDamage;
    private float _baseSpeed;

    protected override void Awake()
    {
        base.Awake();
        maxHealth      = 300f;
        damage         = 20f;
        moveSpeed      = 2.5f;
        attackRange    = 2.5f;
        attackCooldown = 1.5f;
        detectionRange = 20f;
        coinReward     = 100;
        currentHealth  = maxHealth;
        _baseDamage    = damage;
        _baseSpeed     = moveSpeed;
    }

    protected override void Start()
    {
        base.Start();
        SetupBT();
    }

    private void SetupBT()
    {
        var enragedAttack = new Sequence(new List<Node>
        {
            new CheckBossEnraged(0.5f),
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskBossAttack(damage * enragedDamageMultiplier, enragedAttackCooldown, true)
        });

        var normalAttack = new Sequence(new List<Node>
        {
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskBossAttack(damage, attackCooldown, false)
        });

        var chase = new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition);

        _btRoot = new Selector(new List<Node> { enragedAttack, normalAttack, chase });

        if (player != null)
        {
            _btRoot.SetData("player",      player);
            _btRoot.SetData("bossHpRatio", 1f);
        }
    }

    protected override void UpdateBehavior()
    {
        if (player == null) return;

        float hpRatio = currentHealth / maxHealth;
        _btRoot?.SetData("player",      player);
        _btRoot?.SetData("bossHpRatio", hpRatio);

        // Fase 2 solo si el nivel DDA es 6 o superior
        int level = DifficultyManager.Instance != null ? DifficultyManager.Instance.currentLevel : 1;
        if (!_isEnraged && level >= 6 && hpRatio <= 0.5f)
        {
            _isEnraged = true;
            moveSpeed  = _baseSpeed * enragedSpeedMultiplier;
            Debug.Log("[BossController] ¡FASE 2 ACTIVADA! El boss se enfurece.");
        }

        _btRoot?.Evaluate();
    }

    protected override void Attack() { }

    protected override void Die()
    {
        Debug.Log("[BossController] Boss derrotado. ¡Mazmorra completada!");
        OnBossDead?.Invoke();
        base.Die();
    }

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 300f * hpMult;
        currentHealth = maxHealth;
        damage        = 20f  * dmgMult;
        moveSpeed     = 2.5f * spdMult;
        _baseDamage   = damage;
        _baseSpeed    = moveSpeed;
        SetupBT();
    }
}
