using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Boss. Enemigo final de cada mazmorra.
///
/// FASES:
///   FASE 1 (HP > 50% o nivel < 6) → ataque normal, alterna Punch / Swipe
///   FASE 2 (HP ≤ 50% Y nivel ≥ 6) → velocidad y daño aumentados,
///                                     mezcla Punch / Swipe / JumpAttack
///
/// BT:
///   Selector
///   ├─ Sequence (FASE 2 — enfurecido)
///   │   ├─ CheckBossEnraged
///   │   ├─ CheckPlayerInAttackRange
///   │   └─ TaskBossAttack (enraged=true, callback OnEnragedAttack)
///   ├─ Sequence (FASE 1 — normal)
///   │   ├─ CheckPlayerInAttackRange
///   │   └─ TaskBossAttack (enraged=false, callback OnNormalAttack)
///   └─ TaskBossChase
///
/// ANIMACIONES (parámetros Animator):
///   Speed      (float)   → 0 = idle/parado, 1 = caminando
///   Punch      (trigger) → golpe mano izquierda
///   Swipe      (trigger) → golpe mano derecha
///   JumpAttack (trigger) → ataque de área (solo fase 2)
///   Die        (trigger) → muerte
/// </summary>
public class BossController : EnemyBase
{
    // ─────────────────────────────────────────────
    // EVENTOS
    // ─────────────────────────────────────────────

    /// <summary>Se dispara cuando el boss muere. GameManager lo escucha para la victoria.</summary>
    public static event System.Action OnBossDead;

    // ─────────────────────────────────────────────
    // STATS DE FASE 2
    // ─────────────────────────────────────────────

    [Header("Fase 2 — se activa al 50 % HP (niveles 6+)")]
    public float enragedDamageMultiplier = 1.8f;
    public float enragedSpeedMultiplier  = 1.6f;
    public float enragedAttackCooldown   = 0.8f;

    /// <summary>Probabilidad [0-1] de usar JumpAttack en cada golpe de la fase 2.</summary>
    [Range(0f, 1f)]
    public float jumpAttackChance = 0.35f;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    private Node  _btRoot;
    private bool  _isEnraged = false;
    private float _baseDamage;
    private float _baseSpeed;

    /// <summary>Alterna entre Punch (true) y Swipe (false) en cada golpe.</summary>
    private bool _nextAttackIsPunch = true;

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    private Animator _animator;

