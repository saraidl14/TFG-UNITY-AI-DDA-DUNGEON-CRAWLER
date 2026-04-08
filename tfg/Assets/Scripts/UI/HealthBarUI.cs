using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la barra de vida y stamina del jugador en el HUD.
///
/// Requiere en la escena:
///   - Slider o Image (fillAmount) para la vida    → asignar en healthBar
///   - Slider o Image (fillAmount) para la stamina → asignar en staminaBar
///   - Image de pantalla completa semitransparente  → asignar en damageFlash
///
/// El efecto de daño hace un flash rojo en pantalla al recibir golpes.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────

    [Header("Barra de Vida")]
    [Tooltip("Slider o Image con ImageType = Filled para la vida.")]
    public Slider healthBar;

    [Tooltip("Texto opcional que muestra HP actual / HP max.")]
    public TMP_Text healthText;

    [Header("Barra de Stamina")]
    [Tooltip("Slider o Image con ImageType = Filled para la stamina.")]
    public Slider staminaBar;

    [Header("Efecto de Daño")]
    [Tooltip("Image de pantalla completa (color rojo semitransparente) para el flash de daño.")]
    public Image damageFlash;

    [Tooltip("Duracion del flash rojo en segundos.")]
    public float flashDuration = 0.2f;

    [Tooltip("Opacidad maxima del flash (0-1).")]
    [Range(0f, 1f)]
    public float flashMaxAlpha = 0.35f;

    // ─────────────────────────────────────────────
    // REFERENCIA AL JUGADOR
    // ─────────────────────────────────────────────

    private PlayerHealth _playerHealth;
    private float _lastHealth;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Start()
    {
        // Inicializar flash a transparente siempre, independientemente del player
        if (damageFlash != null)
        {
            Color c = damageFlash.color;
            c.a = 0f;
            damageFlash.color = c;
        }
    }

    private void Update()
    {
        // Buscar al player si todavia no se ha instanciado (spawna con delay)
        if (_playerHealth == null)
        {
            _playerHealth = FindObjectOfType<PlayerHealth>();
            if (_playerHealth == null) return;

            // Primera vez que lo encontramos: inicializar lastHealth
            _lastHealth = _playerHealth.GetCurrentHealth();
        }

        // Detectar si se ha recibido daño este frame
        float currentHealth = _playerHealth.GetCurrentHealth();
        if (currentHealth < _lastHealth)
            StartCoroutine(FlashDamage());

        _lastHealth = currentHealth;

        UpdateHealthBar();
        UpdateStaminaBar();
    }

    // ─────────────────────────────────────────────
    // ACTUALIZAR BARRAS
    // ─────────────────────────────────────────────

    private void UpdateHealthBar()
    {
        float hp    = _playerHealth.GetCurrentHealth();
        float maxHp = _playerHealth.GetMaxHealth();
        float ratio = maxHp > 0 ? hp / maxHp : 0f;

        if (healthBar != null)
            healthBar.value = ratio;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}";
    }

    private void UpdateStaminaBar()
    {
        float stamina    = _playerHealth.GetCurrentStamina();
        float maxStamina = _playerHealth.GetMaxStamina();
        float ratio      = maxStamina > 0 ? stamina / maxStamina : 0f;

        if (staminaBar != null)
            staminaBar.value = ratio;
    }

    // ─────────────────────────────────────────────
    // EFECTO FLASH DE DAÑO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Hace un flash rojo semitransparente en pantalla al recibir daño.
    /// Aparece rapido y desaparece gradualmente.
    /// </summary>
    private IEnumerator FlashDamage()
    {
        if (damageFlash == null) yield break;

        // Aparecer instantaneo
        Color c = damageFlash.color;
        c.a = flashMaxAlpha;
        damageFlash.color = c;

        // Desvanecerse gradualmente
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(flashMaxAlpha, 0f, elapsed / flashDuration);
            damageFlash.color = c;
            yield return null;
        }

        c.a = 0f;
        damageFlash.color = c;
    }
}
