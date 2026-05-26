/*  Nombre:      TaskChasePlayer.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       09/04/2026
 *  Descripcion: Acción BT: mueve al enemigo hacia el jugador usando NavMeshAgent.
 */
using UnityEngine;
using UnityEngine.AI;
using BehaviorTree;

/// <summary>
/// Accion BT: mueve al enemigo hacia el jugador usando NavMeshAgent.
/// Si el enemigo no tiene NavMeshAgent cae a movimiento directo (sin colisiones).
/// No sale de su sala gracias al límite maxRoamDistance.
/// </summary>
public class TaskChasePlayer : Node
{
    private readonly Transform _enemyTransform;
    private readonly float     _moveSpeed;
    private readonly float     _maxRoamDistance;
    private readonly Vector3   _spawnPosition;

    /// <summary>
    /// Si true, el modelo está exportado al revés (forward = -Z).
    /// Se invierte la dirección de rotación para que la cara mire al jugador.
    /// Usa esto en enemigos cuyo modelo Humanoid da la espalda con rotación Y=0.
    /// </summary>
    private readonly bool _invertRotation;

    public TaskChasePlayer(Transform enemyTransform, float moveSpeed,
                           float maxRoamDistance, Vector3 spawnPosition,
                           bool invertRotation = false)
    {
        _enemyTransform  = enemyTransform;
        _moveSpeed       = moveSpeed;
        _maxRoamDistance = maxRoamDistance;
        _spawnPosition   = spawnPosition;
        _invertRotation  = invertRotation;
    }

    public override NodeState Evaluate()
    {
        object playerObj = GetData("player");
        if (playerObj == null) { state = NodeState.FAILURE; return state; }

        Transform player = (Transform)playerObj;

        NavMeshAgent agent = _enemyTransform.GetComponent<NavMeshAgent>();

        if (agent != null && agent.isOnNavMesh)
        {
            // ── Movimiento con NavMesh: respeta paredes ──
            agent.speed     = _moveSpeed;
            agent.isStopped = false;

            if (Vector3.Distance(player.position, _spawnPosition) <= _maxRoamDistance)
            {
                agent.SetDestination(player.position);

                // Rotación manual para modelos exportados al revés (Humanoid con forward=-Z)
                if (_invertRotation)
                {
                    Vector3 dir = player.position - _enemyTransform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        _enemyTransform.rotation = Quaternion.Slerp(
                            _enemyTransform.rotation,
                            Quaternion.LookRotation(-dir),   // negativo: root mira atrás, modelo mira al jugador
                            Time.deltaTime * 10f);
                    }
                }
            }
            else
            {
                agent.ResetPath();
            }
        }
        else
        {
            // Sin NavMeshAgent o fuera del NavMesh: intentar warp al punto mas cercano
            // Si no es posible, no moverse (mejor quieto que atravesando paredes)
            if (agent != null && !agent.isOnNavMesh)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(
                    _enemyTransform.position, out UnityEngine.AI.NavMeshHit hit, 2f,
                    UnityEngine.AI.NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }
        }

        state = NodeState.RUNNING;
        return state;
    }
}
