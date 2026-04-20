using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Orco. Tanque: muchisimo HP y daño, pero muy lento.
/// Enemigo de alto nivel (aparece en dificultades 6+).
///
/// BT identico al Skeleton pero con stats de tanque.
/// </summary>
public class OrcController : EnemyBase
{
    private Node _btRoot;

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
        Debug.Log("[OrcController] Orco eliminado.");
        base.Die();
    }

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 120f * hpMult;
        currentHealth = maxHealth;
        damage        = 18f  * dmgMult;
        moveSpeed     = 1.5f * spdMult;
        SetupBT();
    }
}
