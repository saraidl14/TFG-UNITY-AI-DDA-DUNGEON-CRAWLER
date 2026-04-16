using BehaviorTree;
using UnityEngine;

/// <summary>
/// Accion BT-DDA: mantiene el nivel de dificultad actual sin cambios.
/// Siempre devuelve SUCCESS (rama de fallback del Selector DDA).
/// </summary>
public class TaskMaintainDifficulty : Node
{
    public override NodeState Evaluate()
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.AdjustLevel(0);

        Debug.Log($"[BT-DDA] Dificultad mantenida en nivel {DifficultyManager.Instance?.currentLevel}.");
        state = NodeState.SUCCESS;
        return state;
    }
}
