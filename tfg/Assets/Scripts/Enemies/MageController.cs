using System.Collections.Generic;
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Controlador del Mago. Enemigo de largo alcance que huye del cuerpo a cuerpo.
///
/// BT:
///   Selector
///   ├─ Sequence (huir si el jugador esta muy cerca)
///   │   ├─ CheckPlayerDetected
///   │   ├─ CheckPlayerTooClose
///   │   └─ TaskFleeFromPlayer
///   ├─ Sequence (disparar si el jugador esta en rango)
///   │   ├─ CheckPlayerDetected
///   │   └─ TaskShootProjectile (bola de fuego)
///   └─ Sequence (perseguir si esta demasiado lejos para disparar)
///       ├─ CheckPlayerDetected
///       └─ TaskChasePlayer
/// </summary>
public class MageController : EnemyBase
{
    [Header("Ataque a distancia")]
    [Tooltip("Prefab de la bola de fuego. Debe tener EnemyProjectile.")]
    public GameObject spellPrefab;

    [Tooltip("Velocidad del proyectil magico.")]
    public float spellSpeed = 10f;

    [Tooltip("Rango maximo de disparo del mago.")]
    public float castRange = 10f;

    [Tooltip("Cooldown entre lanzamientos.")]
    public float castCooldown = 2.5f;

    [Tooltip("Distancia a la que el mago empieza a huir del jugador.")]
    public float fleeRange = 3f;

    private Node _btRoot;

    protected override void Awake()
    {
        base.Awake();
        maxHealth      = 40f;
        damage         = 12f;
        moveSpeed      = 2.5f;
        attackRange    = castRange;
        attackCooldown = castCooldown;
        detectionRange = 12f;
        coinReward     = 25;
        currentHealth  = maxHealth;
    }

    protected override void Start()
    {
        base.Start();
        SetupBT();
    }

    private void SetupBT()
    {
        // Rama 1: huir si el jugador está encima
        var fleeSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new CheckPlayerTooClose(transform, fleeRange),
            new TaskFleeFromPlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        // Rama 2: disparar hechizo
        var shootSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskShootProjectile(transform, spellPrefab, damage, spellSpeed, castCooldown, castRange)
        });

        // Rama 3: acercarse si el jugador está fuera de rango
        var chaseSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        _btRoot = new Selector(new List<Node> { fleeSequence, shootSequence, chaseSequence });

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
        Debug.Log("[MageController] Mago eliminado.");
        base.Die();
    }

    public void ApplyDifficultyScaling(float hpMult, float dmgMult, float spdMult)
    {
        maxHealth     = 40f  * hpMult;
        currentHealth = maxHealth;
        damage        = 12f  * dmgMult;
        moveSpeed     = 2.5f * spdMult;
        SetupBT();
    }
}
