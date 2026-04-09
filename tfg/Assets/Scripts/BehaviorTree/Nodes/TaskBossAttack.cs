using UnityEngine;
using BehaviorTree;

/// <summary>
/// Accion BT exclusiva del boss: ataca al jugador.
/// Igual que TaskAttackPlayer pero diferencia fase normal y fase enfurecida
/// para el log y posibles efectos futuros (particulas, sonido, etc.).
/// </summary>
public class TaskBossAttack : Node
{
    private readonly float _damage;
    private readonly float _cooldown;
    private readonly bool  _isEnraged;   // Solo para log / efectos

    private float _lastAttackTime = -999f;

    /// <param name="damage">Dano del ataque.</param>
    /// <param name="cooldown">Tiempo entre ataques en segundos.</param>
    /// <param name="isEnraged">True si es el ataque de la fase enfurecida.</param>
    public TaskBossAttack(float damage, float cooldown, bool isEnraged = false)
    {
        _damage    = damage;
        _cooldown  = cooldown;
        _isEnraged = isEnraged;
    }

    public override NodeState Evaluate()
    {
        // Cooldown
        if (Time.time < _lastAttackTime + _cooldown)
        {
            state = NodeState.FAILURE;
            return state;
        }

        object playerObj = GetData("player");
        if (playerObj == null)
        {
            state = NodeState.FAILURE;
            return state;
        }

        Transform player      = (Transform)playerObj;
        PlayerHealth ph       = player.GetComponent<PlayerHealth>();

        if (ph != null)
        {
            ph.TakeDamage(_damage);
            _lastAttackTime = Time.time;

            string fase = _isEnraged ? "FURY" : "normal";
            Debug.Log($"[TaskBossAttack] [{fase}] Dano: {_damage}");
        }

        state = NodeState.SUCCESS;
        return state;
    }
}
