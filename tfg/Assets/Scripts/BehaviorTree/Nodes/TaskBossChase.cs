/*  Nombre:      TaskBossChase.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       21/04/2026
 *  Descripcion: Acción BT: persecución del boss hacia el jugador usando NavMeshAgent.
 */
using UnityEngine;
using UnityEngine.AI;
using BehaviorTree;

/// <summary>
/// Persecución del Boss usando NavMeshAgent.
/// Lee moveSpeed de BossController en tiempo real para que el enrage
/// (velocidad aumentada) se aplique sin reconstruir el BT.
///
/// Si por algún motivo el agente no está disponible, cae a movimiento
/// directo como fallback (sin colisiones con paredes).
/// </summary>
public class TaskBossChase : Node
{
    private readonly Transform      _enemyTransform;
    private readonly BossController _boss;
    private readonly float          _maxRoamDistance;
    private readonly Vector3        _spawnPosition;

    public TaskBossChase(Transform enemyTransform, BossController boss,
                         float maxRoamDistance, Vector3 spawnPosition)
    {
        _enemyTransform  = enemyTransform;
        _boss            = boss;
        _maxRoamDistance = maxRoamDistance;
        _spawnPosition   = spawnPosition;
    }

    public override NodeState Evaluate()
    {
        object playerObj = GetData("player");
        if (playerObj == null) { state = NodeState.FAILURE; return state; }

        Transform player = (Transform)playerObj;
        float speed      = _boss.moveSpeed;   // Lee en tiempo real (cambia al enragiarse)

        NavMeshAgent agent = _enemyTransform.GetComponent<NavMeshAgent>();

        if (agent != null && agent.isOnNavMesh)
        {
            // ── Movimiento con NavMesh: respeta paredes ──
            agent.speed     = speed;
            agent.isStopped = false;

            // Perseguir solo si el jugador está dentro del límite de sala
            if (Vector3.Distance(player.position, _spawnPosition) <= _maxRoamDistance)
                agent.SetDestination(player.position);
            else
                agent.ResetPath();
        }
        else
        {
            // ── Fallback sin NavMesh: movimiento directo (atraviesa paredes) ──
            // Solo ocurre si el prefab no tiene NavMeshAgent o el NavMesh no está horneado.
            Vector3 direction    = (player.position - _enemyTransform.position).normalized;
            Vector3 nextPosition = _enemyTransform.position + direction * speed * Time.deltaTime;

            if (Vector3.Distance(nextPosition, _spawnPosition) <= _maxRoamDistance)
            {
                _enemyTransform.position = nextPosition;
                _enemyTransform.LookAt(
                    new Vector3(player.position.x, _enemyTransform.position.y, player.position.z));
            }
        }

        state = NodeState.RUNNING;
        return state;
    }
}
