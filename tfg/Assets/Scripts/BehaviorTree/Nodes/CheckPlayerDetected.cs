/*  Nombre:      CheckPlayerDetected.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       09/04/2026
 *  Descripcion: Condición BT: comprueba si el jugador está en el rango de detección.
 */
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Condicion BT: comprueba si el jugador esta dentro del rango de deteccion del enemigo.
/// Usa el blackboard para obtener la referencia al transform del jugador y al enemigo.
/// </summary>
public class CheckPlayerDetected : Node
{
    private Transform _enemyTransform;
    private float _detectionRange;

    public CheckPlayerDetected(Transform enemyTransform, float detectionRange)
    {
        _enemyTransform = enemyTransform;
        _detectionRange = detectionRange;
    }

    public override NodeState Evaluate()
    {
        // Obtener el jugador del blackboard (lo guarda el enemigo en SetupBT)
        object playerObj = GetData("player");
        if (playerObj == null)
        {
            state = NodeState.FAILURE;
            return state;
        }

        Transform player = (Transform)playerObj;
        float distance = Vector3.Distance(_enemyTransform.position, player.position);

        state = distance <= _detectionRange ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}
