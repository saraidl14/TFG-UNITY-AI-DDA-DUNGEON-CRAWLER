using UnityEngine;

/// <summary>
/// Controla el sistema de combate del jugador:
/// ataque basico (LMB), ataque pesado (RMB) y dash (Shift).
/// Lee inputs cada frame y delega el dash a PlayerHealth.
/// Registra enemigos eliminados para las metricas DDA.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerCombat : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // ATAQUE BASICO
    // ─────────────────────────────────────────────
    [Header("Ataque Basico (LMB)")]
    /// <summary>Dano del ataque basico. Sin coste de stamina.</summary>
    public float basicAttackDamage = 10f;

    /// <summary>Rango del ataque basico en metros (OverlapSphere).</summary>
    public float basicAttackRange = 1.5f;

    /// <summary>Cooldown entre ataques basicos en segundos.</summary>
    public float basicAttackCooldown = 0.4f;

    private float lastBasicAttackTime = -999f;

    // ─────────────────────────────────────────────
    // ATAQUE PESADO
    // ─────────────────────────────────────────────
    [Header("Ataque Pesado (RMB)")]
    /// <summary>Dano del ataque pesado.</summary>
    public float heavyAttackDamage = 25f;

    /// <summary>Rango del ataque pesado en metros.</summary>
    public float heavyAttackRange = 2f;

    /// <summary>Cooldown del ataque pesado en segundos.</summary>
    public float heavyAttackCooldown = 1f;

    /// <summary>Coste de stamina del ataque pesado.</summary>
    public float heavyAttackStaminaCost = 30f;

    private float lastHeavyAttackTime = -999f;

    // ─────────────────────────────────────────────
    // DASH
    // ─────────────────────────────────────────────
    [Header("Dash (Shift)")]
    /// <summary>
    /// Cooldown propio del componente PlayerCombat para el dash.
    /// El cooldown real lo gestiona PlayerHealth; este es solo
    /// la ventana de input para evitar spam.
    /// </summary>
    public float dashInputCooldown = 2f;
    private float lastDashInputTime = -999f;

    // ─────────────────────────────────────────────
    // CAPAS DE ENEMIGOS
    // ─────────────────────────────────────────────
    [Header("Deteccion de Enemigos")]
    /// <summary>
    /// LayerMask con la capa de enemigos.
    /// Asignar en el Inspector (layer "Enemy").
    /// </summary>
    public LayerMask enemyLayer;

    // ─────────────────────────────────────────────
    // METRICAS DDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Enemigos eliminados en la sala actual.
    /// DifficultyManager y ScoreManager leen este valor.
    /// </summary>
    [HideInInspector]
    public int enemiesKilledThisRoom = 0;

    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────
    private PlayerHealth playerHealth;
    private Camera playerCamera;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        // Buscar la camara hija del jugador
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            Debug.LogWarning("[PlayerCombat] No se encontro Camera en el jugador.");
    }

    private void Update()
    {
        HandleBasicAttack();
        HandleHeavyAttack();
        HandleDash();
    }

    // ─────────────────────────────────────────────
    // ATAQUE BASICO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ataque basico con click izquierdo.
    /// Sin coste de stamina. Dano base 10.
    /// </summary>
    private void HandleBasicAttack()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time < lastBasicAttackTime + basicAttackCooldown) return;

        lastBasicAttackTime = Time.time;
        Debug.Log("[PlayerCombat] Ataque basico ejecutado.");
        PerformAttack(basicAttackDamage, basicAttackRange);
    }

    // ─────────────────────────────────────────────
    // ATAQUE PESADO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ataque pesado con click derecho.
    /// Consume 30 de stamina. Dano base 25.
    /// </summary>
    private void HandleHeavyAttack()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (Time.time < lastHeavyAttackTime + heavyAttackCooldown) return;

        // Intentar gastar stamina; si no hay suficiente, cancelar
        if (!playerHealth.TrySpendStamina(heavyAttackStaminaCost))
        {
            Debug.Log("[PlayerCombat] Stamina insuficiente para ataque pesado.");
            return;
        }

        lastHeavyAttackTime = Time.time;
        Debug.Log("[PlayerCombat] Ataque pesado ejecutado.");
        PerformAttack(heavyAttackDamage, heavyAttackRange);
    }

    // ─────────────────────────────────────────────
    // LOGICA DE ATAQUE COMPARTIDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Detecta enemigos en el rango con OverlapSphere y aplica dano.
    /// Punto de origen: posicion del jugador.
    /// </summary>
    private void PerformAttack(float damage, float range)
    {
        // Detectar todos los colliders de enemigos en el rango
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            // Aplicar dano al enemigo
            float hpBefore = enemy.GetCurrentHealth();
            enemy.TakeDamage(damage);
            float hpAfter = enemy.GetCurrentHealth();

            Debug.Log($"[PlayerCombat] Dano {damage} a {hit.name} | HP: {hpBefore} -> {hpAfter}");

            // Verificar si el enemigo murio (HP llego a 0)
            if (hpAfter <= 0f)
            {
                enemiesKilledThisRoom++;

                // Notificar al MetricsTracker para el DDA
                if (MetricsTracker.Instance != null)
                    MetricsTracker.Instance.RegisterKill();

                Debug.Log($"[PlayerCombat] Enemigo eliminado. Total en sala: {enemiesKilledThisRoom}");
            }
        }
    }

    // ─────────────────────────────────────────────
    // DASH
    // ─────────────────────────────────────────────

    /// <summary>
    /// Dash con Shift. Sin coste de stamina. Cooldown 2s.
    /// Delega la logica de movimiento a PlayerHealth.TryDash().
    /// </summary>
    private void HandleDash()
    {
        if (!Input.GetKeyDown(KeyCode.LeftShift)) return;
        if (Time.time < lastDashInputTime + dashInputCooldown) return;

        // Calcular la direccion de movimiento actual del jugador
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = (transform.right * x + transform.forward * z);

        // Si no hay input, dash hacia adelante
        if (inputDir.sqrMagnitude < 0.01f)
            inputDir = transform.forward;

        // Delegar a PlayerHealth (que gestiona el cooldown real y la corrutina)
        if (playerHealth.CanDash)
        {
            lastDashInputTime = Time.time;
            playerHealth.TryDash(inputDir);
            Debug.Log($"[PlayerCombat] Dash hacia: {inputDir}");
        }
    }

    // ─────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────

    /// <summary>
    /// Resetea el contador de enemigos al entrar en una sala nueva.
    /// Llamado desde el gestor de salas o MetricsTracker.
    /// </summary>
    public void ResetRoomKills()
    {
        enemiesKilledThisRoom = 0;
        Debug.Log("[PlayerCombat] Contador de muertes de sala reseteado.");
    }

    // Gizmo de depuracion: muestra los rangos de ataque en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, basicAttackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, heavyAttackRange);
    }
}
