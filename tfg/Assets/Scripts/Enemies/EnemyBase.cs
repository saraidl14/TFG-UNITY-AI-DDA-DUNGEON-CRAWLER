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

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        spawnPosition = transform.position;
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
    /// Aplica el scaling del nivel de dificultad actual al spawnear.
    /// Cada subclase guarda sus stats base en Awake() ANTES de llamar a Start(),
    /// por lo que aqui ya estan disponibles y se multiplican correctamente.
    /// </summary>
    protected void ApplyCurrentDifficultyScaling()
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
