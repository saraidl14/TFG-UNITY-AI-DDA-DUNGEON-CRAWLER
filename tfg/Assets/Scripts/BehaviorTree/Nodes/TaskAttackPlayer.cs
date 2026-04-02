using UnityEngine;
using BehaviorTree;

/// <summary>
/// Accion BT: ataca al jugador si el cooldown lo permite.
/// Llama a PlayerHealth.TakeDamage con el dano del enemigo.
/// Devuelve SUCCESS al ejecutar el ataque, FAILURE si el cooldown no ha terminado.
/// </summary>
public class TaskAttackPlayer : Node
{
    private float _damage;
    private float _attackCooldown;
    private float _lastAttackTime = -999f;

    public TaskAttackPlayer(float damage, float attackCooldown)
    {
        _damage         = damage;
        _attackCooldown = attackCooldown;
    }

    public override NodeState Evaluate()
    {
        // Comprobar cooldown
        if (Time.time < _lastAttackTime + _attackCooldown)
        {
            state = NodeState.FAILURE;
            return state;
        }

        object playerObj = GetData("player");
        if (playerObj == null)
        {
            state = NodeState.FAILURE;
            return state;
        }

        Transform player = (Transform)playerObj;
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(_damage);
            _lastAttackTime = Time.time;
            Debug.Log($"[TaskAttackPlayer] Ataque ejecutado. Dano: {_damage}");
        }

        state = NodeState.SUCCESS;
        return state;
    }
}
