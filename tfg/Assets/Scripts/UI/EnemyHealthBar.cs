using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida flotante sobre los enemigos.
/// Vive en un Canvas World Space hijo del enemigo y siempre mira a la camara.
///
/// Colores:
///   Verde  → HP > 60%
///   Amarillo → HP 30-60%
///   Rojo   → HP < 30%
///
/// Setup en Unity:
///   EnemigoPrefab
///   └── HealthBarCanvas  (Canvas - World Space, Sort Order 5)
///       ├── Fondo        (Image - color oscuro semitransparente, anclada a toda el area)
///       └── Fill         (Image - Image Type = Filled, Fill Method = Horizontal)
///                        ← asignar al campo "fillImage" del script
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────

    [Header("Referencias")]
    [Tooltip("Image con Image Type = Filled para representar la vida.")]
    public Image fillImage;

    // ─────────────────────────────────────────────
    // COLORES
    // ─────────────────────────────────────────────

    [Header("Colores segun HP")]
    public Color colorAlto  = Color.green;                         // HP > 60%
    public Color colorMedio = new Color(1f, 0.65f, 0f);           // HP 30-60% (naranja)
    public Color colorBajo  = Color.red;                           // HP < 30%

    // ─────────────────────────────────────────────
    // COMPORTAMIENTO
    // ─────────────────────────────────────────────

    [Header("Comportamiento")]
    [Tooltip("Oculta la barra cuando el enemigo tiene la vida al maximo.")]
    public bool ocultarEnMaxHP = true;

    [Tooltip("Offset de altura sobre el pivote del enemigo (ajustar segun tamanyo del modelo).")]
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
        // Buscar el EnemyBase en el padre (el enemigo que nos contiene)
        _enemy          = GetComponentInParent<EnemyBase>();
        _mainCamera     = Camera.main;
        _barraTransform = transform;

        if (_enemy == null)
            Debug.LogWarning("[EnemyHealthBar] No se encontro EnemyBase en el padre.");
    }

    private void LateUpdate()
    {
        if (_enemy == null) return;

        // Buscar camara si todavia no esta (por spawn delay)
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        // ── Posicion: encima del enemigo ──────────
        _barraTransform.position = _enemy.transform.position + Vector3.up * alturaOffset;

        // ── Billboard: siempre mirar a la camara ──
        _barraTransform.LookAt(
            _barraTransform.position + _mainCamera.transform.rotation * Vector3.forward,
            _mainCamera.transform.rotation * Vector3.up
        );

        // ── Ratio de vida ─────────────────────────
        float ratio = _enemy.maxHealth > 0
            ? _enemy.GetCurrentHealth() / _enemy.maxHealth
            : 0f;

        // Ocultar si vida al maximo
        if (ocultarEnMaxHP)
        {
            bool mostrar = ratio < 0.999f;
            if (!mostrar)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
        }

        // ── Actualizar fill ───────────────────────
        if (fillImage == null) return;

        fillImage.fillAmount = Mathf.Clamp01(ratio);

        // Color segun porcentaje de vida
        if (ratio > 0.6f)
            fillImage.color = colorAlto;
        else if (ratio > 0.3f)
            fillImage.color = colorMedio;
        else
            fillImage.color = colorBajo;
    }
}
