/*  Nombre:      SceneLoader.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       25/05/2026
 *  Descripcion: Pantalla de carga mediante un panel DontDestroyOnLoad.
 *               NO necesita escena separada.
 *               Carga la escena destino en background, muestra progreso y consejos.
 *  USO DESDE CÓDIGO:
 *    SceneLoader.LoadScene("GameScene");
 *    SceneLoader.LoadScene("MainMenu", () => Debug.Log("Listo"));
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────
    [Header("Panel que cubre la pantalla durante la carga (empieza DESACTIVADO)")]
    public GameObject loadingPanel;

    [Header("Barra de progreso")]
    public Slider progressBar;

    [Header("Texto de porcentaje  (opcional)")]
    public TMP_Text loadingText;

    [Header("Texto de consejos  (opcional)")]
    public TMP_Text tipText;

    // ─────────────────────────────────────────────
    // CONSEJOS
    // ─────────────────────────────────────────────
    [Header("Consejos que aparecen durante la carga")]
    [TextArea(2, 4)]
    public string[] tips = new string[]
    {
        // ── Controles básicos ──
        "Mantén el botón derecho del ratón (RMB) para apuntar con el arco.",
        "Pulsa LMB para atacar con la espada y RMB + LMB para disparar una flecha.",
        "El dash (Espacio) te permite esquivar ataques en el último momento.",
        "Pulsa E para interactuar con puertas, cofres y otros objetos del entorno.",
        "Abre el inventario con Tab para gestionar tus objetos y equipar armas.",

        // ── Combate ──
        "Derrota a todos los enemigos de una sala para desbloquear sus puertas.",
        "Los slimes son lentos pero golpean fuerte: usa el dash para esquivarlos.",
        "Encadena golpes de espada y luego retrocede antes de que contraataquen.",
        "Usa el arco para atacar a distancia antes de que los enemigos lleguen a ti.",
        "El jefe final tiene más vida y hace más daño que los enemigos normales. ¡Prepárate!",

        // ── Exploración y objetos ──
        "Los cofres contienen armas, pociones y objetos útiles. ¡Ábrelos todos!",
        "Usa las pociones de salud cuando estés por debajo del 30% de vida.",
        "Guardar las pociones para el final puede costarte la partida. Úsalas cuando las necesites.",
        "Cada mazmorra es generada de forma procedural: ningún recorrido será igual.",

        // ── Sistema DDA ──
        "Cuanto más rápido y limpio juegues, más subirá la dificultad automáticamente.",
        "Si recibes mucho daño o mueres, la dificultad bajará para equilibrar la partida.",
        "El sistema de dificultad dinámica aprende de tu estilo de juego en cada sala.",
        "Completar una sala sin recibir ningún daño te da un bonus de sala perfecta.",

        // ── Speedrun / eficiencia ──
        "Cuanto antes mates al jefe, mejor será tu puntuación final.",
        "Limpiar una sala muy rápido aumenta tu puntuación y sube la dificultad más deprisa.",
        "No pierdas tiempo en salas ya limpias: las puertas siguen abiertas al volver.",
        "Planifica tu ruta desde el inicio: la sala del jefe suele estar al fondo de la mazmorra.",

        // ── Puntuación ──
        "Tu puntuación depende del tiempo, el daño recibido, las muertes y los enemigos eliminados.",
        "Encadenar varias salas perfectas consecutivas multiplica tu bonus de puntuación.",
        "El rango final va de D a S+. ¿Puedes conseguir una S?",
    };

    [Header("Tiempo mínimo visible (evita parpadeo en máquinas muy rápidas)")]
    public float minDisplayTime = 0.5f;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private static string _targetScene;
    private static Action  _onLoaded;

    // ─────────────────────────────────────────────
    // API ESTÁTICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Carga la escena mostrando el panel de carga encima de la pantalla actual.
    /// No necesita escena separada.
    /// </summary>
    public static void LoadScene(string sceneName, Action onLoaded = null)
    {
        _targetScene = sceneName;
        _onLoaded    = onLoaded;

        if (Instance != null)
            Instance.StartCoroutine(Instance.LoadWithPanel());
        else
            SceneManager.LoadScene(sceneName); // fallback si no hay instancia
    }

    // ─────────────────────────────────────────────
    // CORRUTINA PRINCIPAL
    // ─────────────────────────────────────────────

    private IEnumerator LoadWithPanel()
    {
        // 1. Mostrar panel
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar  != null) progressBar.value = 0f;
        if (loadingText  != null) loadingText.text  = "Cargando... 0%";
        ShowRandomTip();

        yield return null; // un frame para que el panel aparezca

        // 2. Iniciar carga asíncrona — la duración la decide el hardware, no un timer fijo
        AsyncOperation async = SceneManager.LoadSceneAsync(_targetScene);
        async.allowSceneActivation = false;

        // 3. Animar la barra siguiendo el progreso REAL de carga
        //    - Máquina lenta → async.progress sube despacio → barra tarda más
        //    - Máquina rápida → async.progress sube rápido → barra termina antes
        //    - Lerp suaviza visualmente los saltos bruscos de progreso
        float shown   = 0f;
        float elapsed = 0f;

        while (async.progress < 0.9f || shown < 0.99f || elapsed < minDisplayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float real = Mathf.Clamp01(async.progress / 0.9f);
            shown = Mathf.Lerp(shown, real, Time.unscaledDeltaTime * 3f);
            SetProgress(shown);
            yield return null;
        }
        SetProgress(1f);

        // 4. Activar la escena
        async.allowSceneActivation = true;
        while (!async.isDone)
            yield return null;

        // 6. Ocultar panel
        if (loadingPanel != null) loadingPanel.SetActive(false);
        _onLoaded?.Invoke();
        _onLoaded = null;
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private void SetProgress(float value)
    {
        if (progressBar != null) progressBar.value = value;
        if (loadingText  != null) loadingText.text  = $"Cargando... {(int)(value * 100f)}%";
    }

    private void ShowRandomTip()
    {
        if (tipText == null || tips == null || tips.Length == 0) return;
        tipText.text = tips[UnityEngine.Random.Range(0, tips.Length)];
    }
}
