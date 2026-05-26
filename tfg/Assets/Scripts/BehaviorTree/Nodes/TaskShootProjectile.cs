/*  Nombre:      TaskShootProjectile.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       09/04/2026
 *  Descripcion: Acción BT: el enemigo dispara un proyectil hacia el jugador con cooldown.
 */
using UnityEngine;
using BehaviorTree;

/// <summary>
/// Accion BT: dispara un proyectil hacia el jugador.
/// Usada por el Mago (bola de fuego) y la Araña (tela).
/// El prefab del proyectil debe tener el script EnemyProjectile.
/// </summary>
public class TaskShootProjectile : Node
{
    private readonly Transform      _self;
    private readonly GameObject     _projectilePrefab;
    private readonly float          _damage;
    private readonly float          _speed;
    private readonly float          _cooldown;
    private readonly float          _maxRange;
    private readonly System.Action  _onShoot;   // callback → animación en el controlador

    private float _lastShootTime = -999f;

    public TaskShootProjectile(Transform self, GameObject prefab,
                               float damage, float speed,
                               float cooldown, float maxRange,
                               System.Action onShoot = null)
    {
        _self             = self;
        _projectilePrefab = prefab;
        _damage           = damage;
        _speed            = speed;
        _cooldown         = cooldown;
        _maxRange         = maxRange;
        _onShoot          = onShoot;
    }

    public override NodeState Evaluate()
    {
        object playerObj = GetData("player");
        if (playerObj == null) { state = NodeState.FAILURE; return state; }

        Transform player = (Transform)playerObj;
        float dist = Vector3.Distance(_self.position, player.position);

        // Fuera de rango de disparo → FAILURE para que chase lo acerque
        if (dist > _maxRange) { state = NodeState.FAILURE; return state; }

        // En rango pero en cooldown → RUNNING: el mago espera quieto sin acercarse
        // (FAILURE aqui haría caer al chase sequence y el mago se pegaría al jugador)
        if (Time.time < _lastShootTime + _cooldown) { state = NodeState.RUNNING; return state; }

        if (_projectilePrefab == null) { state = NodeState.FAILURE; return state; }

        // Orientar hacia el jugador
        _self.LookAt(new Vector3(player.position.x, _self.position.y, player.position.z));

        // Instanciar proyectil desde la posición del enemigo
        Vector3 spawnPos  = _self.position + Vector3.up * 1f;
        Vector3 direction = (player.position + Vector3.up * 1f - spawnPos).normalized;

        GameObject projGO = Object.Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        EnemyProjectile proj = projGO.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            proj.damage = _damage;
            proj.speed  = _speed;
        }

        _lastShootTime = Time.time;
        _onShoot?.Invoke();
        Debug.Log($"[TaskShootProjectile] {_self.name} dispara proyectil. Daño: {_damage}");

        state = NodeState.SUCCESS;
        return state;
    }
}
