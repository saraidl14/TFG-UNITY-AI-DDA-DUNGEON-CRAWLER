using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Slime. Enemigo basico del juego.
///
/// Arbol BT:
///   Selector (raiz)
///   ├─ Sequence (atacar)
///   │   ├─ CheckPlayerDetected
///   │   ├─ CheckPlayerInAttackRange
///   │   └─ TaskAttackPlayer
///   └─ Sequence (perseguir)
///       ├─ CheckPlayerDetected
///       └─ TaskChasePlayer
///
/// Animaciones (parámetros Animator):
///   Attack (trigger) → animación de ataque; vuelve sola a SlimeWalk al acabar
///
/// Preparado para DDA: ApplyDifficultyScaling() escala stats en tiempo real.
/// </summary>
public class SlimeController : EnemyBase
{
    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    private Animator _animator;
    private static readonly int HashAttack = Animator.StringToHash("Attack");

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
        maxHealth      = 30f;
        damage         = 5f;
        moveSpeed      = 2f;
        attackRange    = 1.2f;
        attackCooldown = 1.5f;
        detectionRange = 8f;
        currentHealth  = maxHealth;

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
    // BT
    // ─────────────────────────────────────────────

    private void SetupBT()
    {
        var attackSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskAttackPlayer(damage, attackCooldown, OnAttackAnimation)
        });

        var chaseSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        _btRoot = new Selector(new List<Node>
        {
            attackSequence,
            chaseSequence
        });

        if (player != null)
            _btRoot.SetData("player", player);
    }

    // ─────────────────────────────────────────────
    // COMPORTAMIENTO
    // ─────────────────────────────────────────────

    protected override void UpdateBehavior()
    {
        if (player != null)
            _btRoot?.SetData("player", player);

        _btRoot?.Evaluate();
    }

    protected override void Attack() { }

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    /// <summary>
    /// Callback invocado por TaskAttackPlayer al impactar.
    /// Dispara el trigger Attack; el Animator vuelve solo a SlimeWalk al terminar.
    /// </summary>
    private void OnAttackAnimation()
    {
        if (_animator == null) return;
        _animator.SetTrigger(HashAttack);
    }

    // ─────────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        Debug.Log("[SlimeController] Slime eliminado.");
        // StartCoroutine ANTES de enabled=false (no arranca en componentes desactivados)
        StartCoroutine(DestroyAfterDelay(0.5f));
        enabled = false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        base.Die();
    }

    // ─────────────────────────────────────────────
    // DDA: SCALING EN TIEMPO REAL
    // ─────────────────────────────────────────────

    public void ApplyDifficultyScaling(float hpMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHealth     = 30f * hpMultiplier;
        currentHealth = maxHealth;
        damage        = 5f  * damageMultiplier;
        moveSpeed     = 2f  * speedMultiplier;
        SetupBT();

        Debug.Log($"[SlimeController] DDA aplicado | HP:{maxHealth} | DMG:{damage} | SPD:{moveSpeed}");
    }
}
