using UnityEngine;
using BehaviorTree;

/// <summary>
/// Accion BT: mueve al enemigo hacia el jugador sin salir de su sala (maxRoamDistance).
/// Siempre devuelve RUNNING mientras el jugador este fuera del rango de ataque.
/// </summary>
public class TaskChasePlayer : Node
{
    private Transform _enemyTransform;
    private float _moveSpeed;
    private float _maxRoamDistance;
    private Vector3 _spawnPosition;

    public TaskChasePlayer(Transform enemyTransform, float moveSpeed, float maxRoamDistance, Vector3 spawnPosition)
    {
        _enemyTransform  = enemyTransform;
        _moveSpeed       = moveSpeed;
        _maxRoamDistance = maxRoamDistance;
        _spawnPosition   = spawnPosition;
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

        Vector3 direction    = (player.position - _enemyTransform.position).normalized;
        Vector3 nextPosition = _enemyTransform.position + direction * _moveSpeed * Time.deltaTime;

        // No salir de la sala
        if (Vector3.Distance(nextPosition, _spawnPosition) > _maxRoamDistance)
        {
            state = NodeState.RUNNING;
            return state;
        }

        _enemyTransform.position = nextPosition;
        _enemyTransform.LookAt(new Vector3(player.position.x, _enemyTransform.position.y, player.position.z));

        state = NodeState.RUNNING;
        return state;
    }
}
