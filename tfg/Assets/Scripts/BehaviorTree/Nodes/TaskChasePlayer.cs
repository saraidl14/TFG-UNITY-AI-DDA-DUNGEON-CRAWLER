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
            // ── Fallback sin NavMesh: movimiento directo (atraviesa paredes) ──
            Vector3 direction    = (player.position - _enemyTransform.position).normalized;
            Vector3 nextPosition = _enemyTransform.position + direction * _moveSpeed * Time.deltaTime;

            if (Vector3.Distance(nextPosition, _spawnPosition) <= _maxRoamDistance)
            {
                _enemyTransform.position = nextPosition;

                Vector3 lookDir = _invertRotation ? -direction : direction;
                if (lookDir != Vector3.zero)
                    _enemyTransform.rotation = Quaternion.LookRotation(
                        new Vector3(lookDir.x, 0f, lookDir.z));
            }
        }

        state = NodeState.RUNNING;
        return state;
    }
}
