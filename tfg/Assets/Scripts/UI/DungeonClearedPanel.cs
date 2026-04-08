using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de mazmorra completada. Se activa encima del juego cuando el boss muere.
/// Vive dentro del GameController (DontDestroyOnLoad) para persistir entre escenas.
///
/// Uso:
///   - GameManager llama a Show() al detectar OnBossDead.
///   - El jugador pulsa "Siguiente Mazmorra" o "Menu Principal".
/// </summary>
public class DungeonClearedPanel : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // TITULO
    // ─────────────────────────────────────────────

    [Header("Titulo")]
    public TMP_Text tituloText;

    // ─────────────────────────────────────────────
    // ESTADISTICAS DE LA MAZMORRA
    // ─────────────────────────────────────────────

    [Header("Estadisticas")]
    public TMP_Text tiempoText;
    public TMP_Text killsText;
    public TMP_Text monedasText;
    public TMP_Text puntuacionText;

    // ─────────────────────────────────────────────
    // DIFICULTAD
    // ─────────────────────────────────────────────

    [Header("Dificultad")]
    public TMP_Text nivelText;
    /// <summary>Muestra +X / -X / = segun el ultimo ajuste DDA.</summary>
    public TMP_Text cambioText;

    // ─────────────────────────────────────────────
    // BOOST DE VELOCIDAD
    // ─────────────────────────────────────────────

    [Header("Boost de Velocidad")]
    public TMP_Text boostGanadoText;
    public TMP_Text boostTotalText;

    // ─────────────────────────────────────────────
    // BOTONES
    // ─────────────────────────────────────────────

    [Header("Botones")]
    public Button botonSiguiente;
    public Button botonMenu;

    [Header("Escenas")]
    public string mainMenuSceneName = "Start";

    [Header("Input")]
    [Tooltip("Segundos (tiempo real) que espera antes de aceptar input de teclado. " +
             "Evita que el panel se cierre al instante por el mismo clic que mato al boss.")]
    public float inputDelay = 0.5f;

    // ─────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────

    /// <summary>True cuando ya ha pasado el retardo y el panel acepta input.</summary>
    private bool  _canDismiss = false;

    /// <summary>Momento (tiempo real) en el que se mostro el panel.</summary>
    private float _showTime   = 0f;

    /// <summary>Canvas padre cacheado en Awake (cuando el objeto aun esta activo).</summary>
    private Canvas _parentCanvas;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Awake()
    {
        // Cachear el Canvas padre AQUI, mientras el Panel todavia esta activo.
        // No se puede llamar GetComponentInParent desde un objeto inactivo de forma fiable.
        _parentCanvas = GetComponentInParent<Canvas>();

        if (_parentCanvas != null)
            Debug.Log($"[DungeonClearedPanel] Canvas padre cacheado: {_parentCanvas.gameObject.name}");
        else
            Debug.LogError("[DungeonClearedPanel] No se encontro Canvas padre. Comprueba la jerarquia.");
    }

    private void Start()
    {
        if (botonSiguiente != null) botonSiguiente.onClick.AddListener(OnSiguienteMazmorra);
        if (botonMenu      != null) botonMenu.onClick.AddListener(OnMenuPrincipal);

        // Ocultar solo el Panel (no el Canvas padre)
        gameObject.SetActive(false);
        Debug.Log("[DungeonClearedPanel] Panel ocultado. Listo para usar.");
    }

    private void Update()
    {
        // El panel esta oculto: no hacer nada
        if (!gameObject.activeSelf) return;

        // Esperar el retardo antes de aceptar input
        // Se usa unscaledTime porque Time.timeScale = 0 mientras el panel esta visible
        if (!_canDismiss)
        {
            if (Time.unscaledTime >= _showTime + inputDelay)
                _canDismiss = true;
            return;
        }

        // Cualquier tecla del teclado o clic → continuar a la siguiente mazmorra
        if (Input.anyKeyDown)
            OnSiguienteMazmorra();
    }

    // ─────────────────────────────────────────────
    // MOSTRAR / OCULTAR
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activa el panel, pausa el juego y rellena los datos.
    /// Llamado por GameManager al morir el boss.
    /// </summary>
    public void Show()
    {
        Debug.Log("[DungeonClearedPanel] Show() iniciado.");

        // Asegurarse de que el Canvas padre esta activo
        if (_parentCanvas != null)
            _parentCanvas.gameObject.SetActive(true);

        // Activar el panel
        gameObject.SetActive(true);

        // Pausar el juego INMEDIATAMENTE
        Time.timeScale = 0f;
        Debug.Log("[DungeonClearedPanel] Juego pausado. timeScale = 0");

        // Desbloquear cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Activar el retardo de input
        _canDismiss = false;
        _showTime   = Time.unscaledTime;

        PopulateUI();
        Debug.Log("[DungeonClearedPanel] Panel visible y datos rellenados.");
    }

    /// <summary>Oculta el panel y reanuda el juego.</summary>
    public void Hide()
    {
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // RELLENAR DATOS
    // ─────────────────────────────────────────────

    private void PopulateUI()
    {
        GameManager      gm = GameManager.Instance;
        DifficultyManager dm = DifficultyManager.Instance;

        if (gm == null) return;

        // ── Titulo ────────────────────────────────
        if (tituloText != null)
            tituloText.text = $"MAZMORRA {gm.DungeonNumber} COMPLETADA";

        // ── Tiempo ────────────────────────────────
        if (tiempoText != null)
        {
            int mins = Mathf.FloorToInt(gm.LastDungeonTime / 60f);
            int secs = Mathf.FloorToInt(gm.LastDungeonTime % 60f);
            tiempoText.text = $"Tiempo: {mins:00}:{secs:00}";
        }

        // ── Kills ─────────────────────────────────
        if (killsText != null)
            killsText.text = $"Enemigos eliminados: {gm.LastDungeonKills}";

        // ── Monedas ───────────────────────────────
        if (monedasText != null)
            monedasText.text = $"Monedas ganadas: +{gm.LastDungeonCoins}";

        // ── Puntuacion ────────────────────────────
        if (puntuacionText != null)
            puntuacionText.text = $"Puntuacion: +{gm.LastDungeonScore:N0} pts";

        // ── Dificultad ────────────────────────────
        // Usar DifficultyManager si existe, si no usar el nivel guardado en GameManager
        int nivelActual = (dm != null) ? dm.currentLevel : gm.CurrentDifficultyLevel;
        int cambio      = (dm != null) ? dm.LastAdjustment : 0;

        if (nivelText != null)
            nivelText.text = $"Dificultad: {nivelActual}";

        if (cambioText != null)
        {
            if (cambio > 0)
            {
                cambioText.text  = $"+{cambio} ↑";
                cambioText.color = Color.green;
            }
            else if (cambio < 0)
            {
                cambioText.text  = $"{cambio} ↓";
                cambioText.color = Color.red;
            }
            else
            {
                cambioText.text  = $"= Dificultad mantenida";
                cambioText.color = Color.yellow;
            }

            if (dm == null)
                Debug.LogWarning("[DungeonClearedPanel] DifficultyManager no encontrado en escena. " +
                                 "Añade el script DifficultyManager al GameController.");
        }

        // ── Boost de velocidad ────────────────────
        if (boostGanadoText != null)
            boostGanadoText.text = $"+{gm.speedBoostPerBoss:F1} de velocidad ganada";

        if (boostTotalText != null)
        {
            // Velocidad base del PlayerController es 5f
            float velocidadTotal = 5f + gm.AccumulatedSpeedBoost;
            boostTotalText.text = $"Velocidad actual: {velocidadTotal:F1}";
        }
    }

    // ─────────────────────────────────────────────
    // BOTONES
    // ─────────────────────────────────────────────

    private void OnSiguienteMazmorra()
    {
        // Bloquear input inmediatamente para evitar doble disparo
        _canDismiss = false;

        Hide();

        if (GameManager.Instance != null)
            GameManager.Instance.LoadGameScene();
    }

    private void OnMenuPrincipal()
    {
        // Bloquear input y restaurar tiempo antes de cambiar de escena
        _canDismiss    = false;
        Time.timeScale = 1f;

        if (ScoreManager.Instance      != null) ScoreManager.Instance.ResetAll();
        if (DifficultyManager.Instance != null) DifficultyManager.Instance.currentLevel = 1;
        if (GameManager.Instance       != null) GameManager.Instance.ResetSpeedBoost();

        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}
