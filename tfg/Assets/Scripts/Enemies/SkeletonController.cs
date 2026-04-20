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
/// </summary>
public class SkeletonController : EnemyBase
{
    private Node _btRoot;

    protected override void Awake()
    {
        base.Awake();
        maxHealth      = 50f;
        damage         = 8f;
        moveSpeed      = 2.5f;
        attackRange    = 1.5f;
        attackCooldown = 1.2f;
        detectionRange = 9f;
        coinReward     = 15;
        currentHealth  = maxHealth;
    }

    protected override void Start()
    {
        base.Start();
        SetupBT();
    }

    private void SetupBT()
    {
        var attackSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskAttackPlayer(damage, attackCooldown)
        });

        var chaseSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        _btRoot = new Selector(new List<Node> { attackSequence, chaseSequence });

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
        Debug.Log("[SkeletonController] Esqueleto eliminado.");
        base.Die();
    }

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 50f * hpMult;
        currentHealth = maxHealth;
        damage        = 8f  * dmgMult;
        moveSpeed     = 2.5f * spdMult;
        SetupBT();
    }
}
