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
        maxHealth = 300f;
        damage = 25f;
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
}
