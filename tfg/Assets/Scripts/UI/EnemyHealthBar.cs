using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida flotante sobre los enemigos.
/// Usa un Slider cuyo value baja con lerp suave al recibir daño.
///
/// Setup en Unity (prefab de cada enemigo):
///   EnemigoPrefab
///   └── HealthBarCanvas  (Canvas - World Space, Sort Order 5)
///       └── HealthSlider (Slider)  ← arrastrar al campo healthSlider
///           ├── Background  (Image oscura)
///           └── Fill Area
///               └── Fill    (Image roja) ← arrastrar al campo fillImage (para color)
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El componente Slider de la barra de vida.")]
    public Slider healthSlider;

    [Tooltip("La Image del Fill del slider (para cambiar el color segun HP).")]
    public Image fillImage;

    [Header("Colores segun HP")]
    public Color colorAlto  = Color.green;
    public Color colorMedio = new Color(1f, 0.65f, 0f);
    public Color colorBajo  = Color.red;

    [Header("Comportamiento")]
    [Tooltip("Oculta la barra cuando el enemigo tiene la vida al maximo.")]
    public bool ocultarEnMaxHP = true;

    [Tooltip("Offset de altura sobre el pivote del enemigo.")]
    public float alturaOffset = 1.8f;

    [Tooltip("Velocidad del lerp al bajar el value de la barra.")]
    public float lerpSpeed = 6f;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    private float     _displayValue = 1f;   // valor animado del slider
    private EnemyBase _enemy;
    private Camera    _mainCamera;
    private Canvas    _canvas;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _enemy      = GetComponentInParent<EnemyBase>();
        _mainCamera = Camera.main;
        _canvas     = GetComponent<Canvas>();

        transform.localPosition = Vector3.up * alturaOffset;

        // ── Auto-buscar referencias si no están asignadas en el Inspector ──
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (fillImage == null && healthSlider != null && healthSlider.fillRect != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        if (_enemy == null)
            Debug.LogWarning("[EnemyHealthBar] No se encontro EnemyBase en el padre.");

        if (healthSlider == null)
            Debug.LogWarning("[EnemyHealthBar] No se encontro Slider. Asignalo en el Inspector o añade un Slider hijo.");

        // Inicializar slider a 1 (vida completa)
        if (healthSlider != null) healthSlider.value = 1f;
    }

    private void LateUpdate()
    {
        if (_enemy == null) return;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        // Billboard: mirar siempre a la camara
        transform.LookAt(
            transform.position + _mainCamera.transform.rotation * Vector3.forward,
            _mainCamera.transform.rotation * Vector3.up
        );

        // Ratio de vida real
        float ratio = _enemy.maxHealth > 0f
            ? _enemy.GetCurrentHealth() / _enemy.maxHealth
            : 0f;

        // Ocultar si vida al maximo
        if (ocultarEnMaxHP)
        {
            bool mostrar = ratio < 0.999f;
            if (_canvas != null) _canvas.enabled = mostrar;
            if (!mostrar) return;
        }

        if (healthSlider == null) return;

        // Animar el value del Slider con lerp suave
        _displayValue      = Mathf.Lerp(_displayValue, Mathf.Clamp01(ratio), lerpSpeed * Time.deltaTime);
        healthSlider.value = _displayValue;

        // Color segun vida REAL (cambia en el momento correcto, no con el retraso del lerp)
        if (fillImage != null)
        {
            if (ratio > 0.6f)
                fillImage.color = colorAlto;
            else if (ratio > 0.3f)
                fillImage.color = colorMedio;
            else
                fillImage.color = colorBajo;
        }
    }
}
