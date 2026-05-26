/*  Nombre:      CheckPlayerTooClose.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       09/04/2026
 *  Descripcion: Condición BT: comprueba si el jugador está demasiado cerca del enemigo.
 */
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Condicion BT: el jugador esta demasiado cerca (dentro del radio de huida).
/// Usada por el Mago para activar la rama de retroceso.
/// </summary>
public class CheckPlayerTooClose : Node
{
    private readonly Transform _self;
    private readonly float     _fleeRange;

    public CheckPlayerTooClose(Transform self, float fleeRange)
    {
        _self      = self;
        _fleeRange = fleeRange;
    }

    public override NodeState Evaluate()
    {
        object playerObj = GetData("player");
        if (playerObj == null) { state = NodeState.FAILURE; return state; }

        Transform player = (Transform)playerObj;
        float dist = Vector3.Distance(_self.position, player.position);

        state = dist <= _fleeRange ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}
