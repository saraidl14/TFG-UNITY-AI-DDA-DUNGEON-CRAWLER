using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeController : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();
        maxHealth = 30f;
        damage = 5f;
        moveSpeed = 2f;
        attackRange = 1.2f;
        attackCooldown = 1.5f;
        currentHealth = maxHealth;
    }

    // Comportamiento: persigue al jugador y ataca al llegar al rango
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
        // Aqui se puede añadir animacion de muerte, drop de items, etc.
        base.Die();
    }
}
