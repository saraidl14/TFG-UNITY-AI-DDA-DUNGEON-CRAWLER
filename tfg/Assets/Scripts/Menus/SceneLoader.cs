/*  Nombre:      SceneLoader.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       12/05/2026
 *  Descripcion: Carga de escenas asincrona con pantalla de carga.
 *               Sustituye a SceneManager.LoadScene() en toda la aplicacion.
 *
 *  SETUP EN UNITY:
 *    1. Crear una escena llamada "LoadingScreen" (o el nombre que se ponga en
 *       loadingSceneName) y añadirla a Build Settings.
 *    2. En esa escena crear un Canvas con:
 *         LoadingRoot  (Image fondo oscuro, este script o vacío)
 *         └─ ProgressBar  (Slider)  → asignar a 'progressBar'
 *         └─ LoadingText  (TMP_Text) → asignar a 'loadingText'  (opcional)
 *    3. Añadir este script como componente en esa escena de carga
 *       (o en un objeto persistente).
 *
 *  USO DESDE CÓDIGO:
 *    // En lugar de: SceneManager.LoadScene("GameScene");
 *    // Usar:        SceneLoader.LoadScene("GameScene");
 *
 *    // O con callback al terminar:
 *    SceneLoader.LoadScene("GameScene", () => Debug.Log("Cargado"));
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Carga de escenas asincrona con pantalla de transicion.
/// Singleton persistente: se crea automaticamente la primera vez que se usa.
/// </summary>
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
    }

    // ─────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("Escena de carga intermedia (debe estar en Build Settings)")]
    [Tooltip("Nombre exacto de la escena usada como pantalla de carga.")]
    public string loadingSceneName = "LoadingScreen";

    [Header("UI de progreso (asignar desde la escena de carga)")]
    [Tooltip("Slider que muestra el progreso de carga (0-1).")]
    public Slider progressBar;

    [Tooltip("Texto TMP opcional que muestra el porcentaje.")]
    public TMP_Text loadingText;

    [Header("Tiempo minimo de pantalla de carga (segundos)")]
    [Tooltip("Evita que la pantalla de carga parpadee si la escena carga muy rapido.")]
    public float minLoadingTime = 0.5f;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private static string    _targetScene;
    private static Action     _onLoaded;

    // ─────────────────────────────────────────────
    // API ESTÁTICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Inicia la carga asincrona de la escena indicada mostrando una
    /// pantalla de carga intermedia.
    /// </summary>
    /// <param name="sceneName">Nombre de la escena destino.</param>
    /// <param name="onLoaded">Callback opcional al terminar la carga.</param>
    public static void LoadScene(string sceneName, Action onLoaded = null)
    {
        _targetScene = sceneName;
        _onLoaded    = onLoaded;

        // Restaurar timeScale por si se llama desde pausa
        Time.timeScale   = 1f;
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        // Si hay instancia singleton, usarla; si no, cargar directamente
        if (Instance != null)
            Instance.StartCoroutine(Instance.LoadViaLoadingScreen());
        else
            SceneManager.LoadScene(sceneName); // fallback sin pantalla de carga
    }

    // ─────────────────────────────────────────────
    // CORRUTINA DE CARGA
    // ─────────────────────────────────────────────

    private IEnumerator LoadViaLoadingScreen()
    {
        // 1. Cargar la escena de carga de forma sincrona (es muy ligera)
        SceneManager.LoadScene(loadingSceneName);
        yield return null; // esperar un frame para que la escena se active

        // 2. Buscar la UI de progreso en la nueva escena (por si está en otro objeto)
        if (progressBar == null)
            progressBar = FindObjectOfType<Slider>();
        if (loadingText == null)
            loadingText = FindObjectOfType<TMP_Text>();

        // 3. Iniciar la carga asíncrona de la escena destino
        float   startTime = Time.realtimeSinceStartup;
        AsyncOperation async = SceneManager.LoadSceneAsync(_targetScene);

        // No activar la escena hasta que esté lista y haya pasado el tiempo mínimo
        async.allowSceneActivation = false;

        while (!async.isDone)
        {
            // El progreso llega hasta 0.9 antes de activar la escena
            float progress = Mathf.Clamp01(async.progress / 0.9f);

            if (progressBar != null) progressBar.value = progress;
            if (loadingText  != null) loadingText.text  = $"Cargando... {(int)(progress * 100f)}%";

            // Activar cuando esté cargada Y haya pasado el tiempo mínimo
            bool minTimePassed = (Time.realtimeSinceStartup - startTime) >= minLoadingTime;
            if (async.progress >= 0.9f && minTimePassed)
            {
                if (progressBar != null) progressBar.value = 1f;
                if (loadingText  != null) loadingText.text  = "Cargando... 100%";
                yield return new WaitForSecondsRealtime(0.1f); // pequeña pausa visual
                async.allowSceneActivation = true;
            }

            yield return null;
        }

        // 4. Llamar al callback si existe
        _onLoaded?.Invoke();
        _onLoaded = null;
    }
}
