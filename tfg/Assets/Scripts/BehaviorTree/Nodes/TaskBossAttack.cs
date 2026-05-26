/*  Nombre:      TaskBossAttack.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       09/04/2026
 *  Descripcion: Acción BT exclusiva del boss: ataca al jugador diferenciando fase normal y enraged.
 */
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Accion BT exclusiva del boss: ataca al jugador.
/// Igual que TaskAttackPlayer pero diferencia fase normal y fase enfurecida
/// para el log y posibles efectos futuros (particulas, sonido, etc.).
/// </summary>
public class TaskBossAttack : Node
{
    private readonly float         _damage;
    private readonly float         _cooldown;
    private readonly bool          _isEnraged;
    private readonly System.Action _onAttack;  // callback → animación en BossController

    private float _lastAttackTime = -999f;

    /// <param name="damage">Daño del ataque.</param>
    /// <param name="cooldown">Tiempo entre ataques en segundos.</param>
    /// <param name="isEnraged">True si es la fase enfurecida.</param>
    /// <param name="onAttack">Callback opcional: se invoca al impactar (para triggear animación).</param>
    public TaskBossAttack(float damage, float cooldown, bool isEnraged = false, System.Action onAttack = null)
    {
        _damage    = damage;
        _cooldown  = cooldown;
        _isEnraged = isEnraged;
        _onAttack  = onAttack;
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

        Transform player = (Transform)playerObj;
        PlayerHealth ph  = player.GetComponent<PlayerHealth>();

        if (ph == null)
        {
            ph = player.GetComponentInParent<PlayerHealth>();
            if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();
        }

        if (ph != null)
        {
            ph.TakeDamage(_damage);
            _lastAttackTime = Time.time;
            _onAttack?.Invoke();   // notifica al BossController para que elija la animación

            string fase = _isEnraged ? "FURY" : "normal";
            Debug.Log($"[TaskBossAttack] [{fase}] Daño: {_damage}");
        }
        else
        {
            Debug.LogWarning("[TaskBossAttack] PlayerHealth no encontrado en el jugador.");
        }

        state = NodeState.SUCCESS;
        return state;
    }
}
