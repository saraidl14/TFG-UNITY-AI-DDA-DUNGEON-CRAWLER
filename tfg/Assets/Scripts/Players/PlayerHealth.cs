using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la salud y la stamina del jugador.
/// Incluye sistema de stamina dinamica con penalizacion por uso excesivo,
/// dash con cooldown, curacion y eventos de muerte para el sistema DDA.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SALUD
    // ─────────────────────────────────────────────
    [Header("Salud")]
    public float maxHealth = 100f;
    private float currentHealth;

    // ─────────────────────────────────────────────
    // STAMINA DINAMICA
    // ─────────────────────────────────────────────
    [Header("Stamina")]
    public float maxStamina = 100f;

    /// <summary>Tasa de regeneracion rapida (gasto menor al 50%)</summary>
    public float staminaRegenFast = 20f;

    /// <summary>Tasa de regeneracion lenta (usada despues del periodo de penalizacion)</summary>
    public float staminaRegenSlow = 8f;

    /// <summary>Tiempo de penalizacion en segundos cuando se supera el 50% de gasto</summary>
    public float staminaPenaltyDuration = 7f;

    private float currentStamina;

    /// <summary>true mientras el cooldown de penalizacion esta activo</summary>
    private bool isStaminaPenalized = false;

    /// <summary>Marca de tiempo en la que acaba la penalizacion</summary>
    private float penaltyEndTime = 0f;

    /// <summary>Acumulado de stamina gastada en el ciclo actual (se resetea al regenerar completamente)</summary>
    private float staminaSpentThisCycle = 0f;

    // ─────────────────────────────────────────────
    // DASH
    // ─────────────────────────────────────────────
    [Header("Dash")]
    /// <summary>Distancia que recorre el dash en metros</summary>
    public float dashDistance = 5f;

    /// <summary>Duracion del impulso del dash en segundos</summary>
    public float dashDuration = 0.15f;

    /// <summary>Cooldown minimo del dash en segundos</summary>
    public float dashCooldownMin = 1f;

    /// <summary>Cooldown maximo del dash (no se usa directamente, referencia de diseno)</summary>
    public float dashCooldownMax = 2f;

    private float dashCooldown = 1.5f; // Valor inicial dentro del rango
    private float lastDashTime = -999f;
    private bool isDashing = false;

    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────
    private CharacterController characterController;

    // ─────────────────────────────────────────────
    // EVENTOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Evento estatico disparado al morir el jugador.
    /// GameManager y ScoreManager se suscriben a este evento.
    /// </summary>
    public static event System.Action OnPlayerDeath;

    // ─────────────────────────────────────────────
    // PROPIEDADES PUBLICAS
    // ─────────────────────────────────────────────

    /// <summary>Stamina actual del jugador (lectura desde PlayerCombat y MetricsTracker)</summary>
    public float CurrentStamina => currentStamina;

    /// <summary>Salud actual del jugador</summary>
    public float CurrentHealth => currentHealth;

    /// <summary>True si el dash esta disponible</summary>
    public bool CanDash => !isDashing && (Time.time >= lastDashTime + dashCooldown);

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Awake()
    {
        currentHealth  = maxHealth;
        currentStamina = maxStamina;
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleStaminaRegen();
    }

    // ─────────────────────────────────────────────
    // SALUD
    // ─────────────────────────────────────────────

    /// <summary>
    /// Aplica dano al jugador. Registra el dano en MetricsTracker para el DDA.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDashing) return; // Inmunidad durante el dash

        currentHealth -= amount;
        currentHealth  = Mathf.Max(currentHealth, 0f);

        // Registrar dano en metricas DDA
        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.RegisterDamage(amount);

        Debug.Log($"[PlayerHealth] Dano recibido: {amount} | Salud restante: {currentHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Cura al jugador una cantidad determinada sin superar el maximo.
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealth] Curacion: {amount} | Salud actual: {currentHealth}");
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Jugador muerto.");

        // Registrar muerte en metricas DDA
        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.RegisterDeath();

        // Notificar a todos los suscriptores (GameManager, ScoreManager, etc.)
        OnPlayerDeath?.Invoke();
    }

    // ─────────────────────────────────────────────
    // STAMINA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Intenta gastar stamina. Devuelve false si no hay suficiente.
    /// Aplica penalizacion si el gasto supera el 50% del maximo.
    /// </summary>
    public bool TrySpendStamina(float amount)
    {
        if (currentStamina < amount)
        {
            Debug.Log("[PlayerHealth] Stamina insuficiente.");
            return false;
        }

        currentStamina      -= amount;
        staminaSpentThisCycle += amount;

        // Si el gasto acumulado supera el 50% del maximo, activar penalizacion
        if (!isStaminaPenalized && staminaSpentThisCycle > maxStamina * 0.5f)
        {
            isStaminaPenalized = true;
            penaltyEndTime     = Time.time + staminaPenaltyDuration;
            Debug.Log($"[PlayerHealth] Penalizacion de stamina activada por {staminaPenaltyDuration}s.");
        }

        return true;
    }

    /// <summary>
    /// Regenera la stamina cada frame segun el estado de penalizacion.
    /// </summary>
    private void HandleStaminaRegen()
    {
        // Comprobar si ha terminado el periodo de penalizacion
        if (isStaminaPenalized && Time.time >= penaltyEndTime)
        {
            isStaminaPenalized    = false;
            staminaSpentThisCycle = 0f;
            Debug.Log("[PlayerHealth] Penalizacion de stamina finalizada.");
        }

        // No regenerar si esta penalizado
        if (isStaminaPenalized) return;

        // Regenerar: rapido si el gasto fue menor al 50%, lento si estaba en modo recuperacion
        float regenRate = (staminaSpentThisCycle <= maxStamina * 0.5f) ? staminaRegenFast : staminaRegenSlow;
        currentStamina  = Mathf.Min(currentStamina + regenRate * Time.deltaTime, maxStamina);

        // Resetear ciclo al rellenar completamente
        if (currentStamina >= maxStamina)
            staminaSpentThisCycle = 0f;
    }

    // ─────────────────────────────────────────────
    // DASH
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ejecuta el dash si esta disponible. Sin coste de stamina.
    /// Llamado desde PlayerCombat al detectar la entrada de Shift.
    /// </summary>
    public void TryDash(Vector3 direction)
    {
        if (!CanDash) return;

        lastDashTime = Time.time;
        StartCoroutine(DashCoroutine(direction));
    }

    private IEnumerator DashCoroutine(Vector3 direction)
    {
        isDashing = true;
        Debug.Log("[PlayerHealth] Dash ejecutado.");

        float elapsed   = 0f;
        float speed     = dashDistance / dashDuration;
        Vector3 dashDir = direction.normalized;

        while (elapsed < dashDuration)
        {
            // Mover usando CharacterController si esta disponible
            if (characterController != null)
                characterController.Move(dashDir * speed * Time.deltaTime);
            else
                transform.Translate(dashDir * speed * Time.deltaTime, Space.World);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    // ─────────────────────────────────────────────
    // GETTERS (compatibilidad con codigo existente)
    // ─────────────────────────────────────────────

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth()     => maxHealth;
    public float GetCurrentStamina() => currentStamina;
    public float GetMaxStamina()    => maxStamina;
}
