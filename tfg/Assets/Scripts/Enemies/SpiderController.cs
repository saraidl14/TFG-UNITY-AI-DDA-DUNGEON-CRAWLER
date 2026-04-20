using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador de la Araña. Combina melee con disparo de tela que ralentiza al jugador.
///
/// BT:
///   Selector
///   ├─ Sequence (atacar melee si esta muy cerca)
///   │   ├─ CheckPlayerDetected
///   │   ├─ CheckPlayerInAttackRange
///   │   └─ TaskAttackPlayer
///   ├─ Sequence (disparar tela si esta en rango medio)
///   │   ├─ CheckPlayerDetected
///   │   └─ TaskShootProjectile (web)
///   └─ Sequence (perseguir)
///       ├─ CheckPlayerDetected
///       └─ TaskChasePlayer
/// </summary>
public class SpiderController : EnemyBase
{
    [Header("Tela de araña")]
    [Tooltip("Prefab del proyectil tela. Debe tener EnemyProjectile.")]
    public GameObject webPrefab;

    [Tooltip("Velocidad de la tela disparada.")]
    public float webSpeed = 6f;

    [Tooltip("Ralentizacion aplicada al jugador al impactar (unidades/s).")]
    public float webSlowAmount = 2f;

    [Tooltip("Duracion de la ralentizacion en segundos.")]
    public float webSlowDuration = 3f;

    [Tooltip("Rango máximo de disparo de tela.")]
    public float webRange = 6f;

    [Tooltip("Cooldown entre disparos de tela.")]
    public float webCooldown = 3f;

    private Node _btRoot;

    protected override void Awake()
    {
        base.Awake();
        maxHealth      = 35f;
        damage         = 7f;
        moveSpeed      = 3f;
        attackRange    = 1.2f;
        attackCooldown = 1.0f;
        detectionRange = 9f;
        coinReward     = 12;
        currentHealth  = maxHealth;
    }

    protected override void Start()
    {
        base.Start();
        SetupBT();
    }

    private void SetupBT()
    {
        // Rama 1: melee si está muy cerca
        var meleeSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskAttackPlayer(damage, attackCooldown)
        });

        // Rama 2: disparar tela (proyectil con slow)
        var webSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskShootProjectile(transform, webPrefab, 0f, webSpeed, webCooldown, webRange)
            // daño 0 porque el slow es el efecto principal; el daño lo aplica EnemyProjectile si se desea
        });

        // Rama 3: perseguir
        var chaseSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        _btRoot = new Selector(new List<Node> { meleeSequence, webSequence, chaseSequence });

        if (player != null)
            _btRoot.SetData("player", player);
    }

    protected override void UpdateBehavior()
    {
        if (player != null) _btRoot?.SetData("player", player);
        _btRoot?.Evaluate();
    }

    protected override void Attack() { }

    protected override void Die()
    {
        Debug.Log("[SpiderController] Araña eliminada.");
        base.Die();
    }

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 35f * hpMult;
        currentHealth = maxHealth;
        damage        = 7f  * dmgMult;
        moveSpeed     = 3f  * spdMult;
        SetupBT();
    }
}
