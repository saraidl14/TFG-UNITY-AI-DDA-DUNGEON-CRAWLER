/*  Nombre:      CheckPlayerInAttackRange.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       09/04/2026
 *  Descripcion: Condición BT: comprueba si el jugador está en el rango de ataque.
 */
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Condicion BT: comprueba si el jugador esta dentro del rango de ataque del enemigo.
/// </summary>
public class CheckPlayerInAttackRange : Node
{
    private Transform _enemyTransform;
    private float _attackRange;

    public CheckPlayerInAttackRange(Transform enemyTransform, float attackRange)
    {
        _enemyTransform = enemyTransform;
        _attackRange = attackRange;
    }

    public override NodeState Evaluate()
    {
        object playerObj = GetData("player");
        if (playerObj == null)
        {
            state = NodeState.FAILURE;
            return state;
        }

        Transform player = (Transform)playerObj;
        float distance = Vector3.Distance(_enemyTransform.position, player.position);

        state = distance <= _attackRange ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}
