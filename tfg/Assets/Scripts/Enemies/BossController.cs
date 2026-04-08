using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : EnemyBase
{
    // Evento que se dispara cuando el boss muere (para la transicion de sala)
    public static event System.Action OnBossDead;

    protected override void Awake()
    {
        base.Awake();
        maxHealth = 80f;
        damage = 15f;
        moveSpeed = 3.5f;
        attackRange = 2f;
        attackCooldown = 2f;
        currentHealth = maxHealth;
    }

    // Comportamiento: persigue y ataca. Preparado para BT/DDA mas adelante
    protected override void UpdateBehavior()
    {
        if (!PlayerDetected()) return; // Fuera de rango, no hace nada

        if (PlayerInRange())
        {
            if (Time.time >= lastAttackTime + attackCooldown)
                Attack();
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    protected override void Attack()
    {
        lastAttackTime = Time.time;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damage);
    }

    protected override void Die()
    {
        OnBossDead?.Invoke(); // Avisa al sistema de salas para hacer la transicion
        base.Die();
    }

    /// <summary>Escala los stats del Boss segun el nivel DDA. Llamado por EnemiesControllers.</summary>
    public void ApplyDifficultyScaling(float hpMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHealth     = 80f  * hpMultiplier;
        currentHealth = maxHealth;
        damage        = 15f  * damageMultiplier;
        moveSpeed     = 3.5f * speedMultiplier;

        Debug.Log($"[BossController] DDA aplicado | HP:{maxHealth} | DMG:{damage} | SPD:{moveSpeed}");
    }
}
