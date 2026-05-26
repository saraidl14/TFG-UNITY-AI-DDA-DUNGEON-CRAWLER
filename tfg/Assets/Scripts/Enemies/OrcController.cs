/*  Nombre:      OrcController.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       25/03/2026
 *  Descripcion: Controlador del Orco: tanque de alto HP y daño que aparece en niveles 6+.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Orco. Tanque: mucho HP y daño, pero muy lento.
/// Aparece en dificultades 6+.
///
/// Animaciones:
///   Blend Tree (Idle/Walk) → Speed float [0-1]
///   Punch  → trigger (golpe mano izquierda)
///   Swipe  → trigger (golpe mano derecha)
///   Die    → trigger (muerte)
///
/// Los ataques alternan Punch y Swipe en cada golpe.
/// </summary>
public class OrcController : EnemyBase
{
    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────
    private Animator _animator;

    // Hashes precalculados (evita strings en Update)
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashPunch = Animator.StringToHash("Punch");
    private static readonly int HashSwipe = Animator.StringToHash("Swipe");
    private static readonly int HashDie   = Animator.StringToHash("Die");

    /// <summary>Alterna entre Punch (izq) y Swipe (der) en cada ataque.</summary>
    private bool _nextAttackIsPunch = true;

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
        maxHealth      = 120f;
        damage         = 18f;
        moveSpeed      = 1.5f;    // Muy lento
        attackRange    = 2.0f;    // Brazo largo
        attackCooldown = 2.0f;    // Ataca lento pero muy fuerte
        detectionRange = 7f;
        coinReward     = 30;
        currentHealth  = maxHealth;

        // Busca el Animator en este objeto o en sus hijos (por si está en el mesh)
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
            // Pasamos OnAttackAnimation como callback — se llama al impactar
            new TaskAttackPlayer(damage, attackCooldown, OnAttackAnimation)
        });

        var chaseSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        var patrolNode = new TaskPatrol(transform, moveSpeed, maxRoamDistance, spawnPosition, waitTime: 3f);
        _btRoot = new Selector(new List<Node> { attackSequence, chaseSequence, patrolNode });

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

        UpdateAnimationSpeed();
    }

    // Attack() no se usa: lo gestiona TaskAttackPlayer vía callback OnAttackAnimation
    protected override void Attack() { }

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    /// <summary>
    /// Actualiza el parámetro Speed del Blend Tree según la distancia al jugador.
    ///   0 → Idle (fuera de rango o en rango de ataque, parado)
    ///   1 → Walk (persiguiendo)
    /// </summary>
    private void UpdateAnimationSpeed()
    {
        if (_animator == null || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool chasing = dist > attackRange && dist <= detectionRange;

        _animator.SetFloat(HashSpeed, chasing ? 1f : 0f);
    }

    /// <summary>
    /// Callback invocado por TaskAttackPlayer justo cuando el golpe impacta.
    /// Alterna entre Punch (izquierda) y Swipe (derecha) en cada ataque.
    /// </summary>
    private void OnAttackAnimation()
    {
        if (_animator == null) return;

        if (_nextAttackIsPunch)
            _animator.SetTrigger(HashPunch);
        else
            _animator.SetTrigger(HashSwipe);

        _nextAttackIsPunch = !_nextAttackIsPunch;
    }

    // ─────────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        Debug.Log("[OrcController] Orco eliminado.");

        if (_animator != null)
            _animator.SetTrigger(HashDie);

        // StartCoroutine ANTES de enabled=false (no arranca en componentes desactivados)
        StartCoroutine(DestroyAfterDelay(2f));
        enabled = false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Llamamos a base.Die() aquí para dar monedas y desregistrarse
        base.Die();
    }

    // ─────────────────────────────────────────────
    // DDA: SCALING EN TIEMPO REAL
    // ─────────────────────────────────────────────

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 120f * hpMult;
        currentHealth = maxHealth;
        damage        = 18f  * dmgMult;
        moveSpeed     = 1.5f * spdMult;
        SetupBT();

        Debug.Log($"[OrcController] DDA aplicado | HP:{maxHealth} | DMG:{damage} | SPD:{moveSpeed}");
    }
}
