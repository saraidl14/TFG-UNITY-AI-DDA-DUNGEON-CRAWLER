/*  Nombre:      TaskPatrol.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       09/04/2026
 *  Descripcion: Acción BT: el enemigo patrulla aleatoriamente dentro de su radio de sala.
 */
using UnityEngine;
using UnityEngine.AI;
using BehaviorTree;

/// <summary>
/// Accion BT: el enemigo patrulla aleatoriamente dentro de su radio de sala.
/// Elige un punto aleatorio valido en el NavMesh, camina hasta el, espera
/// un momento y repite. Se activa cuando no detecta al jugador.
///
/// Usa NavMeshAgent para respetar paredes y geometria de la sala.
/// </summary>
public class TaskPatrol : Node
{
    private readonly Transform _self;
    private readonly float     _speed;
    private readonly float     _maxRoam;
    private readonly Vector3   _spawnPos;
    private readonly float     _waitTime;

    private bool  _hasTarget  = false;
    private bool  _waiting    = false;
    private float _waitTimer  = 0f;

    /// <param name="waitTime">Segundos que espera al llegar a cada punto de patrulla.</param>
    public TaskPatrol(Transform self, float speed, float maxRoam, Vector3 spawnPos, float waitTime = 2f)
    {
        _self      = self;
        _speed     = speed;
        _maxRoam   = maxRoam;
        _spawnPos  = spawnPos;
        _waitTime  = waitTime;
    }

    public override NodeState Evaluate()
    {
        NavMeshAgent agent = _self.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh) { state = NodeState.FAILURE; return state; }

        // ── Fase de espera en el punto de patrulla ──
        if (_waiting)
        {
            agent.isStopped = true;
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting   = false;
                _hasTarget = false;
            }
            state = NodeState.RUNNING;
            return state;
        }

        // ── Elegir nuevo destino ──
        if (!_hasTarget)
        {
            if (!PickNewTarget(agent))
            {
                // No encontro punto valido: esperar un poco y reintentar
                state = NodeState.RUNNING;
                return state;
            }
        }

        // ── Moverse hacia el destino ──
        agent.isStopped = false;
        agent.speed     = _speed * 0.55f;   // mas lento que al perseguir

        // Llegamos: esperar antes de ir al siguiente punto
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            _waiting   = true;
            _waitTimer = _waitTime;
            _hasTarget = false;
        }

        state = NodeState.RUNNING;
        return state;
    }

    // ─────────────────────────────────────────────
    // UTIL
    // ─────────────────────────────────────────────

    private bool PickNewTarget(NavMeshAgent agent)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 rand2D    = Random.insideUnitCircle * _maxRoam;
            Vector3 candidate = _spawnPos + new Vector3(rand2D.x, 0f, rand2D.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                _hasTarget = true;
                return true;
            }
        }
        return false;
    }
}
