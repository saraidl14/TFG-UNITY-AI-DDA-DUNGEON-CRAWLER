using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Esqueleto. Enemigo melee de dificultad media.
/// Mas resistente y dañino que el Slime.
///
/// BT:
///   Selector
///   ├─ Sequence (atacar si esta en rango)
///   │   ├─ CheckPlayerDetected
///   │   ├─ CheckPlayerInAttackRange
///   │   └─ TaskAttackPlayer
///   └─ Sequence (perseguir si detecta)
///       ├─ CheckPlayerDetected
///       └─ TaskChasePlayer
///
/// Animaciones (parámetros Animator):
///   Speed  (float)   → 0 = Idle, 1 = Walk
///   Attack (trigger) → animación de ataque; vuelve sola a Idle al acabar
/// </summary>
public class SkeletonController : EnemyBase
{
    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    private Animator _animator;
    private static readonly int HashSpeed  = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    private UnityEngine.AI.NavMeshAgent _agent;

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
        maxHealth       = 50f;
        damage          = 8f;
        moveSpeed       = 2.5f;
        attackRange     = 3f;
        attackCooldown  = 1.2f;
        detectionRange  = 9f;
        maxRoamDistance = 10f;
        coinReward      = 30;
        currentHealth   = maxHealth;

        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        SetupBT();

        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        // Sin tocar updateRotation ni angularSpeed: el NavMeshAgent gestiona la rotación igual que el Orco
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

        var patrolNode = new TaskPatrol(transform, moveSpeed, maxRoamDistance, spawnPosition, waitTime: 2f);
        _btRoot = new Selector(new List<Node> { attackSequence, chaseSequence, patrolNode });

        if (player != null)
            _btRoot.SetData("player", player);
    }

    // ─────────────────────────────────────────────
    // COMPORTAMIENTO
    // ─────────────────────────────────────────────

    protected override void UpdateBehavior()
    {
        if (player == null) return;
        _btRoot?.SetData("player", player);
        _btRoot?.Evaluate();
        UpdateAnimationSpeed();
    }

    private void OnDrawGizmos()
    {
        // Rango de detección — amarillo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rango de ataque — rojo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Límite de sala (maxRoamDistance) — verde desde spawn
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position,
                              maxRoamDistance);
    }

protected override void Attack() { }

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    private void OnAttackAnimation()
    {
        if (_animator == null) return;
        _animator.SetTrigger(HashAttack);
    }

    private void UpdateAnimationSpeed()
    {
        if (_animator == null || player == null) return;

        float dist   = Vector3.Distance(transform.position, player.position);
        bool chasing = dist > attackRange && dist <= detectionRange;
        _animator.SetFloat(HashSpeed, chasing ? 1f : 0f);
    }

    // ─────────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        Debug.Log("[SkeletonController] Esqueleto eliminado.");
        // StartCoroutine ANTES de enabled=false (no arranca en componentes desactivados)
        StartCoroutine(DestroyAfterDelay(0.3f));
        enabled = false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        base.Die();
    }

    // ─────────────────────────────────────────────
    // DDA
    // ─────────────────────────────────────────────

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 50f * hpMult;
        currentHealth = maxHealth;
        damage        = 8f  * dmgMult;
        moveSpeed     = 2.5f * spdMult;
        SetupBT();
    }
}
