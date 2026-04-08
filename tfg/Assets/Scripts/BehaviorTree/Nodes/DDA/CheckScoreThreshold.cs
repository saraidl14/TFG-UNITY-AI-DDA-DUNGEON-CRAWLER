using BehaviorTree;

/// <summary>
/// Condicion BT-DDA: comprueba si el score DDA supera (o baja de) un umbral.
/// Lee el score del blackboard (clave "ddaScore") guardado por TaskCollectMetrics.
/// </summary>
public class CheckScoreThreshold : Node
{
    private float _threshold;
    private bool  _checkAbove; // true = score >= threshold | false = score <= threshold

    /// <param name="threshold">Valor umbral a comparar.</param>
    /// <param name="checkAbove">true para comprobar score >= threshold (subir dificultad),
    /// false para score menor o igual (bajar dificultad).</param>
    public CheckScoreThreshold(float threshold, bool checkAbove = true)
    {
        _threshold  = threshold;
        _checkAbove = checkAbove;
    }

    public override NodeState Evaluate()
    {
        object scoreObj = GetData("ddaScore");
        if (scoreObj == null)
        {
            state = NodeState.FAILURE;
            return state;
        }

        float score = (float)scoreObj;

        bool result = _checkAbove ? score >= _threshold : score <= _threshold;
        state = result ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}
