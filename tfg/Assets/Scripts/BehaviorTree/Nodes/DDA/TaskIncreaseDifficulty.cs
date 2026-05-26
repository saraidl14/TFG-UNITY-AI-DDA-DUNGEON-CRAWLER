/*  Nombre:      TaskIncreaseDifficulty.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       04/05/2026
 *  Descripcion: Tarea BT-DDA: sube el nivel de dificultad en el DifficultyManager.
 */
using BehaviorTree;
using UnityEngine;

/// <summary>
/// Accion BT-DDA: sube el nivel de dificultad en DifficultyManager.
/// Llama a ApplyAdjustment a traves del metodo publico de DifficultyManager.
/// </summary>
public class TaskIncreaseDifficulty : Node
{
    private int _amount;

    /// <param name="amount">Cuantos niveles subir (1, 2 o 3 segun tabla v2.0).</param>
    public TaskIncreaseDifficulty(int amount = 1)
    {
        _amount = amount;
    }

    public override NodeState Evaluate()
    {
        if (DifficultyManager.Instance == null)
        {
            Debug.LogWarning("[TaskIncreaseDifficulty] DifficultyManager no encontrado.");
            state = NodeState.FAILURE;
            return state;
        }

        DifficultyManager.Instance.AdjustLevel(_amount);
        Debug.Log($"[BT-DDA] Dificultad aumentada en {_amount}.");

        state = NodeState.SUCCESS;
        return state;
    }
}
