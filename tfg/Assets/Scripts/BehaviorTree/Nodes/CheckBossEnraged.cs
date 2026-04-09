using BehaviorTree;

/// <summary>
/// Condicion BT: comprueba si el boss esta en FASE 2 (enfurecido).
/// Lee "bossHpRatio" del blackboard (0-1).
/// Devuelve SUCCESS si HP actual <= umbral (por defecto 50%).
/// </summary>
public class CheckBossEnraged : Node
{
    private readonly float _threshold;

    /// <param name="hpThreshold">Porcentaje de HP al que se activa la fase 2 (0-1). Por defecto 0.5.</param>
    public CheckBossEnraged(float hpThreshold = 0.5f)
    {
        _threshold = hpThreshold;
    }

    public override NodeState Evaluate()
    {
        object hpRatioObj = GetData("bossHpRatio");
        if (hpRatioObj == null)
        {
            state = NodeState.FAILURE;
            return state;
        }

        float hpRatio = (float)hpRatioObj;
        state = hpRatio <= _threshold ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}
