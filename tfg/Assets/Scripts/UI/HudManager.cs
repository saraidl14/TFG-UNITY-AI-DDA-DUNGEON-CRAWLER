using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestiona el HUD de información de partida: dificultad, puntuación,
/// monedas, número de mazmorra y notificación de cambio DDA.
///
/// Coloca este script en el GameObject HUD (hijo del Canvas de la escena).
/// Las barras de vida/stamina las gestiona HealthBarUI por separado.
/// </summary>
public class HudManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // DIFICULTAD
    // ─────────────────────────────────────────────

    [Header("Dificultad")]
    [Tooltip("Texto permanente que muestra el nivel de dificultad actual.")]
    public TMP_Text dificultadText;

    [Tooltip("Texto de notificacion temporal cuando el DDA cambia el nivel. " +
             "Se desvanece solo tras 'notifDuracion' segundos.")]
    public TMP_Text ddaNotifText;

    [Tooltip("Segundos que permanece visible la notificacion DDA antes de desvanecerse.")]
    public float notifDuracion = 3f;

    // ─────────────────────────────────────────────
    // PUNTUACION Y MONEDAS
    // ─────────────────────────────────────────────

    [Header("Puntuacion y Monedas")]
    [Tooltip("Puntuacion acumulada en la partida.")]
    public TMP_Text scoreText;

    [Tooltip("Monedas ganadas en la sesion actual.")]
    public TMP_Text monedasText;

    // ─────────────────────────────────────────────
    // MAZMORRA
    // ─────────────────────────────────────────────

    [Header("Mazmorra")]
    [Tooltip("Numero de mazmorra actual.")]
    public TMP_Text mazmorraText;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    /// <summary>Ultimo nivel de dificultad conocido para detectar cambios.</summary>
    private int _lastDiffLevel = -1;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Start()
    {
        // Ocultar notificacion DDA al inicio
        if (ddaNotifText != null)
        {
            SetTextAlpha(ddaNotifText, 0f);
            ddaNotifText.text = "";
        }
    }

    private void Update()
    {
        UpdateDificultad();
        UpdateScore();
        UpdateMonedas();
        UpdateMazmorra();
    }

    // ─────────────────────────────────────────────
    // ACTUALIZACIONES POR FRAME
    // ─────────────────────────────────────────────

    private void UpdateDificultad()
    {
        DifficultyManager dm = DifficultyManager.Instance;
        if (dm == null) return;

        int nivelActual = dm.currentLevel;

        // Texto permanente de dificultad
        if (dificultadText != null)
            dificultadText.text = $"Dif: {nivelActual}";

        // Detectar cambio de nivel para la notificacion
        if (_lastDiffLevel != -1 && nivelActual != _lastDiffLevel)
        {
            int cambio = nivelActual - _lastDiffLevel;
            MostrarNotifDDA(cambio, nivelActual);
        }

        _lastDiffLevel = nivelActual;
    }

    private void UpdateScore()
    {
        if (scoreText == null) return;
        ScoreManager sm = ScoreManager.Instance;
        if (sm == null) return;

        scoreText.text = $"{sm.AccumulatedScore:N0} pts";
    }

    private void UpdateMonedas()
    {
        if (monedasText == null) return;
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        monedasText.text = $"{gm.SessionCoins} monedas";
    }

    private void UpdateMazmorra()
    {
        if (mazmorraText == null) return;
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        mazmorraText.text = $"Mazmorra {gm.DungeonNumber}";
    }

    // ─────────────────────────────────────────────
    // NOTIFICACION DDA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Muestra una notificacion flotante cuando el DDA cambia el nivel.
    ///   +2 ↑  en verde  → dificultad subio
    ///   -1 ↓  en rojo   → dificultad bajo
    /// La notificacion se desvanece sola tras notifDuracion segundos.
    /// </summary>
    private void MostrarNotifDDA(int cambio, int nivelNuevo)
    {
        if (ddaNotifText == null) return;

        if (cambio > 0)
        {
            ddaNotifText.text  = $"+{cambio} ↑  Nivel {nivelNuevo}";
            ddaNotifText.color = Color.green;
        }
        else
        {
            ddaNotifText.text  = $"{cambio} ↓  Nivel {nivelNuevo}";
            ddaNotifText.color = Color.red;
        }

        // Reiniciar coroutine si ya habia una en curso
        StopCoroutine(nameof(DesvanecerNotif));
        StartCoroutine(nameof(DesvanecerNotif));
    }

    /// <summary>
    /// Mantiene la notificacion visible notifDuracion segundos
    /// y luego la desvanece gradualmente en 0.5s.
    /// </summary>
    private IEnumerator DesvanecerNotif()
    {
        // Mostrar al 100% de opacidad
        SetTextAlpha(ddaNotifText, 1f);

        // Esperar el tiempo de visualizacion
        yield return new WaitForSeconds(notifDuracion);

        // Desvanecer en 0.5 segundos
        float elapsed   = 0f;
        float fadeDur   = 0.5f;

        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(ddaNotifText, Mathf.Lerp(1f, 0f, elapsed / fadeDur));
            yield return null;
        }

        SetTextAlpha(ddaNotifText, 0f);
        ddaNotifText.text = "";
    }

    // ─────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
