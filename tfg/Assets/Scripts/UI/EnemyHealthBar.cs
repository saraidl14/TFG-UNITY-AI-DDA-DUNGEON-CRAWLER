using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida flotante sobre los enemigos.
///
/// IMPORTANTE: este script debe ir en el objeto RAIZ del enemigo (no en el Canvas).
/// El Canvas (HealthBarCanvas) es un hijo del enemigo y se referencia con "healthBarCanvas".
///
/// Colores:
///   Verde    → HP > 60%
///   Naranja  → HP 30-60%
///   Rojo     → HP < 30%
///
/// Jerarquia:
///   EnemigoPrefab           ← EnemyHealthBar.cs va AQUI
///   └── HealthBarCanvas     (Canvas - World Space, Sort Order 5) ← asignar a "healthBarCanvas"
///       └── Slider          ← asignar a "healthSlider"
///           ├── Background
///           ├── Fill Area
///           │   └── Fill    ← asignar a "fillImage"
///           └── Handle Slide Area  ← ELIMINAR o desactivar
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────

    [Header("Referencias")]
    [Tooltip("Canvas hijo del enemigo que contiene la barra de vida.")]
    public GameObject healthBarCanvas;

    [Tooltip("Slider de la barra de vida.")]
    public Slider healthSlider;

    [Tooltip("Image Fill para cambiar el color segun HP (el Fill hijo del Slider).")]
    public Image fillImage;

    // ─────────────────────────────────────────────
    // COLORES
    // ─────────────────────────────────────────────

    [Header("Colores segun HP")]
    public Color colorAlto  = Color.green;
    public Color colorMedio = new Color(1f, 0.65f, 0f);
    public Color colorBajo  = Color.red;

    // ─────────────────────────────────────────────
    // COMPORTAMIENTO
    // ─────────────────────────────────────────────

    [Header("Comportamiento")]
    [Tooltip("Oculta la barra cuando el enemigo tiene la vida al maximo.")]
    public bool ocultarEnMaxHP = true;

    [Tooltip("Offset de altura sobre el pivote del enemigo.")]
    public float alturaOffset = 1.8f;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    private EnemyBase _enemy;
    private Camera    _mainCamera;
    private Transform _barraTransform;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Awake()
    {
        // El script esta en el enemigo; buscar EnemyBase en este mismo objeto o en el padre
        _enemy      = GetComponent<EnemyBase>();
        if (_enemy == null)
            _enemy  = GetComponentInParent<EnemyBase>();

        _mainCamera = Camera.main;

        // Si no se asigno el canvas en el Inspector, buscarlo automaticamente
        if (healthBarCanvas == null)
            healthBarCanvas = GetComponentInChildren<Canvas>(true)?.gameObject;

        if (healthBarCanvas != null)
            _barraTransform = healthBarCanvas.transform;

        if (_enemy == null)
            Debug.LogWarning("[EnemyHealthBar] No se encontro EnemyBase en este objeto.", this);
        if (healthBarCanvas == null)
            Debug.LogWarning("[EnemyHealthBar] No se encontro el Canvas de la barra de vida.", this);
    }

    private void LateUpdate()
    {
        if (_enemy == null || healthBarCanvas == null) return;

        // Buscar camara si todavia no esta (por spawn delay)
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        // ── Ratio de vida ─────────────────────────
        float ratio = _enemy.maxHealth > 0
            ? _enemy.GetCurrentHealth() / _enemy.maxHealth
            : 0f;

        // ── Ocultar si vida al maximo ──────────────
        // IMPORTANTE: controlamos el Canvas hijo, NO este gameObject.
        // Asi el script sigue corriendo aunque la barra este oculta.
        if (ocultarEnMaxHP)
        {
            bool mostrar = ratio < 0.999f;
            healthBarCanvas.SetActive(mostrar);
            if (!mostrar) return;
        }
        else
        {
            healthBarCanvas.SetActive(true);
        }

        // ── Posicion: encima del enemigo ──────────
        if (_barraTransform != null)
        {
            _barraTransform.position = transform.position + Vector3.up * alturaOffset;

            // ── Billboard: siempre mirar a la camara ──
            _barraTransform.LookAt(
                _barraTransform.position + _mainCamera.transform.rotation * Vector3.forward,
                _mainCamera.transform.rotation * Vector3.up
            );
        }

        // ── Actualizar Slider ─────────────────────
        if (healthSlider != null)
            healthSlider.value = Mathf.Clamp01(ratio);

        // ── Actualizar color del Fill ─────────────
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