    private static readonly int HashSpeed      = Animator.StringToHash("Speed");
    private static readonly int HashPunch      = Animator.StringToHash("Punch");
    private static readonly int HashSwipe      = Animator.StringToHash("Swipe");
    private static readonly int HashJumpAttack = Animator.StringToHash("JumpAttack");
    private static readonly int HashDie        = Animator.StringToHash("Die");

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        maxHealth       = 300f;
        damage          = 20f;
        moveSpeed       = 2.5f;
        attackRange     = 2.5f;
        attackCooldown  = 1.5f;
        detectionRange  = 25f;
        maxRoamDistance = 8f;
        coinReward      = 100;
        currentHealth   = maxHealth;
        _baseDamage     = damage;
        _baseSpeed      = moveSpeed;

        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        SetupBT();
    }

    // ─────────────────────────────────────────────
    // SCALING (boss tiene su propia curva)
    // ─────────────────────────────────────────────

    protected override void ApplyCurrentDifficultyScaling()
    {
        if (DifficultyManager.Instance == null) return;
        int level = DifficultyManager.Instance.currentLevel;

        maxHealth     = 40f + level * 10f;   // nv1=50, nv5=90, nv10=140
        currentHealth = maxHealth;
        damage        = 8f  + level * 2f;    // nv1=10, nv10=28
        moveSpeed     = 2f  + level * 0.05f;
        _baseDamage   = damage;
        _baseSpeed    = moveSpeed;

        Debug.Log($"[BossController] Stats nivel {level} | HP:{maxHealth} | DMG:{damage}");
    }

    // ─────────────────────────────────────────────
    // BT
    // ─────────────────────────────────────────────

    private void SetupBT()
    {
        // Fase 2 — enfurecido (solo nivel 6+, HP ≤ 50%)
        var enragedAttack = new Sequence(new List<Node>
        {
            new CheckBossEnraged(0.5f),
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskBossAttack(damage * enragedDamageMultiplier,
                               enragedAttackCooldown,
                               isEnraged: true,
                               onAttack:  OnEnragedAttack)
        });

        // Fase 1 — ataque normal
        var normalAttack = new Sequence(new List<Node>
        {
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskBossAttack(damage,
                               attackCooldown,
                               isEnraged: false,
                               onAttack:  OnNormalAttack)
        });

        var chase = new TaskBossChase(transform, this, maxRoamDistance, spawnPosition);

        _btRoot = new Selector(new List<Node> { enragedAttack, normalAttack, chase });

        if (player != null)
        {
            _btRoot.SetData("player",      player);
            _btRoot.SetData("bossHpRatio", 1f);
        }
    }

    // ─────────────────────────────────────────────
    // COMPORTAMIENTO
    // ─────────────────────────────────────────────

    protected override void UpdateBehavior()
    {
        if (player == null) return;

        float hpRatio = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        _btRoot?.SetData("player",      player);
        _btRoot?.SetData("bossHpRatio", hpRatio);

        // Activar Fase 2 solo si nivel DDA ≥ 6
        int level = DifficultyManager.Instance != null ? DifficultyManager.Instance.currentLevel : 1;
        if (!_isEnraged && level >= 6 && hpRatio <= 0.5f)
        {
            _isEnraged = true;
            moveSpeed  = _baseSpeed * enragedSpeedMultiplier;
            Debug.Log("[BossController] ¡FASE 2 ACTIVADA!");
        }

        _btRoot?.Evaluate();

        UpdateAnimationSpeed();
    }

    protected override void Attack() { }   // Gestionado por TaskBossAttack

    // ─────────────────────────────────────────────
    // ANIMACIÓN — callbacks desde TaskBossAttack
    // ─────────────────────────────────────────────

    /// <summary>
    /// Fase 1 — alterna Punch y Swipe en cada golpe.
    /// </summary>
    private void OnNormalAttack()
    {
        if (_animator == null) return;

        _animator.SetTrigger(_nextAttackIsPunch ? HashPunch : HashSwipe);
        _nextAttackIsPunch = !_nextAttackIsPunch;
    }

    /// <summary>
    /// Fase 2 — mezcla Punch / Swipe con JumpAttack (35 % por defecto).
    /// JumpAttack solo se activa si el nivel DDA es 6 o superior.
    /// </summary>
    private void OnEnragedAttack()
    {
        if (_animator == null) return;

        int level = DifficultyManager.Instance != null ? DifficultyManager.Instance.currentLevel : 1;
        bool canJump = level >= 6 && Random.value < jumpAttackChance;

        if (canJump)
        {
            _animator.SetTrigger(HashJumpAttack);
        }
        else
        {
            _animator.SetTrigger(_nextAttackIsPunch ? HashPunch : HashSwipe);
            _nextAttackIsPunch = !_nextAttackIsPunch;
        }
    }

    /// <summary>
    /// Actualiza el parámetro Speed del Blend Tree.
    ///   0 = idle / parado para atacar
    ///   1 = persiguiendo
    /// </summary>
    private void UpdateAnimationSpeed()
    {
        if (_animator == null || player == null) return;

        float dist    = Vector3.Distance(transform.position, player.position);
        bool chasing  = dist > attackRange && dist <= detectionRange;

        _animator.SetFloat(HashSpeed, chasing ? 1f : 0f);
    }

    // ─────────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        Debug.Log("[BossController] Boss derrotado. ¡Mazmorra completada!");

        // Triggear animación de muerte
        if (_animator != null)
            _animator.SetTrigger(HashDie);

        // DDA se registra inmediatamente (no necesita esperar la animación)
        if (EnemiesControllers.Instance != null)
            EnemiesControllers.Instance.TriggerBossDDA();

        // Victoria y destrucción esperan a que termine la animación.
        // IMPORTANTE: StartCoroutine ANTES de enabled=false (no funciona al revés).
        StartCoroutine(DestroyAfterDelay(2.5f));

        // Parar el BT después de iniciar la coroutine
        enabled = false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Monedas y desregistro
        if (coinReward > 0 && GameManager.Instance != null)
            GameManager.Instance.AddCoins(coinReward);

        if (EnemiesControllers.Instance != null)
            EnemiesControllers.Instance.UnregisterEnemy(this);

        // Ahora sí: disparar la pantalla de victoria (tras la animación de muerte)
        OnBossDead?.Invoke();

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────
    // DDA: SCALING EN TIEMPO REAL
    // ─────────────────────────────────────────────

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
