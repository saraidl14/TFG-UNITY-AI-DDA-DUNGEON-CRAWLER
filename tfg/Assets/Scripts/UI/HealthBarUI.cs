/*  Nombre:      HealthBarUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/05/2026
 *  Descripcion: Controla las barras de vida y stamina del jugador en el HUD con lerp suave.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la barra de vida y stamina del jugador en el HUD.
/// Ambas barras se animan con lerp suave hacia el valor real.
/// Al recibir daño también hay un flash rojo en pantalla.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────

    [Header("Barra de Vida")]
    public Slider   healthBar;
    public TMP_Text healthText;

    [Tooltip("Velocidad del lerp al bajar (daño).")]
    public float healthLerpDown = 5f;
    [Tooltip("Velocidad del lerp al subir (curación).")]
    public float healthLerpUp   = 3f;

    [Header("Barra de Stamina")]
    public Slider staminaBar;
    public float  staminaLerpSpeed = 8f;

    [Header("Efecto de Daño")]
    public Image damageFlash;
    public float flashDuration = 0.2f;
    [Range(0f, 1f)]
    public float flashMaxAlpha = 0.35f;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    private PlayerHealth _playerHealth;
    private float        _lastHealth;
    private float        _healthDisplay   = 1f;   // valor animado de la barra de vida
    private float        _staminaDisplay  = 1f;   // valor animado de la barra de stamina
    private bool         _flashRunning    = false;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (damageFlash != null)
        {
            Color c = damageFlash.color;
            c.a = 0f;
            damageFlash.color = c;
        }
    }

    private void Update()
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindObjectOfType<PlayerHealth>();
            if (_playerHealth == null) return;

            _lastHealth    = _playerHealth.GetCurrentHealth();
            _healthDisplay = _lastHealth / Mathf.Max(1f, _playerHealth.GetMaxHealth());
            _staminaDisplay = 1f;
        }

        UpdateHealthBar();
        UpdateStaminaBar();
    }

    // ─────────────────────────────────────────────
    // BARRA DE VIDA — lerp suave
    // ─────────────────────────────────────────────

    private void UpdateHealthBar()
    {
        float hp    = _playerHealth.GetCurrentHealth();
        float maxHp = _playerHealth.GetMaxHealth();
        float target = maxHp > 0f ? hp / maxHp : 0f;

        // Detectar daño → flash de pantalla
        if (hp < _lastHealth && !_flashRunning)
            StartCoroutine(FlashDamage());

        _lastHealth = hp;

        // Lerp: más rápido al bajar (daño), más lento al subir (curación)
        float speed = (target < _healthDisplay) ? healthLerpDown : healthLerpUp;
        _healthDisplay = Mathf.Lerp(_healthDisplay, target, speed * Time.deltaTime);

        if (healthBar  != null) healthBar.value = _healthDisplay;
        if (healthText != null) healthText.text = $"{Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}";
    }

    // ─────────────────────────────────────────────
    // BARRA DE STAMINA — lerp suave
    // ─────────────────────────────────────────────

    private void UpdateStaminaBar()
    {
        if (staminaBar == null) return;

        float stamina    = _playerHealth.GetCurrentStamina();
        float maxStamina = _playerHealth.GetMaxStamina();
        float target     = maxStamina > 0f ? stamina / maxStamina : 0f;

        _staminaDisplay  = Mathf.Lerp(_staminaDisplay, target, staminaLerpSpeed * Time.deltaTime);
        staminaBar.value = _staminaDisplay;
    }

    // ─────────────────────────────────────────────
    // FLASH DE DAÑO
    // ─────────────────────────────────────────────

    private IEnumerator FlashDamage()
    {
        if (damageFlash == null) yield break;
        _flashRunning = true;

        Color c = damageFlash.color;
        c.a = flashMaxAlpha;
        damageFlash.color = c;

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
        _flashRunning = false;
    }
}
