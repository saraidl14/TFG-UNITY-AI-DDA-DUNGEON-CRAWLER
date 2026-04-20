using UnityEngine;
using BehaviorTree;

/// <summary>
/// Accion BT: el enemigo retrocede alejandose del jugador.
/// Usada por el Mago cuando el jugador entra en su radio de huida.
/// </summary>
public class TaskFleeFromPlayer : Node
{
    private readonly Transform _self;
    private readonly float     _speed;
    private readonly float     _maxRoam;
    private readonly Vector3   _spawnPos;

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

        // Dirección opuesta al jugador
        Vector3 fleeDir = (_self.position - player.position).normalized;
        Vector3 nextPos = _self.position + fleeDir * _speed * Time.deltaTime;

        // No salir del radio de la sala
        if (Vector3.Distance(nextPos, _spawnPos) <= _maxRoam)
            _self.position = nextPos;

        _self.LookAt(new Vector3(player.position.x, _self.position.y, player.position.z));

        state = NodeState.SUCCESS;
        return state;
    }
}
