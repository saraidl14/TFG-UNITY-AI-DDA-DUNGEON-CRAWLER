using UnityEngine;
using UnityEngine.AI;
using BehaviorTree;

/// <summary>
/// Accion BT: el enemigo retrocede alejandose del jugador usando NavMeshAgent.
/// Se detiene cuando el jugador esta a mas de safeDistance unidades.
///
/// Correccion v2: usa NavMeshAgent.SetDestination en lugar de manipular
/// _self.position directamente, evitando que el enemigo atraviese paredes y suelo.
/// </summary>
public class TaskFleeFromPlayer : Node
{
    private readonly Transform _self;
    private readonly float     _speed;
    private readonly float     _maxRoam;
    private readonly Vector3   _spawnPos;

    // Cuanto se aleja en cada huida (unidades hacia atras)
    private const float FleeStepDistance = 6f;

    // Si el jugador esta a mas de esta distancia, deja de huir
    private const float SafeDistance = 5f;

    public TaskFleeFromPlayer(Transform self, float speed, float maxRoam, Vector3 spawnPos)
    {
        _self     = self;
        _speed    = speed;
        _maxRoam  = maxRoam;
        _spawnPos = spawnPos;
    }

    public override NodeState Evaluate()
    {
        object playerObj = GetData("player");
        if (playerObj == null) { state = NodeState.FAILURE; return state; }

        Transform player = (Transform)playerObj;
        NavMeshAgent agent = _self.GetComponent<NavMeshAgent>();

        // Sin NavMeshAgent no podemos huir de forma segura
        if (agent == null || !agent.isOnNavMesh) { state = NodeState.FAILURE; return state; }

        float dist = Vector3.Distance(_self.position, player.position);

        // Ya estamos suficientemente lejos: dejar de huir
        if (dist >= SafeDistance)
        {
            agent.isStopped = true;
            state = NodeState.SUCCESS;
            return state;
        }

        // Calcular direccion de huida solo en horizontal (sin Y) para no hundirnos en el suelo
        Vector3 fleeDir2D  = _self.position - player.position;
        fleeDir2D.y        = 0f;
        if (fleeDir2D.sqrMagnitude < 0.001f) fleeDir2D = Vector3.back;
        fleeDir2D = fleeDir2D.normalized;

        // Destino de huida: FleeStepDistance unidades en horizontal, misma Y que el spawn
        Vector3 fleeTarget = _self.position + fleeDir2D * FleeStepDistance;
        fleeTarget.y       = _spawnPos.y;   // fijar al nivel del suelo original

        // Clampear dentro del radio de sala
        Vector3 flat = fleeTarget - _spawnPos;
        flat.y = 0f;
        if (flat.magnitude > _maxRoam)
            fleeTarget = _spawnPos + flat.normalized * _maxRoam;

        // Buscar posicion valida en NavMesh cerca del objetivo de huida
        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.speed     = _speed;
            agent.SetDestination(hit.position);
        }

        // Mirar al jugador mientras retrocede (para poder disparar de frente)
        Vector3 lookDir = player.position - _self.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            _self.rotation = Quaternion.Slerp(
                _self.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 8f);

        state = NodeState.RUNNING;
        return state;
    }
}
