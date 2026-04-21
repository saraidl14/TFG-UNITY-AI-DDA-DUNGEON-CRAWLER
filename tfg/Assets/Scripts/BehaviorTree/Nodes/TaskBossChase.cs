using UnityEngine;
using BehaviorTree;

/// <summary>
/// Igual que TaskChasePlayer pero lee moveSpeed del BossController en tiempo real,
/// para que la velocidad enraizada se refleje sin reconstruir el BT.
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

        Transform player  = (Transform)playerObj;
        float speed       = _boss.moveSpeed;   // Lee el valor actual (puede cambiar con enrage)

        Vector3 direction    = (player.position - _enemyTransform.position).normalized;
        Vector3 nextPosition = _enemyTransform.position + direction * speed * Time.deltaTime;

        if (Vector3.Distance(nextPosition, _spawnPosition) <= _maxRoamDistance)
        {
            _enemyTransform.position = nextPosition;
            _enemyTransform.LookAt(new Vector3(player.position.x,
                                               _enemyTransform.position.y,
                                               player.position.z));
        }

        state = NodeState.RUNNING;
        return state;
    }
}
