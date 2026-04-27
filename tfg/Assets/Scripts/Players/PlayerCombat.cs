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
    // ARMA EQUIPADA
    // ─────────────────────────────────────────────
    private WeaponData _equippedWeapon = null;

    // Valores por defecto (sin arma) — se guardan en Awake para poder restaurarlos
    private float _defaultBasicDamage;
    private float _defaultBasicRange;
    private float _defaultHeavyDamage;
    private float _defaultHeavyRange;

    // Boost de daño temporal por poción (0 = sin boost, 0.25 = +25%)
    private float _damageBuff = 0f;

    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────
    [Header("Modelo 3D del arma")]
    [Tooltip("GameObject vacío hijo de la cámara donde se instancia el modelo 3D del arma equipada.")]
    public WeaponHolder weaponHolder;

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

        // Guardar stats base (sin arma) para poder restaurar al desequipar
        _defaultBasicDamage = basicAttackDamage;
        _defaultBasicRange  = basicAttackRange;
        _defaultHeavyDamage = heavyAttackDamage;
        _defaultHeavyRange  = heavyAttackRange;
    }

    private void Update()
    {
        // Bloquear toda accion de combate mientras el inventario este abierto
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return;

        HandleBasicAttack();
        HandleHeavyAttack();
        HandleDash();
    }

    // ─────────────────────────────────────────────
    // ATAQUE BASICO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ataque basico con click izquierdo.
    /// Si el arma equipada es de largo alcance, dispara una flecha.
    /// Si es melee, usa el OverlapSphere habitual.
    /// </summary>
    private void HandleBasicAttack()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time < lastBasicAttackTime + basicAttackCooldown) return;

        lastBasicAttackTime = Time.time;

        if (_equippedWeapon is LongRangeWeaponData bow)
        {
            ShootArrow(bow);
        }
        else
        {
            Debug.Log("[PlayerCombat] Ataque basico ejecutado.");
            PerformAttack(basicAttackDamage, basicAttackRange);
        }
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
    /// <summary>Aplica un boost de daño temporal (llamado al usar una poción de daño).</summary>
    public void ApplyDamageBoost(float value, float duration)
    {
        StartCoroutine(DamageBoostCoroutine(value, duration));
    }

    private System.Collections.IEnumerator DamageBoostCoroutine(float value, float duration)
    {
        _damageBuff += value;
        Debug.Log($"[PlayerCombat] Damage boost: +{value * 100f:F0}% durante {duration}s");
        yield return new WaitForSeconds(duration);
        _damageBuff = Mathf.Max(0f, _damageBuff - value);
        Debug.Log("[PlayerCombat] Damage boost expirado.");
    }

    private void PerformAttack(float damage, float range)
    {
        // Aplicar buff de daño
        float finalDamage = damage * (1f + _damageBuff);

        // Origen desde el pecho del jugador hacia adelante, no desde los pies
        Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
        Collider[] hits = Physics.OverlapSphere(origin, range, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            // Aplicar dano al enemigo
            float hpBefore = enemy.GetCurrentHealth();
            enemy.TakeDamage(finalDamage);
            float hpAfter = enemy.GetCurrentHealth();

            Debug.Log($"[PlayerCombat] Dano {finalDamage:F1} a {hit.name} | HP: {hpBefore} -> {hpAfter}");

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
    // DISPARO (arco / ballesta)
    // ─────────────────────────────────────────────

    private void ShootArrow(LongRangeWeaponData bow)
    {
        if (bow.projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] El arco no tiene projectilePrefab asignado.");
            return;
        }

        // Buscar flechas en el inventario (slots generales 3-8)
        int ammoSlot = FindAmmoSlot();
        if (ammoSlot < 0)
        {
            Debug.Log("[PlayerCombat] Sin flechas en el inventario.");
            return;
        }

        // Origen y dirección desde el centro de la cámara
        Vector3 origin    = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        GameObject arrowGO = Instantiate(bow.projectilePrefab, origin, Quaternion.LookRotation(direction));

        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.damage = bow.GetEffectiveDamage() * (1f + _damageBuff);
            arrow.speed  = bow.projectileSpeed;
        }

        // Consumir una flecha del inventario
        int remaining = Inventory.Instance.GetSlot(ammoSlot).quantity - 1;
        Inventory.Instance.UseItem(ammoSlot);
        Debug.Log($"[PlayerCombat] Flecha disparada | Flechas restantes: {remaining}");
    }

    /// <summary>
    /// Busca el primer slot del inventario (general, índices 3-8) que contenga AmmoData.
    /// Devuelve el índice del slot o -1 si no hay flechas.
    /// </summary>
    private int FindAmmoSlot()
    {
        if (Inventory.Instance == null) return -1;

        for (int i = Inventory.WEAPON_SLOTS; i < Inventory.TOTAL_SLOTS; i++)
        {
            Inventory.Slot slot = Inventory.Instance.GetSlot(i);
            if (!slot.IsEmpty && slot.item.itemType == ItemType.Ammo)
                return i;
        }
        return -1;
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

    // ─────────────────────────────────────────────
    // EQUIPAR ARMA (llamado por HotbarUI)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Equipa un arma y aplica sus stats a los ataques.
    /// Pasar null para desequipar y volver a los stats base.
    /// </summary>
    public void SetActiveWeapon(WeaponData weapon)
    {
        _equippedWeapon = weapon;

        if (weapon != null)
        {
            basicAttackDamage = weapon.GetEffectiveDamage();
            basicAttackRange  = weapon.AttackRange;
            heavyAttackDamage = weapon.GetEffectiveDamage() * 2.5f;
            heavyAttackRange  = weapon.AttackRange * 1.2f;
            Debug.Log($"[PlayerCombat] Arma equipada: {weapon.itemName} | Daño: {basicAttackDamage:F1} | Rango: {basicAttackRange:F1}");
        }
        else
        {
            basicAttackDamage = _defaultBasicDamage;
            basicAttackRange  = _defaultBasicRange;
            heavyAttackDamage = _defaultHeavyDamage;
            heavyAttackRange  = _defaultHeavyRange;
            Debug.Log("[PlayerCombat] Sin arma equipada. Stats base restaurados.");
        }

        // Actualizar el modelo 3D en la mano
        if (weaponHolder != null)
        {
            if (weapon != null) weaponHolder.ShowWeapon(weapon);
            else                weaponHolder.HideWeapon();
        }
    }

    /// <summary>El arma actualmente equipada (null = puños).</summary>
    public WeaponData EquippedWeapon => _equippedWeapon;

    // Gizmo de depuracion: muestra los rangos de ataque en el editor
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, basicAttackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, heavyAttackRange);
    }
}
