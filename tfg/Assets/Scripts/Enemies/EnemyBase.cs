using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float moveSpeed = 3f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float detectionRange = 8f;

    [Header("Room Bounds")]
    public float maxRoamDistance = 8f;

    [Header("Recompensa")]
    [Tooltip("Monedas que da este enemigo al morir. 0 = sin recompensa directa (ej: boss).")]
    public int coinReward = 20;

    private Animation anim;

    protected float currentHealth;
    protected Transform player;
    protected float lastAttackTime;
    protected Vector3 spawnPosition;

    private bool _isBeingKnockedBack = false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        // Los enemigos se mueven por BT/NavMeshAgent, nunca por fisica.
        // Desactivar gravedad del Rigidbody para evitar que se hundan en el suelo.
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        // Hacer warp al punto NavMesh mas cercano al spawnear
        // Evita que el enemigo empiece fuera del NavMesh y caiga
        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(
                transform.position, out UnityEngine.AI.NavMeshHit hit, 5f,
                UnityEngine.AI.NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
            }
        }

        spawnPosition = transform.position;   // guardar DESPUÉS del warp

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Registrarse en EnemiesControllers para el tracking DDA
        if (EnemiesControllers.Instance != null)
            EnemiesControllers.Instance.RegisterEnemy(this);

        // Aplicar scaling del nivel actual al spawnar
        ApplyCurrentDifficultyScaling();
    }

    protected virtual void Update()
    {
        if (player == null) return;
        if (_isBeingKnockedBack) return; // el BT no corre durante el knockback
        UpdateBehavior();
    }

    // Hook principal: cada enemigo define su comportamiento aqui.
    // Cuando se integre BT para DDA, se sobreescribe este metodo.
    protected abstract void UpdateBehavior();

    // Hook de ataque: cada enemigo define como ataca
    protected abstract void Attack();

    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        { 
            Die(); 
            //meter sistema de particulas y la animacion
        }
    }

    protected virtual void Die()
    {
        // Dar monedas por matar este enemigo
        if (coinReward > 0 && GameManager.Instance != null)
            GameManager.Instance.AddCoins(coinReward);

        // Desregistrarse: EnemiesControllers detecta si la sala queda limpia
        if (EnemiesControllers.Instance != null)
            EnemiesControllers.Instance.UnregisterEnemy(this);

        Destroy(gameObject);
    }

    /// <summary>Devuelve la salud actual del enemigo (usado por PlayerCombat para detectar muertes).</summary>
    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Aplica un impulso de knockback al enemigo en la direccion indicada.
    /// Desactiva el NavMeshAgent brevemente para que el movimiento sea visible.
    /// </summary>
    public virtual void Knockback(Vector3 direction, float force)
    {
        StartCoroutine(KnockbackCoroutine(direction.normalized, force));
    }

    private IEnumerator KnockbackCoroutine(Vector3 dir, float force)
    {
        _isBeingKnockedBack = true;

        var nav = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (nav != null) { nav.isStopped = true; nav.ResetPath(); }

        // Calcular posicion destino del knockback
        Vector3 targetPos = transform.position + dir * force;

        // Buscar el punto valido mas cercano en el NavMesh hacia esa direccion
        if (nav != null)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(
                    targetPos, out UnityEngine.AI.NavMeshHit hit, force, UnityEngine.AI.NavMesh.AllAreas))
                nav.Warp(hit.position);  // teletransporte instantaneo al punto de impacto
        }
        else
        {
            transform.position = targetPos;
        }

        // Pausa breve para que se vea el desplazamiento antes de reanudar
        yield return new WaitForSeconds(0.15f);

        if (nav != null) nav.isStopped = false;
        _isBeingKnockedBack = false;
    }

    /// <summary>
    /// Aplica el scaling del nivel de dificultad actual al spawnear.
    /// Cada subclase guarda sus stats base en Awake() ANTES de llamar a Start(),
    /// por lo que aqui ya estan disponibles y se multiplican correctamente.
    /// </summary>
    protected virtual void ApplyCurrentDifficultyScaling()
    {
        if (DifficultyManager.Instance == null) return;

        float hpMult  = DifficultyManager.Instance.GetHPScaling();
        float dmgMult = DifficultyManager.Instance.GetDamageScaling();
        float spdMult = DifficultyManager.Instance.GetSpeedScaling();

        maxHealth     = maxHealth  * hpMult;
        currentHealth = maxHealth;
        damage        = damage     * dmgMult;
        moveSpeed     = moveSpeed  * spdMult;

        Debug.Log($"[{GetType().Name}] Scaling nivel {DifficultyManager.Instance.currentLevel} aplicado | " +
                  $"HP:{maxHealth:F0} | DMG:{damage:F1} | SPD:{moveSpeed:F2}");
    }

    protected bool PlayerInRange()
    {
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    protected bool PlayerDetected()
    {
        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    protected void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 nextPosition = transform.position + direction * moveSpeed * Time.deltaTime;

        // No sale de su sala
        if (Vector3.Distance(nextPosition, spawnPosition) > maxRoamDistance) return;

        transform.position = nextPosition;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }
}
