using UnityEngine;
using BehaviorTree;

/// <summary>
/// Accion BT: el enemigo lanza un ataque en area cuando el jugador esta muy cerca.
/// El dano real (DoT) lo gestiona el controlador via el callback onAttack.
/// </summary>
public class TaskAreaAttack : Node
{
    private readonly Transform    _self;
    private readonly float        _range;
    private readonly float        _cooldown;
    private readonly System.Action _onAttack;

    private float _lastAttackTime = -999f;

    public TaskAreaAttack(Transform self, float range, float cooldown, System.Action onAttack)
    {
        _self      = self;
        _range     = range;
        _cooldown  = cooldown;
        _onAttack  = onAttack;
    }

    public override NodeState Evaluate()
    {
        object playerObj = GetData("player");
        if (playerObj == null) { state = NodeState.FAILURE; return state; }

        Transform player = (Transform)playerObj;

        if (Vector3.Distance(_self.position, player.position) > _range)
        { state = NodeState.FAILURE; return state; }

        // En cooldown: cede el turno a otros nodos (flee, shoot...)
        if (Time.time < _lastAttackTime + _cooldown)
        { state = NodeState.FAILURE; return state; }

        _lastAttackTime = Time.time;
        _onAttack?.Invoke();

        state = NodeState.SUCCESS;
        return state;
    }
}
