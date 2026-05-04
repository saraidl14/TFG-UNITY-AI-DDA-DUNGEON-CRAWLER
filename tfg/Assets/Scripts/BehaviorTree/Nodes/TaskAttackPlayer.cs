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

    /// <summary>
    /// Callback opcional: se invoca justo cuando el ataque impacta.
    /// Úsalo para lanzar la animación de ataque desde el controlador del enemigo.
    /// </summary>
    private readonly System.Action _onAttack;

    public TaskAttackPlayer(float damage, float attackCooldown, System.Action onAttack = null)
    {
        _damage         = damage;
        _attackCooldown = attackCooldown;
        _onAttack       = onAttack;
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

        if (playerHealth == null)
        {
            // Buscar también en padres/hijos por si PlayerHealth no está en el root
            playerHealth = player.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(_damage);
            _lastAttackTime = Time.time;
            _onAttack?.Invoke();   // notifica al controlador del enemigo (animación, etc.)
            Debug.Log($"[TaskAttackPlayer] Ataque ejecutado. Daño: {_damage}");
        }
        else
        {
            Debug.LogWarning("[TaskAttackPlayer] PlayerHealth no encontrado en el jugador. ¿Está el componente en el prefab?");
        }

        state = NodeState.SUCCESS;
        return state;
    }
}
