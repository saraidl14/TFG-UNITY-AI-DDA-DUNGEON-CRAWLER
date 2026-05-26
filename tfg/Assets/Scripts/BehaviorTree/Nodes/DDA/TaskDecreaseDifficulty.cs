/*  Nombre:      TaskDecreaseDifficulty.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       04/05/2026
 *  Descripcion: Tarea BT-DDA: baja el nivel de dificultad en el DifficultyManager.
 */
using BehaviorTree;
using UnityEngine;

/// <summary>
/// Accion BT-DDA: baja el nivel de dificultad en DifficultyManager.
/// </summary>
public class TaskDecreaseDifficulty : Node
{
    private int _amount;

    /// <param name="amount">Cuantos niveles bajar (1, 2 o 3 segun tabla v2.0).</param>
    public TaskDecreaseDifficulty(int amount = 1)
    {
        _amount = amount;
    }

    public override NodeState Evaluate()
    {
        if (DifficultyManager.Instance == null)
        {
            Debug.LogWarning("[TaskDecreaseDifficulty] DifficultyManager no encontrado.");
            state = NodeState.FAILURE;
            return state;
        }

        DifficultyManager.Instance.AdjustLevel(-_amount);
        Debug.Log($"[BT-DDA] Dificultad reducida en {_amount}.");

        state = NodeState.SUCCESS;
        return state;
    }
}
