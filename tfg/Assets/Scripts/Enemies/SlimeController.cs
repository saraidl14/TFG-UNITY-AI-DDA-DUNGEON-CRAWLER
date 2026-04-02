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
/// Preparado para DDA: ApplyDifficultyScaling() escala stats en tiempo real.
/// </summary>
public class SlimeController : EnemyBase
{
    // Raiz del arbol BT
    private Node _btRoot;

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
    }

    protected override void Start()
    {
        base.Start(); // Busca al jugador y guarda spawnPosition
        SetupBT();
    }

    // ─────────────────────────────────────────────
    // CONSTRUCCION DEL ARBOL BT
    // ─────────────────────────────────────────────

    private void SetupBT()
    {
        // Rama atacar: detectado + en rango + atacar
        var attackSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new CheckPlayerInAttackRange(transform, attackRange),
            new TaskAttackPlayer(damage, attackCooldown)
        });

        // Rama perseguir: detectado + moverse hacia el jugador
        var chaseSequence = new Sequence(new List<Node>
        {
            new CheckPlayerDetected(transform, detectionRange),
            new TaskChasePlayer(transform, moveSpeed, maxRoamDistance, spawnPosition)
        });

        // Raiz: intentar atacar primero, si no, perseguir
        _btRoot = new Selector(new List<Node>
        {
            attackSequence,
            chaseSequence
        });

        // Guardar referencia al jugador en el blackboard
        if (player != null)
            _btRoot.SetData("player", player);
    }

    // ─────────────────────────────────────────────
    // HOOK DE ENEMYBASE
    // ─────────────────────────────────────────────

    protected override void UpdateBehavior()
    {
        // Mantener el blackboard actualizado
        if (player != null)
            _btRoot?.SetData("player", player);

        _btRoot?.Evaluate();
    }

    // Attack() no se usa directamente: lo gestiona TaskAttackPlayer en el BT
    protected override void Attack() { }

    // ─────────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────────

    protected override void Die()
    {
        Debug.Log("[SlimeController] Slime eliminado.");
        // TODO: particulas, drop de monedas, sonido
        base.Die();
    }

    // ─────────────────────────────────────────────
    // DDA: SCALING EN TIEMPO REAL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Escala los stats del Slime segun el nivel de dificultad del DDA.
    /// Llamado por DifficultyManager al subir o bajar el nivel.
    /// </summary>
    public void ApplyDifficultyScaling(float hpMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHealth     = 30f * hpMultiplier;
        currentHealth = maxHealth;
        damage        = 5f  * damageMultiplier;
        moveSpeed     = 2f  * speedMultiplier;

        // Reconstruir BT con los nuevos stats
        SetupBT();

        Debug.Log($"[SlimeController] DDA aplicado | HP:{maxHealth} | DMG:{damage} | SPD:{moveSpeed}");
    }
}
