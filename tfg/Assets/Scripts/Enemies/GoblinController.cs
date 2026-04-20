using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Goblin. Enemigo rapido y agresivo, pero fragil.
/// Alta velocidad de movimiento, bajo HP.
///
/// BT identico al Skeleton pero con mayor velocidad y menor HP.
/// </summary>
public class GoblinController : EnemyBase
{
    private Node _btRoot;

    protected override void Awake()
    {
        base.Awake();
        maxHealth      = 25f;
        damage         = 6f;
        moveSpeed      = 4.5f;   // Muy rapido
        attackRange    = 1.2f;
        attackCooldown = 0.8f;   // Ataca rapido
        detectionRange = 10f;    // Detecta desde lejos
        coinReward     = 10;
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
        Debug.Log("[GoblinController] Goblin eliminado.");
        base.Die();
    }

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 25f  * hpMult;
        currentHealth = maxHealth;
        damage        = 6f   * dmgMult;
        moveSpeed     = 4.5f * spdMult;
        SetupBT();
    }
}
